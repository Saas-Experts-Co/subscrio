import { ISubscriptionRepository } from '../repositories/ISubscriptionRepository.js';
import { ICustomerRepository } from '../repositories/ICustomerRepository.js';
import { IPlanRepository } from '../repositories/IPlanRepository.js';
import { IBillingCycleRepository } from '../repositories/IBillingCycleRepository.js';
import { Subscription } from '../../domain/entities/Subscription.js';
import { BillingCycle } from '../../domain/entities/BillingCycle.js';
import { Plan } from '../../domain/entities/Plan.js';
import { Customer } from '../../domain/entities/Customer.js';
import { generateKey } from '../../infrastructure/utils/uuid.js';
import Stripe from 'stripe';
import { NotFoundError, ValidationError, ConfigurationError, ConflictError } from '../errors/index.js';
import { SubscriptionStatus } from '../../domain/value-objects/SubscriptionStatus.js';
import { now } from '../../infrastructure/utils/date.js';

const SUBSCRIO_CUSTOMER_KEY_METADATA_KEYS = ['subscrioCustomerKey', 'subscrio_customer_key'];
const SUBSCRIO_SUBSCRIPTION_KEY_METADATA_KEYS = ['subscrioSubscriptionKey', 'subscrio_subscription_key'];

export class StripeIntegrationService {
  constructor(
    private readonly subscriptionRepository: ISubscriptionRepository,
    private readonly customerRepository: ICustomerRepository,
    private readonly planRepository: IPlanRepository,
    private readonly billingCycleRepository: IBillingCycleRepository,
    private readonly config?: { stripe?: { secretKey?: string } }
  ) {}

  /**
   * Process a verified Stripe event
   * NOTE: Signature verification MUST be done by implementor before calling this
   */
  async processStripeEvent(event: Stripe.Event): Promise<void> {
    // Event is already verified - process based on type
    switch (event.type) {
      case 'customer.created':
      case 'customer.updated':
        await this.handleCustomerUpsert(event.data.object as Stripe.Customer);
        break;

      case 'customer.deleted':
        await this.handleCustomerDeleted(event.data.object as Stripe.Customer | Stripe.DeletedCustomer);
        break;

      case 'customer.subscription.created':
        await this.handleSubscriptionCreated(
          event.data.object as Stripe.Subscription
        );
        break;

      case 'customer.subscription.updated':
        await this.handleSubscriptionUpdated(
          event.data.object as Stripe.Subscription
        );
        break;

      case 'customer.subscription.deleted':
        await this.handleSubscriptionDeleted(
          event.data.object as Stripe.Subscription
        );
        break;

      case 'invoice.payment_succeeded':
        await this.handlePaymentSucceeded(
          event.data.object as Stripe.Invoice
        );
        break;

      default:
        // Ignore unhandled event types
        // Log unhandled event types for debugging (remove in production)
        if (process.env.NODE_ENV === 'development') {
          console.log(`Unhandled Stripe event type: ${event.type}`);
        }
    }
  }

