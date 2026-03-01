# Getting Started with Subscrio Core

> This tutorial walks through the minimum set of steps to bootstrap the Subscrio core library, define features/products/plans, onboard a customer, issue a subscription, and verify feature access. It is a distilled version of the full sample app—aimed at engineers who want a fast path to a working setup.

## Prerequisites

- Node.js 18+, PNPM or NPM.
- PostgreSQL connection string (same one used by the core library and admin app).
- Run `pnpm install` (or `npm install`) at the repo root:  
  ```bash
  pnpm install
  ```
- Optional: copy `core.typescript/.env.example` to `.env` and set `DATABASE_URL`.

## Step 1 – Initialize Subscrio

Create a file such as `scripts/bootstrap.ts`:

```typescript
import { Subscrio } from '@subscrio/core';
import { loadConfig } from '@subscrio/core/config';

async function main() {
  const config = loadConfig();
  const subscrio = new Subscrio(config);

  // Check if schema exists
  const schemaVersion = await subscrio.verifySchema();
  if (schemaVersion === null) {
    await subscrio.installSchema();
    console.log('Schema installed.');
  } else {
    console.log(`Schema version: ${schemaVersion}`);
    // NOTE: Only include this migration code if you want your application
    // to automatically run migrations at startup. Alternatively, run migrations
    // manually via CLI (npm run migrate) or as a separate deployment step.
    const migrationsApplied = await subscrio.migrate();
    if (migrationsApplied > 0) {
      console.log(`Applied ${migrationsApplied} migration(s).`);
    }
  }
  
  console.log('Ready to use Subscrio services.');
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
```

Run with `ts-node` or compile to JavaScript. This sets up the database tables and exposes the service collection on `subscrio`.

### Running Migrations

When you update the `@subscrio/core` package, you may need to run migrations to update your database schema. You can do this in two ways:

**Option 1: Programmatically**
```typescript
const migrationsApplied = await subscrio.migrate();
```

**Option 2: CLI Command**
```bash
npm run migrate
# or if installed globally
npx @subscrio/core migrate
```

The migration system tracks schema versions in the `system_config` table and only applies pending migrations, so it's safe to run multiple times.

## Step 2 – Define Features

Features are global definitions with typed defaults. Plans and subscriptions draw from them later.

> **Tip:** Define *all* keys—products, plans, billing cycles, features—as constants or in a shared module so both backend and admin UI reference the same strings. This prevents typos and makes refactors safer.

```typescript
const FEATURE_KEYS = {
  Analytics: 'analytics-dashboard',
  MaxProjects: 'max-projects'
} as const;

const analyticsFeature = await subscrio.features.createFeature({
  key: FEATURE_KEYS.Analytics,
  displayName: 'Analytics Dashboard',
  valueType: 'toggle',
  defaultValue: 'false'
});

const maxProjectsFeature = await subscrio.features.createFeature({
  key: FEATURE_KEYS.MaxProjects,
  displayName: 'Max Projects',
  valueType: 'numeric',
  defaultValue: '3'
});
```

## Step 3 – Create Product, Plan, and Billing Cycle

1. Create a product (using a constant for the key):
   ```typescript
   const PRODUCT = 'projecthub';

   const product = await subscrio.products.createProduct({
     key: PRODUCT,
     displayName: 'ProjectHub',
     description: 'Project management suite'
   });
   ```
2. Associate features with the product:
   ```typescript
   await subscrio.products.associateFeature(PRODUCT, FEATURE_KEYS.Analytics);
   await subscrio.products.associateFeature(PRODUCT, FEATURE_KEYS.MaxProjects);
   ```
3. Define a plan and billing cycle:
   ```typescript
    const PLAN = 'starter';

    await subscrio.plans.createPlan({
     productKey: PRODUCT,
     key: PLAN,
     displayName: 'Starter Plan',
     description: 'Best for small teams'
   });

   const BILLING_CYCLE = 'starter-monthly';

   await subscrio.billingCycles.createBillingCycle({
     planKey: PLAN,
     key: BILLING_CYCLE,
     displayName: 'Monthly',
     durationValue: 1,
     durationUnit: 'months'
   });
   ```
4. Set plan feature values:
   ```typescript
   await subscrio.plans.setFeatureValue(PLAN, FEATURE_KEYS.Analytics, 'true');
   await subscrio.plans.setFeatureValue(PLAN, FEATURE_KEYS.MaxProjects, '10');
   ```

## Step 4 – Onboard a Customer

```typescript
const customer = await subscrio.customers.createCustomer({
  key: 'acme-corp',
  displayName: 'Acme Corporation',
  email: 'admin@acme.test',
  externalBillingId: 'cus_123' // optional (Stripe ID, etc.)
});
```

## Step 5 – Issue a Subscription

Subscriptions tie the customer to a plan/billing cycle (and optionally contain overrides).

```typescript
const subscription = await subscrio.subscriptions.createSubscription({
  key: 'acme-subscription',
  customerKey: customer.key,
  billingCycleKey: BILLING_CYCLE,
  trialEndDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString()
});
```

Need a temporary override? For example, bump `max-projects` to `20` for a month:

```typescript
await subscrio.subscriptions.addFeatureOverride(
  subscription.key,
  FEATURE_KEYS.MaxProjects,
  '20',
  'temporary'
);
```

## Step 6 – Verify Feature Access

Use the Feature Checker service to evaluate the final resolved values.

```typescript
const maxProjects = await subscrio.featureChecker.getValueForCustomer<number>(
  customer.key,
  PRODUCT,
  FEATURE_KEYS.MaxProjects,
  0
);

const hasAnalytics = await subscrio.featureChecker.isEnabledForCustomer(
  customer.key,
  PRODUCT,
  FEATURE_KEYS.Analytics
);

console.log({ maxProjects, hasAnalytics });
```

Results obey the hierarchy: subscription override → plan value → feature default.

## Where to Go Next

- `core-overview.md` – service-by-service reference.
- `products.md`, `plans.md`, `billing-cycles.md` – deeper dives on catalog modeling.
- `subscriptions.md` & `subscription-lifecycle.md` – lifecycle rules.
- `feature-checker.md` – advanced feature resolution scenarios.
- `sample/` project – full demo covering trials, upgrades, overrides, and downgrades.

Once these steps succeed end-to-end, you can expand into Stripe integration, API key management, admin UI, and automated migrations. Happy building!

