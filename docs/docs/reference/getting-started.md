# Getting Started with Subscrio Core

> This tutorial walks through the minimum set of steps to bootstrap the Subscrio core library, define features/products/plans, onboard a customer, issue a subscription, and verify feature access. It is a distilled version of the full sample app—aimed at engineers who want a fast path to a working setup.

## Prerequisites

- PostgreSQL connection string (same one used by the core library and admin app).

=== "TypeScript"
    - Node.js 18+, PNPM or NPM.
    - Run `pnpm install` (or `npm install`) at the repo root:

    ```bash
    pnpm install
    ```

    - Optional: copy `core.typescript/.env.example` to `.env` and set `DATABASE_URL`.

=== ".NET"
    - .NET 8.0+ SDK.
    - Add the `Subscrio.Core` NuGet package to your project.
    - Set `DATABASE_URL` environment variable (e.g., `Host=localhost;Port=5432;Database=subscrio;Username=postgres;Password=postgres`).

## Step 1 – Initialize Subscrio

=== "TypeScript"
    Create a file such as `scripts/bootstrap.ts`:

    ```typescript
    import { Subscrio } from 'core.typescript';
    import { loadConfig } from 'core.typescript/config';

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

    Run with `ts-node` or compile to JavaScript.

=== ".NET"
    Create a bootstrap method:

    ```csharp
    using Subscrio.Core;
    using Subscrio.Core.Config;

    var config = ConfigLoader.LoadConfig();
    await using var subscrio = new Subscrio(config);

    var schemaVersion = await subscrio.VerifySchemaAsync();
    if (schemaVersion == null)
    {
        await subscrio.InstallSchemaAsync();
        Console.WriteLine("Schema installed.");
    }
    else
    {
        Console.WriteLine($"Schema version: {schemaVersion}");
        var migrationsApplied = await subscrio.MigrateAsync();
        if (migrationsApplied > 0)
        {
            Console.WriteLine($"Applied {migrationsApplied} migration(s).");
        }
    }

    Console.WriteLine("Ready to use Subscrio services.");
    ```

    Use `await using` (or `Dispose()` when done) to ensure proper cleanup.

### Running Migrations

When you update the Subscrio package, you may need to run migrations to update your database schema. The migration system tracks schema versions in the `system_config` table and only applies pending migrations, so it's safe to run multiple times. You can run migrations programmatically:

=== "TypeScript"
    ```typescript
    const migrationsApplied = await subscrio.migrate();
    ```

    Or via CLI: `npm run migrate` or `npx subscrio migrate`

=== ".NET"
    ```csharp
    var migrationsApplied = await subscrio.MigrateAsync();
    ```

## Step 2 – Define Features

Features are global definitions with typed defaults. Plans and subscriptions draw from them later.

> **Tip:** Define *all* keys—products, plans, billing cycles, features—as constants or in a shared module so both backend and admin UI reference the same strings. This prevents typos and makes refactors safer.

=== "TypeScript"
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

=== ".NET"
    ```csharp
    const string FEATURE_ANALYTICS = "analytics-dashboard";
    const string FEATURE_MAX_PROJECTS = "max-projects";

    await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
        Key: FEATURE_ANALYTICS,
        DisplayName: "Analytics Dashboard",
        ValueType: "toggle",
        DefaultValue: "false"
    ));

    await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
        Key: FEATURE_MAX_PROJECTS,
        DisplayName: "Max Projects",
        ValueType: "numeric",
        DefaultValue: "3"
    ));
    ```

## Step 3 – Create Product, Plan, and Billing Cycle

Create a product (with a constant key), associate features, define a plan and billing cycle, then set plan feature values:

=== "TypeScript"
    ```typescript
    const PRODUCT = 'projecthub';

    const product = await subscrio.products.createProduct({
      key: PRODUCT,
      displayName: 'ProjectHub',
      description: 'Project management suite'
    });

    await subscrio.products.associateFeature(PRODUCT, FEATURE_KEYS.Analytics);
    await subscrio.products.associateFeature(PRODUCT, FEATURE_KEYS.MaxProjects);

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

    await subscrio.plans.setFeatureValue(PLAN, FEATURE_KEYS.Analytics, 'true');
    await subscrio.plans.setFeatureValue(PLAN, FEATURE_KEYS.MaxProjects, '10');
    ```

=== ".NET"
    ```csharp
    const string PRODUCT = "projecthub";

    var product = await subscrio.Products.CreateProductAsync(new CreateProductDto(
        Key: PRODUCT,
        DisplayName: "ProjectHub",
        Description: "Project management suite"
    ));

    await subscrio.Products.AssociateFeatureAsync(PRODUCT, FEATURE_ANALYTICS);
    await subscrio.Products.AssociateFeatureAsync(PRODUCT, FEATURE_MAX_PROJECTS);

    const string PLAN = "starter";
    const string BILLING_CYCLE = "starter-monthly";

    await subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
        ProductKey: PRODUCT,
        Key: PLAN,
        DisplayName: "Starter Plan",
        Description: "Best for small teams"
    ));

    await subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
        PlanKey: PLAN,
        Key: BILLING_CYCLE,
        DisplayName: "Monthly",
        DurationValue: 1,
        DurationUnit: "months"
    ));

    await subscrio.Plans.SetFeatureValueAsync(PLAN, FEATURE_ANALYTICS, "true");
    await subscrio.Plans.SetFeatureValueAsync(PLAN, FEATURE_MAX_PROJECTS, "10");
    ```

## Step 4 – Onboard a Customer

=== "TypeScript"
    ```typescript
    const customer = await subscrio.customers.createCustomer({
      key: 'acme-corp',
      displayName: 'Acme Corporation',
      email: 'admin@acme.test',
      externalBillingId: 'cus_123' // optional (Stripe ID, etc.)
    });
    ```

=== ".NET"
    ```csharp
    var customer = await subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
        Key: "acme-corp",
        DisplayName: "Acme Corporation",
        Email: "admin@acme.test",
        ExternalBillingId: "cus_123"  // optional (Stripe ID, etc.)
    ));
    ```

## Step 5 – Issue a Subscription

Subscriptions tie the customer to a plan/billing cycle (and optionally contain overrides).

=== "TypeScript"
    ```typescript
    const subscription = await subscrio.subscriptions.createSubscription({
      key: 'acme-subscription',
      customerKey: customer.key,
      billingCycleKey: BILLING_CYCLE,
      trialEndDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString()
    });
    ```

=== ".NET"
    ```csharp
    var subscription = await subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
        Key: "acme-subscription",
        CustomerKey: customer.Key,
        BillingCycleKey: BILLING_CYCLE,
        TrialEndDate: DateTime.UtcNow.AddDays(14)
    ));
    ```

Need a temporary override? For example, bump `max-projects` to `20` for a month:

===! "TypeScript"
    ```typescript
    await subscrio.subscriptions.addFeatureOverride(
      subscription.key,
      FEATURE_KEYS.MaxProjects,
      '20',
      'temporary'
    );
    ```

=== ".NET"
    ```csharp
    await subscrio.Subscriptions.AddFeatureOverrideAsync(
        subscription.Key,
        FEATURE_MAX_PROJECTS,
        "20",
        OverrideType.Temporary
    );
    ```

## Step 6 – Verify Feature Access

Use the Feature Checker service to evaluate the final resolved values.

=== "TypeScript"
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

=== ".NET"
    ```csharp
    var maxProjects = await subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
        customer.Key,
        PRODUCT,
        FEATURE_MAX_PROJECTS,
        "0"
    );

    var hasAnalytics = await subscrio.FeatureChecker.IsEnabledForCustomerAsync(
        customer.Key,
        PRODUCT,
        FEATURE_ANALYTICS
    );

    Console.WriteLine($"Max projects: {maxProjects}, Has analytics: {hasAnalytics}");
    ```

Results obey the hierarchy: subscription override → plan value → feature default.

## Where to Go Next

- `core-overview.md` – service-by-service reference.
- `products.md`, `plans.md`, `billing-cycles.md` – deeper dives on catalog modeling.
- `subscriptions.md` & `subscription-lifecycle.md` – lifecycle rules.
- `feature-checker.md` – advanced feature resolution scenarios.
- `sample/` project – full demo covering trials, upgrades, overrides, and downgrades.

Once these steps succeed end-to-end, you can expand into Stripe integration, API key management, admin UI, and automated migrations. Happy building!