  private async handleSubscriptionCreated(
    stripeSubscription: Stripe.Subscription
  ): Promise<void> {
    const stripeCustomerId = this.extractCustomerId(stripeSubscription.customer);
    const customer = await this.resolveCustomer(
      stripeCustomerId,
      stripeSubscription.metadata
    );

    const { billingCycle, plan } = await this.resolvePlanFromSubscription(stripeSubscription);

    // First check if subscription already linked by Stripe ID
    const existingByStripeId = await this.subscriptionRepository.findByStripeId(stripeSubscription.id);
    if (existingByStripeId) {
      this.applyStripeSubscriptionFields(
        existingByStripeId, 
        customer.id!, 
        plan.id!, 
        billingCycle.id!, 
        stripeSubscription,
        billingCycle.props.externalProductId!
      );
      await this.subscriptionRepository.save(existingByStripeId);
      return;
    }

    // Check metadata for existing subscription to link
    const existingByMetadata = await this.findSubscriptionByMetadata(
      stripeSubscription.metadata,
      customer.id!
    );

    if (existingByMetadata) {
      // Link existing subscription to Stripe
      this.applyStripeSubscriptionFields(
        existingByMetadata, 
        customer.id!, 
        plan.id!, 
        billingCycle.id!, 
        stripeSubscription,
        billingCycle.props.externalProductId!
      );
      await this.subscriptionRepository.save(existingByMetadata);
      return;
    }

    // No existing subscription found, create new one
    const subscriptionKey =
      this.getMetadataValue(stripeSubscription.metadata, SUBSCRIO_SUBSCRIPTION_KEY_METADATA_KEYS) ??
      generateKey('sub');

    const activationDate = new Date(stripeSubscription.created * 1000);
    
    // Get period dates from matching subscription item
    const { periodStart, periodEnd } = this.getPeriodDatesFromStripeSubscription(
      stripeSubscription,
      billingCycle.props.externalProductId!
    );
    
    const currentPeriodStart = this.toDateOrDefault(periodStart, activationDate);
    const currentPeriodEnd = this.toDateOrUndefined(periodEnd) ??
      billingCycle.calculateNextPeriodEnd(currentPeriodStart) ??
      undefined;

    // Merge Stripe metadata with schedule ID
    const metadata = this.mergeScheduleIntoMetadata(
      stripeSubscription.metadata,
      stripeSubscription.schedule
    );

    const subscription = new Subscription({
      key: subscriptionKey,
      customerId: customer.id!,
      planId: plan.id!,
      billingCycleId: billingCycle.id!,
      status: this.mapStripeStatus(stripeSubscription.status),
      isArchived: false,
      activationDate,
      expirationDate: undefined,
      cancellationDate: stripeSubscription.canceled_at ? new Date(stripeSubscription.canceled_at * 1000) : undefined,
      trialEndDate: this.toDateOrUndefined(stripeSubscription.trial_end),
      currentPeriodStart,
      currentPeriodEnd,
      stripeSubscriptionId: stripeSubscription.id,
      featureOverrides: [],
      metadata,
      createdAt: now(),
      updatedAt: now()
    });

    await this.subscriptionRepository.save(subscription);
  }

  private async handleSubscriptionUpdated(
    stripeSubscription: Stripe.Subscription
  ): Promise<void> {
    const existing = await this.subscriptionRepository.findByStripeId(
      stripeSubscription.id
    );
    if (!existing) {
      // If not found, treat as creation
      return this.handleSubscriptionCreated(stripeSubscription);
    }

    const stripeCustomerId = this.extractCustomerId(stripeSubscription.customer);
    const customer = await this.resolveCustomer(
      stripeCustomerId,
      stripeSubscription.metadata
    );

    const { billingCycle, plan } = await this.resolvePlanFromSubscription(stripeSubscription);
    this.applyStripeSubscriptionFields(
      existing, 
      customer.id!, 
      plan.id!, 
      billingCycle.id!, 
      stripeSubscription,
      billingCycle.props.externalProductId!
    );
    await this.subscriptionRepository.save(existing);
  }

  private async handleSubscriptionDeleted(
    stripeSubscription: Stripe.Subscription
  ): Promise<void> {
    const subscription = await this.subscriptionRepository.findByStripeId(
      stripeSubscription.id
    );
    if (!subscription) {
      return; // Already deleted or never existed
    }

    subscription.expire();
    await this.subscriptionRepository.save(subscription);
  }

  private async handlePaymentSucceeded(
    stripeInvoice: Stripe.Invoice
  ): Promise<void> {
    if (!stripeInvoice.subscription) {
      return; // Not a subscription invoice
    }

    const subscription = await this.subscriptionRepository.findByStripeId(
      stripeInvoice.subscription as string
    );
    if (!subscription) {
      return; // Subscription not found
    }

    // Get the subscription's billing cycle to find matching line item
    const billingCycle = await this.billingCycleRepository.findById(subscription.props.billingCycleId);
    if (!billingCycle || !billingCycle.props.externalProductId) {
      return; // Can't match without externalProductId
    }

    // Find the invoice line item that matches this billing cycle's Stripe price ID
    const matchingLineItem = stripeInvoice.lines?.data?.find(
      line => line.price?.id === billingCycle.props.externalProductId
    );

    const period = matchingLineItem?.period ?? stripeInvoice.lines?.data?.[0]?.period;
    if (period) {
      subscription.props.currentPeriodStart = this.toDateOrDefault(
        period.start, 
        subscription.props.currentPeriodStart ?? now()
      );
      // Preserve existing value if period.end is not provided
      subscription.props.currentPeriodEnd = period.end !== undefined && period.end !== null
        ? new Date(period.end * 1000)
        : subscription.props.currentPeriodEnd;
    }

    subscription.props.updatedAt = now();
    await this.subscriptionRepository.save(subscription);
  }

