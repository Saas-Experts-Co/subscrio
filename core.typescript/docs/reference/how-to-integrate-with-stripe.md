# How to Integrate with Stripe

This guide walks through the exact steps needed to keep Subscrio and Stripe in sync. The library already contains all of the handlers—your job is to provide the right metadata, map Stripe IDs to Subscrio records, and forward verified events to `subscrio.stripe.processStripeEvent`.

## Prerequisites

- `@subscrio/core` installed and configured with a reachable PostgreSQL database.
- `STRIPE_SECRET_KEY` available to your server-side code (never expose it to browsers).
- Ability to configure Stripe webhooks and create customers/subscriptions via the Stripe API or dashboard.

## 1. Map Stripe prices to billing cycles

Subscrio derives the plan automatically by looking up `BillingCycle.externalProductId`. For every Stripe price ID you sell, create (or update) a billing cycle and store the price ID:

```ts
await subscrio.billingCycles.createBillingCycle({
  planKey: 'basic-plan',
  key: 'basic-monthly',
  displayName: 'Basic – Monthly',
  durationValue: 1,
  durationUnit: 'months',
  externalProductId: 'price_12345'
});
```

## 2. Attach Subscrio metadata when creating Stripe entities

Every Stripe customer **must** include the Subscrio customer key in metadata so webhooks can backfill `externalBillingId`:

- `subscrioCustomerKey` – required when `Customer.externalBillingId` is blank.
- `subscrioSubscriptionKey` – optional; use it to **link to an existing Subscrio subscription**. When provided in subscription metadata, the webhook handler will **update the existing subscription** instead of creating a new one.

### Subscription Linking Behavior

When Stripe sends a `customer.subscription.created` webhook, Subscrio handles it as follows:

1. **Check for existing link**: First checks if a subscription with the Stripe subscription ID already exists (already linked).
2. **Check metadata for existing subscription**: If metadata contains `subscrioSubscriptionKey`, looks up the existing Subscrio subscription by key.
3. **Update existing subscription**: If found and belongs to the customer, **updates the existing subscription**:
   - Links the Stripe subscription ID
   - Updates plan/billing cycle if changed
   - Preserves existing feature overrides
   - Updates period dates and status from Stripe
4. **Create new subscription**: If no existing subscription found, creates a new subscription.

This allows you to:
- **Upgrade/downgrade subscriptions**: Pass existing `subscriptionKey` when creating checkout to update the subscription
- **Link Stripe subscriptions to existing Subscrio subscriptions**: Use metadata to connect Stripe subscriptions to subscriptions created outside Stripe

### Manual Metadata Setup

Example when creating a Stripe customer:

```ts
await stripe.customers.create({
  email: 'user@example.com',
  metadata: {
    subscrioCustomerKey: 'customer_acme_corp'
  }
});
```

When you create a Stripe subscription (through Checkout, Billing Portal, or the API), make sure the subscription metadata includes:
- `subscrioCustomerKey` (required)
- `subscrioSubscriptionKey` (optional, to link to existing subscription)

## 3. Receiving and verifying webhooks

Your HTTP endpoint must:

1. Read the **raw** request body (do not JSON-parse first).
2. Verify the signature with `stripe.webhooks.constructEvent`.
3. Pass the verified event to `subscrio.stripe.processStripeEvent`.

```ts
import express from 'express';
import Stripe from 'stripe';
import { Subscrio } from '@subscrio/core';

const app = express();
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY!, { apiVersion: '2023-10-16' });
const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });

app.post('/webhooks/stripe',
  express.raw({ type: 'application/json' }),
  async (req, res) => {
    try {
      const sig = req.headers['stripe-signature']!;
      const event = stripe.webhooks.constructEvent(req.body, sig, process.env.STRIPE_WEBHOOK_SECRET!);
      await subscrio.stripe.processStripeEvent(event);
      res.json({ received: true });
    } catch (error) {
      console.error('Stripe webhook error:', error);
      res.status(400).json({ error: 'Invalid webhook payload' });
    }
  }
);
```

Never call `processStripeEvent` with unverified JSON—Subscrio assumes the payload is genuine once it reaches the service.

## 4. Required Stripe events

Subscribe to the following event types in the Stripe dashboard (or CLI). They are the only ones that mutate Subscrio data:

| Event | What Subscrio does |
| --- | --- |
| `customer.created` / `customer.updated` | Looks up customers by `externalBillingId` or `subscrioCustomerKey` metadata and backfills the Stripe customer ID. |
| `customer.deleted` | Clears `externalBillingId` so future provisioning can recreate the link. |
| `customer.subscription.created` | Resolves the mapped billing cycle/plan. If metadata contains `subscrioSubscriptionKey`, updates existing subscription; otherwise creates new subscription. |
| `customer.subscription.updated` | Updates plan/billing-cycle references, trial dates, cancellation status, and billing periods. |
| `customer.subscription.deleted` | Expires the subscription locally. |
| `invoice.payment_succeeded` | Updates `currentPeriodStart`/`currentPeriodEnd` from the invoice line period once payment clears. |

## 5. Data requirements for security and mapping

