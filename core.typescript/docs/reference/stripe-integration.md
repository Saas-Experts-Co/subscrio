# Stripe Integration Service Reference

## Service Overview
`StripeIntegrationService` connects verified Stripe webhook events to Subscrio customers and subscriptions. Your infrastructure is responsible for signature verification—call `processStripeEvent` only after Stripe’s SDK validates the payload. The built-in handlers now cover customer lifecycle, subscription lifecycle, successful invoices, and a helper for bootstrapping Subscrio subscriptions that reference Stripe customers/prices.

- Store the Subscrio customer key in Stripe metadata (`subscrioCustomerKey`) whenever you create a Stripe customer or subscription—this allows webhooks to backfill `Customer.externalBillingId` automatically.
- Persist Stripe customer IDs in `Customer.externalBillingId`. If it’s missing, the webhook handler will backfill it using the metadata described above.
- Persist Stripe price IDs in `BillingCycle.externalProductId` so events can map to billing cycles (and therefore plans). Once the price is mapped, Subscrio can create and update subscriptions with no additional customization.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const stripeService = subscrio.stripe;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `processStripeEvent` | Entry point for verified Stripe webhook events | `Promise<void>` |
| `createStripeSubscription` | Helper to create a Subscrio subscription tied to Stripe metadata | `Promise<Subscription>` |
| `createCheckoutSession` | Generate Stripe Checkout URL with automatic customer creation and subscription linking | `Promise<{ url: string; sessionId: string }>` |

*(Handlers invoked internally by `processStripeEvent` are described for completeness.)*

## Method Reference

### processStripeEvent

#### Description
Routes a verified `Stripe.Event` to the appropriate handler (subscription lifecycle or invoice events).

#### Signature
```typescript
processStripeEvent(event: Stripe.Event): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `event` | `Stripe.Event` | Yes | Stripe webhook payload that your endpoint already verified. |

#### Returns
`Promise<void>`

#### Expected Results
- Switches on `event.type` and handles:
  - `customer.created`
  - `customer.updated`
  - `customer.deleted`
  - `customer.subscription.created`
  - `customer.subscription.updated`
  - `customer.subscription.deleted`
  - `invoice.payment_succeeded`
- Unhandled event types are ignored (logged in development).
- Missing entities (customer, plan, billing cycle, subscription) throw so you can fix data mapping.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer, billing cycle, plan, or subscription cannot be resolved. |
| `ValidationError` | Required data such as `externalBillingId` is missing. |

#### Example
```typescript
import Stripe from 'stripe';

const event = stripe.webhooks.constructEvent(body, sig, webhookSecret);
await stripeService.processStripeEvent(event);
```

#### Internal Handlers
- **`handleCustomerCreated/Updated`**
  - Resolves existing customers by `externalBillingId` or `subscrioCustomerKey` metadata and backfills the Stripe customer ID.
- **`handleCustomerDeleted`**
  - Clears `externalBillingId` when Stripe deletes a customer.
- **`handleSubscriptionCreated`**
  - Resolves the customer, billing cycle (via `externalProductId`), and owning plan.
  - **Subscription Linking Behavior:**
    1. First checks if a subscription with the Stripe subscription ID already exists (already linked).
    2. If metadata contains `subscrioSubscriptionKey`, looks up the existing Subscrio subscription by key.
    3. If found and belongs to the customer, **updates the existing subscription** (links Stripe ID, updates plan/billing cycle, preserves feature overrides).
    4. If no existing subscription found, **creates a new subscription**.
  - Sets `stripeSubscriptionId`, period dates, and metadata on the subscription.
- **`handleSubscriptionUpdated`**
  - Syncs plan/billing-cycle changes, period dates, and cancellation status for an existing subscription.
- **`handleSubscriptionDeleted`**
  - Calls `subscription.expire()` when Stripe marks the subscription deleted so the computed status becomes `expired`.
- **`handlePaymentSucceeded`**
  - Updates the subscription’s `currentPeriodStart`/`currentPeriodEnd` from the invoice line’s billing period after a successful payment.

### createStripeSubscription

#### Description
Bootstraps a Subscrio subscription linked to Stripe metadata (customer/billing-cycle) while you build deeper automation.

#### Signature
```typescript
createStripeSubscription(
  customerKey: string,
  planKey: string,
  billingCycleKey: string,
  stripePriceId: string
): Promise<Subscription>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `customerKey` | `string` | Yes | Customer key (must have `externalBillingId`). |
| `planKey` | `string` | Yes | Plan being subscribed to. |
| `billingCycleKey` | `string` | Yes | Billing cycle governing the cadence. |
| `stripePriceId` | `string` | Yes | Stripe price identifier (stored for reference). |

#### Returns
`Promise<Subscription>` – the saved domain subscription (with placeholder `stripeSubscriptionId`).

#### Expected Results
- Validates entities and ensures customer has `externalBillingId`.
- Generates a subscription key (or use one you pass in metadata), sets activation/current period timestamps, and saves the subscription.
- Leaves a placeholder `stripeSubscriptionId` for later reconciliation—webhooks will attach the real Stripe subscription once you pass the metadata described above.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer, plan, or billing cycle missing. |
| `ValidationError` | Customer lacks `externalBillingId`. |