  /**
   * Find billing cycle by Stripe price ID (stored in externalProductId)
   */
  private async findBillingCycleByStripePriceId(stripePriceId: string): Promise<BillingCycle | null> {
    // Search all billing cycles for one with matching externalProductId
    const allCycles = await this.billingCycleRepository.findAll();
    return allCycles.find(cycle => cycle.props.externalProductId === stripePriceId) || null;
  }

  /**
   * Create Stripe subscription from Subscrio data
   */
  async createStripeSubscription(
    customerKey: string,
    planKey: string,
    billingCycleKey: string,
    _stripePriceId: string
  ): Promise<Subscription> {
    // Find customer
    const customer = await this.customerRepository.findByKey(customerKey);
    if (!customer) {
      throw new NotFoundError(`Customer with key '${customerKey}' not found`);
    }

    if (!customer.externalBillingId) {
      throw new ValidationError('Customer must have external billing ID for Stripe integration');
    }

    // Find plan
    const plan = await this.planRepository.findByKey(planKey);
    if (!plan) {
      throw new NotFoundError(`Plan with key '${planKey}' not found`);
    }

    // Find billing cycle
    const billingCycle = await this.billingCycleRepository.findByKey(billingCycleKey);
    if (!billingCycle) {
      throw new NotFoundError(`Billing cycle with key '${billingCycleKey}' not found`);
    }

    // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
    // This would integrate with Stripe SDK to create the subscription
    // For now, creating a placeholder subscription
    // Create domain entity (no ID - database will generate)
    const subscription = new Subscription({
      key: generateKey('sub'),  // Auto-generate key for Stripe subscriptions
      customerId: customer.id!,
      planId: plan.id!,
      billingCycleId: billingCycle.id!,
      status: SubscriptionStatus.Active,  // Default status
      isArchived: false,
      activationDate: now(),
      currentPeriodStart: now(),
      currentPeriodEnd: billingCycle.calculateNextPeriodEnd(now()) ?? undefined,
      stripeSubscriptionId: `sub_placeholder_${Date.now()}`,
      featureOverrides: [],
      createdAt: now(),
      updatedAt: now()
    });

    // Save and get entity with generated ID
    const savedSubscription = await this.subscriptionRepository.save(subscription);
    return savedSubscription;
  }

  private async resolveCustomer(
    stripeCustomerId: string,
    metadata?: Stripe.Metadata | null
  ): Promise<Customer> {
    const existing = await this.customerRepository.findByExternalBillingId(stripeCustomerId);
    if (existing) {
      return existing;
    }

    const customerKey = this.getMetadataValue(metadata, SUBSCRIO_CUSTOMER_KEY_METADATA_KEYS);
    if (!customerKey) {
      throw new NotFoundError(
        `Customer not found for Stripe customer ID '${stripeCustomerId}'. ` +
        `Provide 'subscrioCustomerKey' metadata when creating Stripe customers or subscriptions.`
      );
    }

    const fallbackCustomer = await this.customerRepository.findByKey(customerKey);
    if (!fallbackCustomer) {
      throw new NotFoundError(
        `Customer with key '${customerKey}' not found while handling Stripe customer '${stripeCustomerId}'.`
      );
    }

    fallbackCustomer.setExternalBillingId(stripeCustomerId);
    return await this.customerRepository.save(fallbackCustomer);
  }

  private getMetadataValue(
    metadata: Stripe.Metadata | null | undefined,
    keys: string[]
  ): string | null {
    if (!metadata) {
      return null;
    }

    for (const key of keys) {
      const value = metadata[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value.trim();
      }
    }

    return null;
  }

  private extractCustomerId(
    customer: string | Stripe.Customer | Stripe.DeletedCustomer
  ): string {
    if (typeof customer === 'string') {
      return customer;
    }

    return customer.id;
  }

  private async resolvePlanFromSubscription(stripeSubscription: Stripe.Subscription): Promise<{
    billingCycle: BillingCycle;
    plan: Plan;
  }> {
    const firstItem = stripeSubscription.items.data[0];
    if (!firstItem || !firstItem.price || !firstItem.price.id) {
      throw new ValidationError('Stripe subscription payload is missing price information');
    }

    const stripePriceId = firstItem.price.id;
    const billingCycle = await this.findBillingCycleByStripePriceId(stripePriceId);
    if (!billingCycle) {
      throw new NotFoundError(
        `Billing cycle not found for Stripe price ID '${stripePriceId}'. ` +
        `Create a billing cycle with externalProductId='${stripePriceId}' to complete the mapping.`
      );
    }

    const plan = await this.planRepository.findById(billingCycle.props.planId);
    if (!plan || plan.id === undefined) {
      throw new NotFoundError(`Plan not found for billing cycle '${billingCycle.key}'`);
    }

    return { billingCycle, plan };
  }