- **Database** – ensure every billing cycle that can be sold via Stripe has `externalProductId` set to the Stripe price ID.
- **Metadata** – always include `subscrioCustomerKey` (and optionally `subscrioSubscriptionKey`). Missing metadata causes `NotFoundError` so you can detect misconfigured flows quickly.
- **Secrets** – keep `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` in server-side environment variables only.

## 6. Creating Checkout Sessions (Recommended)

The easiest way to create Stripe Checkout links is using `subscrio.stripe.createCheckoutSession()`. This method:
- **Automatically creates Stripe customers** if they don't exist
- Sets all required metadata for webhook reconciliation
- Supports linking to existing subscriptions for upgrades/downgrades

### Basic Usage

```ts
// Create checkout for new subscription
const { url, sessionId } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-monthly',
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel'
});

// Redirect user to checkout
window.location.href = url;
```

### Updating Existing Subscriptions

To upgrade/downgrade an existing subscription, pass the `subscriptionKey`:

```ts
// Update existing subscription (change plan/billing cycle)
const { url } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-annual',      // New billing cycle
  subscriptionKey: 'sub_456',          // Existing subscription to update
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel'
});

// When checkout completes, webhook will:
// 1. Find subscription 'sub_456' via metadata
// 2. Update it with new plan/billing cycle
// 3. Link the Stripe subscription ID
```

### Stripe Customer Creation

**Important**: `createCheckoutSession` automatically creates Stripe customers if they don't exist. You don't need to create them manually.

- If customer has `externalBillingId`: Uses existing Stripe customer
- If customer doesn't have `externalBillingId`: Creates new Stripe customer with `subscrioCustomerKey` metadata
- Updates customer email/name if provided in parameters

### Full Feature Access

The method supports all Stripe Checkout options:

```ts
const { url } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-monthly',
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel',
  quantity: 2,                        // Subscription quantity
  customerEmail: 'user@example.com',  // Pre-fill email
  allowPromotionCodes: true,          // Enable promo codes
  trialPeriodDays: 14,                // 14-day trial
  metadata: {                         // Custom metadata
    campaign: 'summer2024'
  },
  stripeOptions: {                    // Full Stripe API access
    phone_number_collection: { enabled: true },
    consent_collection: { terms_of_service: 'required' }
  }
});
```

### Stripe Secret Key

The Stripe secret key can be provided in two ways:
1. **Config**: Set `config.stripe.secretKey` when creating Subscrio instance
2. **Parameter**: Pass `stripeSecretKey` to `createCheckoutSession` (takes precedence)

```ts
// Option 1: Via config
const subscrio = new Subscrio({
  database: { connectionString: process.env.DATABASE_URL! },
  stripe: { secretKey: process.env.STRIPE_SECRET_KEY! }
});

// Option 2: Via parameter (overrides config)
const { url } = await subscrio.stripe.createCheckoutSession({
  customerKey: 'customer_123',
  billingCycleKey: 'pro-monthly',
  stripeSecretKey: process.env.STRIPE_SECRET_KEY!,  // Override
  successUrl: 'https://yourapp.com/success',
  cancelUrl: 'https://yourapp.com/cancel'
});
```

## 7. End-to-end flow summary

### Flow 1: New Subscription via Checkout (Recommended)

1. Create the customer in Subscrio (stores the canonical customer key).
2. Call `createCheckoutSession()` - **automatically creates Stripe customer** if needed.
3. Redirect user to checkout URL.
4. User completes checkout in Stripe.
5. Stripe emits `customer.subscription.created` webhook.
6. Subscrio webhook handler **creates new subscription** and links Stripe subscription ID.
7. Future renewals/cancellations sync via webhooks.

### Flow 2: Update Existing Subscription via Checkout

1. Customer has existing Subscrio subscription (not linked to Stripe).
2. Call `createCheckoutSession()` with `subscriptionKey` parameter.
3. Redirect user to checkout URL.
4. User completes checkout in Stripe.
5. Stripe emits `customer.subscription.created` webhook with `subscrioSubscriptionKey` metadata.
6. Subscrio webhook handler **finds existing subscription** by key and **updates it**:
   - Links Stripe subscription ID
   - Updates plan/billing cycle
   - Preserves feature overrides
7. Future renewals/cancellations sync via webhooks.

### Flow 3: Manual Stripe Integration

1. Create the customer in Subscrio (stores the canonical customer key).
2. Create the same customer in Stripe, passing `subscrioCustomerKey` metadata.
3. Stripe sends `customer.created`; Subscrio records the Stripe customer ID.
4. Create/checkout a Stripe subscription whose price matches `BillingCycle.externalProductId` and includes metadata:
   - `subscrioCustomerKey` (required)
   - `subscrioSubscriptionKey` (optional, to link to existing subscription)
5. Stripe emits `customer.subscription.created`; Subscrio:
   - If `subscrioSubscriptionKey` provided: **updates existing subscription**
   - If no key provided: **creates new subscription**
6. Whenever Stripe renews or cancels, the corresponding subscription/invoice webhooks keep Subscrio's data up to date.

With these steps in place, Subscrio automatically mirrors Stripe's lifecycle without any REST API calls—everything stays in process, type-safe, and consistent.