#### Example
```typescript
const sub = await stripeService.createStripeSubscription(
  'cust_123',
  'pro-plan',
  'pro-plan-annual',
  'price_ABC123'
);
console.log(sub.key);
```

### createCheckoutSession

#### Description
Generates a Stripe Checkout Session URL for subscription purchases. This helper method:
- **Automatically creates Stripe customers** if they don't exist (sets proper metadata)
- **Supports linking to existing Subscrio subscriptions** via `subscriptionKey` parameter
- Provides full access to Stripe Checkout options including quantity, trial periods, and custom metadata
- Sets all required metadata for webhook reconciliation

When a customer completes checkout, the webhook handler will:
- If `subscriptionKey` was provided: **update the existing subscription** (link Stripe ID, update plan/billing cycle)
- If no `subscriptionKey`: **create a new subscription**

#### Signature
```typescript
createCheckoutSession(params: {
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
}): Promise<{ url: string; sessionId: string }>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `customerKey` | `string` | Yes | Subscrio customer key. Stripe customer will be created if missing. |
| `billingCycleKey` | `string` | Yes | Billing cycle key (must have `externalProductId` set to Stripe price ID). |
| `subscriptionKey` | `string` | No | Existing Subscrio subscription key to link/update. If provided, webhook will update this subscription instead of creating new. |
| `stripeSecretKey` | `string` | No | Stripe secret key (overrides `config.stripe.secretKey`). |
| `successUrl` | `string` | Yes | URL to redirect after successful checkout. |
| `cancelUrl` | `string` | Yes | URL to redirect if checkout is cancelled. |
| `quantity` | `number` | No | Subscription quantity (default: 1). |
| `customerEmail` | `string` | No | Pre-fill customer email in checkout. |
| `customerName` | `string` | No | Pre-fill customer name in checkout. |
| `allowPromotionCodes` | `boolean` | No | Enable promotion code input in checkout. |
| `billingAddressCollection` | `'auto' \| 'required'` | No | Control billing address collection. |
| `paymentMethodTypes` | `PaymentMethodType[]` | No | Restrict allowed payment methods. |
| `trialPeriodDays` | `number` | No | Set trial period duration in days. |
| `metadata` | `Record<string, string>` | No | Additional custom metadata to pass through. |
| `stripeOptions` | `Partial<SessionCreateParams>` | No | Full Stripe API access for any checkout option. |

#### Returns
`Promise<{ url: string; sessionId: string }>` – Checkout URL to redirect user and session ID for tracking.

#### Expected Results
- **Stripe Customer Creation**: If customer doesn't have `externalBillingId`, creates Stripe customer automatically with `subscrioCustomerKey` metadata.
- **Metadata Setup**: Sets `subscrioCustomerKey` and optionally `subscrioSubscriptionKey` in both session and subscription metadata.
- **Subscription Linking**: If `subscriptionKey` provided, validates subscription exists and belongs to customer. Webhook will update this subscription when checkout completes.
- **Full Stripe Support**: All Stripe Checkout options accessible via convenience parameters or `stripeOptions` for complete API access.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Stripe secret key not provided (neither in config nor parameter). |
| `NotFoundError` | Customer, billing cycle, or subscription (if key provided) not found. |
| `ValidationError` | Billing cycle missing `externalProductId`, invalid URLs, etc. |
| `ConflictError` | Subscription key provided but doesn't belong to customer. |

#### Example: New Subscription
```typescript
// Create checkout for new subscription
const { url, sessionId } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-monthly',
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel',
  quantity: 2,
  allowPromotionCodes: true,
  customerEmail: 'user@example.com'
});

// Redirect user to url
window.location.href = url;
```

#### Example: Update Existing Subscription
```typescript
// Create checkout to update existing subscription (change plan/billing cycle)
const { url, sessionId } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-annual',  // New billing cycle
  subscriptionKey: 'sub_456',      // Existing subscription to update
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel'
});

// When checkout completes, webhook will update subscription 'sub_456'
// with new plan/billing cycle and link Stripe subscription ID
```

#### Example: Full Stripe API Access
```typescript
// Use stripeOptions for any Stripe Checkout parameter
const { url } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-monthly',
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel',
  stripeOptions: {
    consent_collection: {
      terms_of_service: 'required'
    },
    phone_number_collection: {
      enabled: true
    },
    custom_fields: [
      {
        key: 'company_name',
        label: { type: 'custom', custom: 'Company Name' },
        type: 'text'
      }
    ]
  }
});
```

## Related Workflows
- **Webhook verification** – Your HTTP endpoint must verify Stripe signatures with `stripe.webhooks.constructEvent` (or equivalent) before calling `processStripeEvent`.
- **Customer metadata** – Attach `subscrioCustomerKey` (and optionally `subscrioSubscriptionKey`) to every Stripe customer and subscription you create so Subscrio can reconcile records automatically.
- **Billing-cycle mapping** – Store Stripe price IDs in `BillingCycle.externalProductId` to map subscriptions/billing cycles accurately.
- **Reference guide** – See `docs/reference/how-to-integrate-with-stripe.md` for an end-to-end walkthrough that covers setup, metadata, and webhook handling.