  /**
   * Find the subscription item that matches the billing cycle's Stripe price ID
   * and extract period dates from it
   */
  private getPeriodDatesFromStripeSubscription(
    stripeSubscription: Stripe.Subscription,
    billingCycleExternalProductId: string
  ): { periodStart?: number; periodEnd?: number } {
    // Find the subscription item that matches this billing cycle's Stripe price ID
    const matchingItem = stripeSubscription.items?.data?.find(
      item => item.price?.id === billingCycleExternalProductId
    );
    
    // Get period dates from matching subscription item, or fall back to subscription level
    // Note: current_period_start/end exist on SubscriptionItem in API responses but may not be in TypeScript types
    const itemPeriodStart = matchingItem ? (matchingItem as unknown as { current_period_start?: number }).current_period_start : undefined;
    const itemPeriodEnd = matchingItem ? (matchingItem as unknown as { current_period_end?: number }).current_period_end : undefined;
    
    return {
      periodStart: itemPeriodStart ?? stripeSubscription.current_period_start,
      periodEnd: itemPeriodEnd ?? stripeSubscription.current_period_end
    };
  }

  private applyStripeSubscriptionFields(
    subscription: Subscription,
    customerId: number,
    planId: number,
    billingCycleId: number,
    stripeSubscription: Stripe.Subscription,
    billingCycleExternalProductId: string
  ) {
    subscription.props.customerId = customerId;
    subscription.props.planId = planId;
    subscription.props.billingCycleId = billingCycleId;
    subscription.props.activationDate = subscription.props.activationDate ?? new Date(stripeSubscription.created * 1000);
    
    // Get period dates from matching subscription item
    const { periodStart, periodEnd } = this.getPeriodDatesFromStripeSubscription(
      stripeSubscription,
      billingCycleExternalProductId
    );
    
    subscription.props.currentPeriodStart = this.toDateOrDefault(
      periodStart,
      subscription.props.currentPeriodStart ?? now()
    );
    
    // Preserve existing value if Stripe doesn't provide one
    subscription.props.currentPeriodEnd = periodEnd !== undefined && periodEnd !== null
      ? new Date(periodEnd * 1000)
      : subscription.props.currentPeriodEnd;
    subscription.props.trialEndDate = this.toDateOrUndefined(stripeSubscription.trial_end);
    subscription.props.cancellationDate = stripeSubscription.canceled_at
      ? new Date(stripeSubscription.canceled_at * 1000)
      : stripeSubscription.cancel_at_period_end
        ? subscription.props.cancellationDate
        : undefined;
    subscription.props.stripeSubscriptionId = stripeSubscription.id;
    subscription.props.metadata = this.mergeScheduleIntoMetadata(
      stripeSubscription.metadata,
      stripeSubscription.schedule,
      subscription.props.metadata
    );
    subscription.props.status = this.mapStripeStatus(stripeSubscription.status);
    subscription.props.updatedAt = now();
  }

  private mapStripeStatus(status: Stripe.Subscription.Status): SubscriptionStatus {
    switch (status) {
      case 'active':
        return SubscriptionStatus.Active;
      case 'trialing':
        return SubscriptionStatus.Trial;
      case 'canceled':
        return SubscriptionStatus.Cancelled;
      case 'past_due':
      case 'unpaid':
        return SubscriptionStatus.CancellationPending;
      case 'incomplete':
        return SubscriptionStatus.Pending;
      case 'incomplete_expired':
        return SubscriptionStatus.Expired;
      default:
        return SubscriptionStatus.Active;
    }
  }

  private toDateOrUndefined(timestamp?: number | null): Date | undefined {
    if (!timestamp) {
      return undefined;
    }

    return new Date(timestamp * 1000);
  }

  private toDateOrDefault(timestamp: number | null | undefined, fallback: Date): Date {
    if (!timestamp) {
      return fallback;
    }

    return new Date(timestamp * 1000);
  }

  /**
   * Merge Stripe schedule ID into subscription metadata
   * If schedule is present, adds it to metadata as 'stripeScheduleId'
   * If schedule is null/undefined, removes it from metadata
   */
  private mergeScheduleIntoMetadata(
    stripeMetadata: Stripe.Metadata | null | undefined,
    schedule: string | Stripe.SubscriptionSchedule | null | undefined,
    existingMetadata?: Record<string, unknown>
  ): Record<string, unknown> | undefined {
    // Merge existing metadata with Stripe metadata (Stripe metadata takes precedence)
    const merged: Record<string, unknown> = existingMetadata 
      ? { ...existingMetadata }
      : {};

    // Overlay Stripe metadata on top
    if (stripeMetadata) {
      Object.assign(merged, stripeMetadata);
    }

    // Extract schedule ID (could be string or object)
    const scheduleId = typeof schedule === 'string' 
      ? schedule 
      : schedule?.id 
        ? schedule.id 
        : null;

    // Add or remove schedule ID from metadata
    if (scheduleId) {
      merged.stripeScheduleId = scheduleId;
    } else {
      // Remove schedule ID if it exists
      delete merged.stripeScheduleId;
    }

    // Return undefined if metadata is empty, otherwise return the merged object
    return Object.keys(merged).length > 0 ? merged : undefined;
  }

  private async handleCustomerUpsert(
    stripeCustomer: Stripe.Customer
  ): Promise<void> {
    await this.resolveCustomer(stripeCustomer.id, stripeCustomer.metadata);
  }

  private async handleCustomerDeleted(
    stripeCustomer: Stripe.Customer | Stripe.DeletedCustomer
  ): Promise<void> {
    const existing = await this.customerRepository.findByExternalBillingId(stripeCustomer.id);
    if (!existing) {
      return;
    }

    existing.setExternalBillingId(undefined);
    await this.customerRepository.save(existing);
  }

  /**
   * Find subscription by metadata (subscription key)
   * Verifies the subscription belongs to the specified customer
   */
  private async findSubscriptionByMetadata(
    metadata: Stripe.Metadata | null | undefined,
    customerId: number
  ): Promise<Subscription | null> {
    // Look up by subscription key (external ID)
    const subscriptionKey = this.getMetadataValue(
      metadata,
      SUBSCRIO_SUBSCRIPTION_KEY_METADATA_KEYS
    );

    if (subscriptionKey) {
      const subscription = await this.subscriptionRepository.findByKey(subscriptionKey);
      // Verify it belongs to the customer
      if (subscription && subscription.props.customerId === customerId) {
        return subscription;
      }
    }

    return null;
  }

  /**
   * Get Stripe client instance
   * Uses provided secret key or falls back to config
   */
  private getStripeClient(secretKey?: string): Stripe {
    const key = secretKey || this.config?.stripe?.secretKey;
    if (!key) {
      throw new ConfigurationError(
        'Stripe secret key is required. Provide it in config.stripe.secretKey or pass stripeSecretKey parameter.'
      );
    }
    return new Stripe(key, {
      apiVersion: '2023-10-16'
    });
  }

  /**
   * Create a Stripe Checkout Session URL for subscription purchase
   * 
   * This helper method:
   * - Ensures the customer exists in Stripe (creates if needed)
   * - Sets proper metadata for webhook linking
   * - Supports linking to existing Subscrio subscriptions
   * - Provides full access to Stripe Checkout options
   */
  async createCheckoutSession(params: {
    customerKey: string;
    billingCycleKey: string;
    subscriptionKey?: string;  // Optional: existing subscription key to update
    stripeSecretKey?: string;  // Optional: override config Stripe key
    successUrl: string;
    cancelUrl: string;
    // Convenience options
    quantity?: number;
    customerEmail?: string;
    customerName?: string;
    allowPromotionCodes?: boolean;
    billingAddressCollection?: 'auto' | 'required';
    paymentMethodTypes?: Stripe.Checkout.SessionCreateParams.PaymentMethodType[];
    trialPeriodDays?: number;
    metadata?: Record<string, string>;  // Additional custom metadata
    // Full Stripe API access
    stripeOptions?: Partial<Stripe.Checkout.SessionCreateParams>;
  }): Promise<{ url: string; sessionId: string }> {
    // Get Stripe client
    const stripe = this.getStripeClient(params.stripeSecretKey);

    // Find customer
    const customer = await this.customerRepository.findByKey(params.customerKey);
    if (!customer) {
      throw new NotFoundError(`Customer with key '${params.customerKey}' not found`);
    }

    // Find billing cycle
    const billingCycle = await this.billingCycleRepository.findByKey(params.billingCycleKey);
    if (!billingCycle) {
      throw new NotFoundError(`Billing cycle with key '${params.billingCycleKey}' not found`);
    }

    // Validate billing cycle has Stripe price ID
    if (!billingCycle.props.externalProductId) {
      throw new ValidationError(
        `Billing cycle '${params.billingCycleKey}' does not have externalProductId set. ` +
        `Set it to a Stripe price ID to enable checkout.`
      );
    }

    // If subscription key provided, verify it exists and belongs to customer
    if (params.subscriptionKey) {
      const existingSubscription = await this.subscriptionRepository.findByKey(params.subscriptionKey);
      if (!existingSubscription) {
        throw new NotFoundError(`Subscription with key '${params.subscriptionKey}' not found`);
      }
      if (existingSubscription.props.customerId !== customer.id!) {
        throw new ConflictError(
          `Subscription '${params.subscriptionKey}' does not belong to customer '${params.customerKey}'`
        );
      }
    }

    // Ensure Stripe customer exists
    let stripeCustomerId = customer.externalBillingId;
    if (!stripeCustomerId) {
      // Create Stripe customer
      const stripeCustomer = await stripe.customers.create({
        email: params.customerEmail,
        name: params.customerName,
        metadata: {
          subscrioCustomerKey: customer.key
        }
      });
      stripeCustomerId = stripeCustomer.id;

      // Update customer with Stripe ID
      customer.setExternalBillingId(stripeCustomerId);
      await this.customerRepository.save(customer);
    } else {
      // Update existing Stripe customer metadata if needed
      try {
        await stripe.customers.update(stripeCustomerId, {
          email: params.customerEmail,
          name: params.customerName,
          metadata: {
            subscrioCustomerKey: customer.key
          }
        });
      } catch (error) {
        // If customer doesn't exist in Stripe, create it
        if (error instanceof Stripe.errors.StripeError && error.code === 'resource_missing') {
          const stripeCustomer = await stripe.customers.create({
            email: params.customerEmail,
            name: params.customerName,
            metadata: {
              subscrioCustomerKey: customer.key
            }
          });
          stripeCustomerId = stripeCustomer.id;
          customer.setExternalBillingId(stripeCustomerId);
          await this.customerRepository.save(customer);
        } else {
          throw error;
        }
      }
    }

    // Build metadata for checkout session
    const sessionMetadata: Record<string, string> = {
      subscrioCustomerKey: customer.key,
      ...(params.subscriptionKey && {
        subscrioSubscriptionKey: params.subscriptionKey
      }),
      ...params.metadata
    };

    // Build line items
    const lineItems: Stripe.Checkout.SessionCreateParams.LineItem[] = [{
      price: billingCycle.props.externalProductId,
      quantity: params.quantity ?? 1
    }];

    // Build subscription data metadata (this goes on the subscription, not the session)
    const subscriptionMetadata: Record<string, string> = {
      subscrioCustomerKey: customer.key,
      ...(params.subscriptionKey && {
        subscrioSubscriptionKey: params.subscriptionKey
      }),
      ...params.metadata
    };

    // Build base checkout session params
    // Note: Cannot specify both 'customer' and 'customer_email' - we always use 'customer' since we ensure customer exists
    const sessionParams: Stripe.Checkout.SessionCreateParams = {
      customer: stripeCustomerId,
      mode: 'subscription',
      line_items: lineItems,
      success_url: params.successUrl,
      cancel_url: params.cancelUrl,
      metadata: sessionMetadata,
      subscription_data: {
        metadata: subscriptionMetadata,
        ...(params.trialPeriodDays && { trial_period_days: params.trialPeriodDays })
      },
      // Do NOT set customer_email when customer is set - Stripe doesn't allow both
      // If customer_email was provided, it was already used when creating/updating the Stripe customer
      ...(params.allowPromotionCodes !== undefined && { allow_promotion_codes: params.allowPromotionCodes }),
      ...(params.billingAddressCollection && { billing_address_collection: params.billingAddressCollection }),
      ...(params.paymentMethodTypes && { payment_method_types: params.paymentMethodTypes }),
      // Merge in any additional Stripe options
      ...params.stripeOptions
    };

    // Create checkout session
    const session = await stripe.checkout.sessions.create(sessionParams);

    if (!session.url) {
      throw new ValidationError('Stripe checkout session was created but did not return a URL');
    }

    return {
      url: session.url,
      sessionId: session.id
    };
  }

}
