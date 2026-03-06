# Configuration Sync Service Reference

The Configuration Sync Service allows you to define all products, features, plans, and billing cycles in a single JSON configuration file or programmatically, then sync them to the database. This is ideal for version-controlled configuration management and infrastructure-as-code workflows.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const configSync = subscrio.configSync;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var configSync = subscrio.ConfigSync;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `syncFromFile` | Loads configuration from a JSON file and syncs to the database | `Promise<ConfigSyncReport>` |
    | `syncFromJson` | Syncs configuration from a ConfigSyncDto object | `Promise<ConfigSyncReport>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `SyncFromFileAsync` | Loads configuration from a JSON file and syncs to the database | `Task<ConfigSyncReport>` |
    | `SyncFromJsonAsync` | Syncs configuration from a ConfigSyncDto object | `Task<ConfigSyncReport>` |

## Overview

The sync service:
- **Creates** entities that don't exist in the database
- **Updates** existing entities with new values
- **Archives/Unarchives** entities based on the `archived` flag
- **Syncs associations** (product-feature, plan-feature values)
- **Ignores** entities not in the config (leaves them unchanged)
- **Validates** all references and data types before syncing

## Quick Start

### Option 1: Sync from JSON File

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({
      database: { connectionString: process.env.DATABASE_URL! }
    });

    // Sync from a JSON file
    const report = await subscrio.configSync.syncFromFile('./config.json');

    console.log(`Created: ${report.created.features} features, ${report.created.products} products`);
    console.log(`Updated: ${report.updated.features} features, ${report.updated.products} products`);
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);

    var report = await subscrio.ConfigSync.SyncFromFileAsync("./config.json");

    Console.WriteLine($"Created: {report.Created.Features} features, {report.Created.Products} products");
    Console.WriteLine($"Updated: {report.Updated.Features} features, {report.Updated.Products} products");
    ```

### Option 2: Sync from Programmatic Config

=== "TypeScript"
    ```typescript
    import { Subscrio, ConfigSyncDto } from 'core.typescript';

    const subscrio = new Subscrio({
      database: { connectionString: process.env.DATABASE_URL! }
    });

    const config: ConfigSyncDto = {
      version: '1.0',
      features: [
        { key: 'max-projects', displayName: 'Maximum Projects', valueType: 'numeric', defaultValue: '10' }
      ],
      products: [
        {
          key: 'project-management',
          displayName: 'Project Management',
          features: ['max-projects'],
          plans: [
            {
              key: 'basic',
              displayName: 'Basic Plan',
              featureValues: { 'max-projects': '5' },
              billingCycles: [
                { key: 'monthly', displayName: 'Monthly', durationValue: 1, durationUnit: 'months' }
              ]
            }
          ]
        }
      ]
    };

    const report = await subscrio.configSync.syncFromJson(config);
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;
    using Subscrio.Core.Application.DTOs;

    var subscrio = new Subscrio(config);

    var config = new ConfigSyncDto(
        Version: "1.0",
        Features: new List<FeatureConfig>
        {
            new FeatureConfig("max-projects", "Maximum Projects", ValueType: "numeric", DefaultValue: "10")
        },
        Products: new List<ProductConfig>
        {
            new ProductConfig(
                "project-management",
                "Project Management",
                Features: new List<string> { "max-projects" },
                Plans: new List<PlanConfig>
                {
                    new PlanConfig(
                        "basic",
                        "Basic Plan",
                        FeatureValues: new Dictionary<string, string> { ["max-projects"] = "5" },
                        BillingCycles: new List<BillingCycleConfig>
                        {
                            new BillingCycleConfig("monthly", "Monthly", DurationValue: 1, DurationUnit: "months")
                        }
                    )
                }
            )
        }
    );

    var report = await subscrio.ConfigSync.SyncFromJsonAsync(config);
    ```

### Option 3: Initial config at construction

You can pass the same config sync input to the Subscrio constructor via `initialConfig` (TypeScript) or `InitialConfig` (.NET). After construction, call `runInitialConfigSync()` / `RunInitialConfigSyncAsync()` to apply it (e.g. after installing or verifying the schema).

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({
      database: { connectionString: process.env.DATABASE_URL! },
      initialConfig: { type: 'file', filePath: './config.json' }
      // or: initialConfig: { type: 'json', config: myConfigSyncDto }
    });
    await subscrio.installSchema();
    const report = await subscrio.runInitialConfigSync(); // applies initial config, or null if none
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;
    using Subscrio.Core.Config;

    var subscrio = new Subscrio(new SubscrioConfig
    {
        Database = new DatabaseConfig { ConnectionString = "..." },
        InitialConfig = new InitialConfigOptions { FilePath = "./config.json" }
        // or: InitialConfig = new InitialConfigOptions { Config = myConfigSyncDto }
    });
    await subscrio.InstallSchemaAsync();
    var report = await subscrio.RunInitialConfigSyncAsync(); // applies initial config, or null if none
    ```

## JSON Schema

### Root Configuration

**CRITICAL: The `features` array MUST appear before the `products` array in JSON files.**

=== "TypeScript / JSON"
    ```json
    {
      "version": "1.0",
      "features": [...],
      "products": [...]
    }
    ```

=== ".NET / JSON"
    ```json
    {
      "version": "1.0",
      "features": [...],
      "products": [...]
    }
    ```
    Use `ConfigSyncDto` with `SyncFromJsonAsync()` or load from file with `SyncFromFileAsync()`.

### Feature Configuration

=== "TypeScript"
    ```typescript
    interface FeatureConfig {
      key: string;                    // Required, globally unique, immutable
      displayName: string;            // Required
      description?: string;           // Optional
      valueType: 'toggle' | 'numeric' | 'text';  // Required
      defaultValue: string;           // Required, validated against valueType
      groupName?: string;             // Optional
      validator?: Record<string, unknown>;  // Optional
      metadata?: Record<string, unknown>;  // Optional
      archived?: boolean;             // Optional, defaults to false
    }
    ```

=== ".NET"
    ```csharp
    public record FeatureConfig(
        string Key,
        string DisplayName,
        string? Description = null,
        string ValueType = "toggle",
        string DefaultValue = "false",
        string? GroupName = null,
        Dictionary<string, object?>? Validator = null,
        Dictionary<string, object?>? Metadata = null,
        bool? Archived = null
    );
    ```

**Example:**

```json
{
  "key": "max-projects",
  "displayName": "Maximum Projects",
  "description": "Maximum number of projects allowed",
  "valueType": "numeric",
  "defaultValue": "1",
  "groupName": "Limits",
  "archived": false
}
```

### Product Configuration

=== "TypeScript"
    ```typescript
    interface ProductConfig {
      key: string;                    // Required, globally unique, immutable
      displayName: string;            // Required
      description?: string;           // Optional
      metadata?: Record<string, unknown>;  // Optional
      archived?: boolean;             // Optional, defaults to false
      features?: string[];            // Optional, array of feature keys
      plans?: PlanConfig[];          // Optional, nested plans
    }
    ```

=== ".NET"
    ```csharp
    public record ProductConfig(
        string Key,
        string DisplayName,
        string? Description = null,
        Dictionary<string, object?>? Metadata = null,
        bool? Archived = null,
        List<string>? Features = null,
        List<PlanConfig>? Plans = null
    );
    ```

**Example:**

```json
{
  "key": "project-management",
  "displayName": "Project Management",
  "description": "Complete project management solution",
  "archived": false,
  "features": ["max-projects", "team-size", "gantt-charts"],
  "plans": [...]
}
```

### Plan Configuration

=== "TypeScript"
    ```typescript
    interface PlanConfig {
      key: string;                    // Required, unique within product, immutable
      displayName: string;            // Required
      description?: string;           // Optional
      onExpireTransitionToBillingCycleKey?: string;  // Optional, must reference billing cycle in any plan within same product
      metadata?: Record<string, unknown>;  // Optional
      archived?: boolean;             // Optional, defaults to false
      featureValues?: Record<string, string>;  // Optional, feature key -> value mapping
      billingCycles?: BillingCycleConfig[];  // Optional, nested billing cycles
    }
    ```

=== ".NET"
    ```csharp
    public record PlanConfig(
        string Key,
        string DisplayName,
        string? Description = null,
        string? OnExpireTransitionToBillingCycleKey = null,
        Dictionary<string, string>? FeatureValues = null,
        List<BillingCycleConfig>? BillingCycles = null,
        Dictionary<string, object?>? Metadata = null,
        bool? Archived = null
    );
    ```

**Example:**

```json
{
  "key": "basic",
  "displayName": "Basic Plan",
  "description": "For small teams",
  "archived": false,
  "featureValues": {
    "max-projects": "5",
    "gantt-charts": "false"
  },
  "billingCycles": [...]
}
```

### Billing Cycle Configuration

=== "TypeScript"
    ```typescript
    interface BillingCycleConfig {
      key: string;                    // Required, unique within plan, immutable
      displayName: string;            // Required
      description?: string;           // Optional
      durationValue?: number;          // Required if durationUnit !== 'forever'
      durationUnit: 'days' | 'weeks' | 'months' | 'years' | 'forever';  // Required
      externalProductId?: string;      // Optional, e.g., Stripe price ID
      archived?: boolean;             // Optional, defaults to false
    }
    ```

=== ".NET"
    ```csharp
    public record BillingCycleConfig(
        string Key,
        string DisplayName,
        string? Description = null,
        int? DurationValue = null,
        string DurationUnit = "days",
        string? ExternalProductId = null,
        bool? Archived = null
    );
    ```

**Example:**

```json
{
  "key": "monthly",
  "displayName": "Monthly",
  "description": "Monthly billing cycle",
  "durationValue": 1,
  "durationUnit": "months",
  "externalProductId": "price_stripe_monthly",
  "archived": false
}
```

## Complete Example

The same JSON config file works for both TypeScript and .NET:

```json
{
  "version": "1.0",
  "features": [
    {
      "key": "max-projects",
      "displayName": "Maximum Projects",
      "description": "Maximum number of projects allowed",
      "valueType": "numeric",
      "defaultValue": "1",
      "groupName": "Limits"
    },
    {
      "key": "gantt-charts",
      "displayName": "Gantt Charts",
      "description": "Enable Gantt chart visualization",
      "valueType": "toggle",
      "defaultValue": "false",
      "groupName": "Features"
    }
  ],
  "products": [
    {
      "key": "project-management",
      "displayName": "Project Management",
      "description": "Complete project management solution",
      "archived": false,
      "features": ["max-projects", "gantt-charts"],
      "plans": [
        {
          "key": "basic",
          "displayName": "Basic Plan",
          "description": "For small teams",
          "archived": false,
          "featureValues": {
            "max-projects": "5",
            "gantt-charts": "false"
          },
          "billingCycles": [
            {
              "key": "monthly",
              "displayName": "Monthly",
              "durationValue": 1,
              "durationUnit": "months",
              "archived": false
            },
            {
              "key": "yearly",
              "displayName": "Yearly",
              "durationValue": 1,
              "durationUnit": "years",
              "archived": false
            }
          ]
        },
        {
          "key": "pro",
          "displayName": "Pro Plan",
          "description": "For growing teams",
          "archived": false,
          "featureValues": {
            "max-projects": "50",
            "gantt-charts": "true"
          },
          "billingCycles": [
            {
              "key": "monthly",
              "displayName": "Monthly",
              "durationValue": 1,
              "durationUnit": "months",
              "externalProductId": "price_stripe_monthly",
              "archived": false
            }
          ]
        }
      ]
    }
  ]
}
```

## Sync Behavior

### Create Operations

Entities in the config that don't exist in the database are created:
- Features are created first (independent entities)
- Products are created next
- Plans are created for each product
- Billing cycles are created for each plan

### Update Operations

Entities that exist in both config and database are updated:
- Only fields specified in the config are updated
- Keys are immutable and cannot be changed
- Archive status is handled separately (see below)

### Archive Operations

The `archived` boolean property controls entity status:

- **`archived: true`** - Sets entity status to `archived`
- **`archived: false`** or **omitted** - Sets entity status to `active`

Archive operations use the entity's `archive()` and `unarchive()` methods, ensuring business rules are followed.

### Association Sync

**Product-Feature Associations:**
- Features listed in `product.features` are associated
- Features not listed are dissociated
- Only features explicitly in the config are synced

**Plan Feature Values:**
- Feature values in `plan.featureValues` are set
- Feature values not in config are removed
- Values are validated against feature `valueType`

### Ignore Behavior

Entities in the database but **not** in the config are:
- **Completely ignored** - no changes made
- **Counted** in the sync report's `ignored` section
- **Left unchanged** - status, associations, and values remain as-is

This allows partial syncs where you only update specific entities.

## Sync Report

The sync service returns a detailed report:

=== "TypeScript"
    ```typescript
    interface ConfigSyncReport {
      created: {
        features: number;
        products: number;
        plans: number;
        billingCycles: number;
      };
      updated: {
        features: number;
        products: number;
        plans: number;
        billingCycles: number;
      };
      archived: {
        features: number;
        products: number;
        plans: number;
        billingCycles: number;
      };
      unarchived: {
        features: number;
        products: number;
        plans: number;
        billingCycles: number;
      };
      ignored: {
        features: number;
        products: number;
        plans: number;
        billingCycles: number;
      };
      errors: Array<{
        entityType: 'feature' | 'product' | 'plan' | 'billingCycle';
        key: string;
        message: string;
      }>;
      warnings: Array<{
        entityType: 'feature' | 'product' | 'plan' | 'billingCycle';
        key: string;
        message: string;
      }>;
    }
    ```

=== ".NET"
    ```csharp
    public record ConfigSyncCounts(int Features, int Products, int Plans, int BillingCycles);

    public record ConfigSyncError(string EntityType, string Key, string Message);

    public record ConfigSyncWarning(string EntityType, string Key, string Message);

    public record ConfigSyncReport(
        ConfigSyncCounts Created,
        ConfigSyncCounts Updated,
        ConfigSyncCounts Archived,
        ConfigSyncCounts Unarchived,
        ConfigSyncCounts Ignored,
        List<ConfigSyncError> Errors,
        List<ConfigSyncWarning> Warnings
    );
    ```

**Example Usage:**

=== "TypeScript"
    ```typescript
    const report = await subscrio.configSync.syncFromJson(config);

    if (report.errors.length > 0) {
      console.error('Sync errors:');
      report.errors.forEach(error => {
        console.error(`  ${error.entityType} ${error.key}: ${error.message}`);
      });
    }

    console.log(`Sync complete: ${report.created.features} features created`);
    ```

=== ".NET"
    ```csharp
    var report = await subscrio.ConfigSync.SyncFromJsonAsync(config);

    if (report.Errors.Count > 0)
    {
        Console.Error.WriteLine("Sync errors:");
        foreach (var error in report.Errors)
        {
            Console.Error.WriteLine($"  {error.EntityType} {error.Key}: {error.Message}");
        }
    }

    Console.WriteLine($"Sync complete: {report.Created.Features} features created");
    ```

## Validation

The sync service performs comprehensive validation:

### Schema Validation
- All required fields are present
- Field types match expected types
- String lengths within limits
- Enum values are valid

### JSON Property Order
- **CRITICAL**: `features` must appear before `products` in JSON files
- Validation throws `ValidationError` if order is incorrect

### Duplicate Key Validation
- Feature keys must be globally unique
- Product keys must be globally unique
- Plan keys must be unique within product
- Billing cycle keys must be unique within plan

### Reference Validation
- All feature keys referenced in products must exist in features array
- All feature keys in `plan.featureValues` must be associated with the product
- `onExpireTransitionToBillingCycleKey` must reference a valid billing cycle in any plan within the same product

### Feature Value Validation
- Toggle features: values must be `"true"` or `"false"`
- Numeric features: values must be valid numbers
- Text features: any string value is accepted

## Error Handling

### Validation Errors

Validation errors are thrown before any sync operations:

=== "TypeScript"
    ```typescript
    try {
      await subscrio.configSync.syncFromJson(config);
    } catch (error) {
      if (error instanceof ValidationError) {
        console.error('Validation failed:', error.message);
        console.error('Details:', error.errors);
      }
    }
    ```

=== ".NET"
    ```csharp
    try
    {
        await subscrio.ConfigSync.SyncFromJsonAsync(config);
    }
    catch (Subscrio.Core.Application.Errors.ValidationException ex)
    {
        Console.Error.WriteLine($"Validation failed: {ex.Message}");
    }
    ```

### Sync Errors

Errors during sync operations are collected in the report:

=== "TypeScript"
    ```typescript
    const report = await subscrio.configSync.syncFromJson(config);

    if (report.errors.length > 0) {
      // Some operations failed, but others may have succeeded
      // Operations are idempotent, so you can re-run sync
      console.error('Some operations failed:', report.errors);
    }
    ```

=== ".NET"
    ```csharp
    var report = await subscrio.ConfigSync.SyncFromJsonAsync(config);

    if (report.Errors.Count > 0)
    {
        // Some operations failed, but others may have succeeded
        // Operations are idempotent, so you can re-run sync
        Console.Error.WriteLine("Some operations failed:");
        foreach (var err in report.Errors)
            Console.Error.WriteLine($"  {err.EntityType} {err.Key}: {err.Message}");
    }
    ```

### Partial Completion

Since operations are **idempotent**, if an error occurs:
1. Completed operations remain in the database
2. Failed operations are reported in `report.errors`
3. You can re-run sync to complete remaining operations

## Best Practices

### 1. Version Control Your Config

Store configuration files in version control:
```bash
config/
├── production.json
├── staging.json
└── development.json
```

### 2. Use Programmatic Config for Dynamic Generation

=== "TypeScript"
    ```typescript
    function generateConfig(environment: string): ConfigSyncDto {
      const baseFeatures = [...];
      const environmentFeatures = getEnvironmentFeatures(environment);
      
      return {
        version: '1.0',
        features: [...baseFeatures, ...environmentFeatures],
        products: [...]
      };
    }

    await subscrio.configSync.syncFromJson(generateConfig('production'));
    ```

=== ".NET"
    ```csharp
    ConfigSyncDto GenerateConfig(string environment)
    {
        var baseFeatures = new List<FeatureConfig> { /* ... */ };
        var environmentFeatures = GetEnvironmentFeatures(environment);
        return new ConfigSyncDto(
            Version: "1.0",
            Features: baseFeatures.Concat(environmentFeatures).ToList(),
            Products: new List<ProductConfig> { /* ... */ }
        );
    }

    await subscrio.ConfigSync.SyncFromJsonAsync(GenerateConfig("production"));
    ```

### 3. Validate Before Production

Always validate your config before syncing to production:

=== "TypeScript"
    ```typescript
    import { ConfigSyncDtoSchema } from 'core.typescript';

    try {
      const config = ConfigSyncDtoSchema.parse(jsonData);
      // Config is valid, safe to sync
      await subscrio.configSync.syncFromJson(config);
    } catch (error) {
      console.error('Config validation failed:', error);
      process.exit(1);
    }
    ```

=== ".NET"
    ```csharp
    try
    {
        var config = JsonSerializer.Deserialize<ConfigSyncDto>(jsonData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (config == null) throw new InvalidOperationException("Invalid config");
        await subscrio.ConfigSync.SyncFromJsonAsync(config);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Config validation failed: {ex.Message}");
        Environment.Exit(1);
    }
    ```

### 4. Handle Errors Gracefully

=== "TypeScript"
    ```typescript
    const report = await subscrio.configSync.syncFromJson(config);

    if (report.errors.length > 0) {
      // Log errors but don't fail the entire process
      logger.error('Sync completed with errors', { errors: report.errors });
      
      // Optionally re-run for failed operations
      if (shouldRetry(report.errors)) {
        await subscrio.configSync.syncFromJson(config);
      }
    }
    ```

=== ".NET"
    ```csharp
    var report = await subscrio.ConfigSync.SyncFromJsonAsync(config);

    if (report.Errors.Count > 0)
    {
        foreach (var err in report.Errors)
            Console.Error.WriteLine($"  {err.EntityType} {err.Key}: {err.Message}");
        if (ShouldRetry(report.Errors))
        {
            await subscrio.ConfigSync.SyncFromJsonAsync(config);
        }
    }
    ```

### 5. Use Partial Syncs

Only include entities you want to update:

=== "TypeScript"
    ```typescript
    // Only update features, leave products unchanged
    const partialConfig: ConfigSyncDto = {
      version: '1.0',
      features: [
        { key: 'new-feature', displayName: 'New Feature', valueType: 'toggle', defaultValue: 'false' }
      ],
      products: []  // Empty - products won't be touched
    };

    await subscrio.configSync.syncFromJson(partialConfig);
    ```

=== ".NET"
    ```csharp
    // Only update features, leave products unchanged
    var partialConfig = new ConfigSyncDto(
        Version: "1.0",
        Features: new List<FeatureConfig>
        {
            new FeatureConfig("new-feature", "New Feature", ValueType: "toggle", DefaultValue: "false")
        },
        Products: new List<ProductConfig>()  // Empty - products won't be touched
    );

    await subscrio.ConfigSync.SyncFromJsonAsync(partialConfig);
    ```

### 6. Archive Instead of Delete

Use `archived: true` to mark entities as archived rather than deleting them:

```json
{
  "key": "old-feature",
  "displayName": "Old Feature",
  "valueType": "toggle",
  "defaultValue": "false",
  "archived": true
}
```

## Common Patterns

### Feature Flags

=== "TypeScript"
    ```typescript
    const config: ConfigSyncDto = {
      version: '1.0',
      features: [
        {
          key: 'beta-feature',
          displayName: 'Beta Feature',
          valueType: 'toggle',
          defaultValue: 'false'
        }
      ],
      products: [
        {
          key: 'main-product',
          displayName: 'Main Product',
          features: ['beta-feature'],
          plans: [
            {
              key: 'premium',
              displayName: 'Premium',
              featureValues: {
                'beta-feature': 'true'  // Enable for premium plan
              }
            }
          ]
        }
      ]
    };
    ```

=== ".NET"
    ```csharp
    var config = new ConfigSyncDto(
        Version: "1.0",
        Features: new List<FeatureConfig>
        {
            new FeatureConfig("beta-feature", "Beta Feature", ValueType: "toggle", DefaultValue: "false")
        },
        Products: new List<ProductConfig>
        {
            new ProductConfig(
                "main-product",
                "Main Product",
                Features: new List<string> { "beta-feature" },
                Plans: new List<PlanConfig>
                {
                    new PlanConfig(
                        "premium",
                        "Premium",
                        FeatureValues: new Dictionary<string, string> { ["beta-feature"] = "true" }
                    )
                }
            )
        }
    );
    ```

### Tiered Plans

=== "TypeScript"
    ```typescript
    const config: ConfigSyncDto = {
      version: '1.0',
      features: [
        { key: 'storage-gb', displayName: 'Storage (GB)', valueType: 'numeric', defaultValue: '1' }
      ],
      products: [
        {
          key: 'storage-product',
          displayName: 'Storage Product',
          features: ['storage-gb'],
          plans: [
            {
              key: 'basic',
              displayName: 'Basic',
              featureValues: { 'storage-gb': '10' }
            },
            {
              key: 'pro',
              displayName: 'Pro',
              featureValues: { 'storage-gb': '100' }
            },
            {
              key: 'enterprise',
              displayName: 'Enterprise',
              featureValues: { 'storage-gb': '1000' }
            }
          ]
        }
      ]
    };
    ```

=== ".NET"
    ```csharp
    var config = new ConfigSyncDto(
        Version: "1.0",
        Features: new List<FeatureConfig>
        {
            new FeatureConfig("storage-gb", "Storage (GB)", ValueType: "numeric", DefaultValue: "1")
        },
        Products: new List<ProductConfig>
        {
            new ProductConfig(
                "storage-product",
                "Storage Product",
                Features: new List<string> { "storage-gb" },
                Plans: new List<PlanConfig>
                {
                    new PlanConfig("basic", "Basic", FeatureValues: new Dictionary<string, string> { ["storage-gb"] = "10" }),
                    new PlanConfig("pro", "Pro", FeatureValues: new Dictionary<string, string> { ["storage-gb"] = "100" }),
                    new PlanConfig("enterprise", "Enterprise", FeatureValues: new Dictionary<string, string> { ["storage-gb"] = "1000" })
                }
            )
        }
    );
    ```

## Limitations

1. **No Delete Operations**: Entities can only be archived, not deleted
2. **Immutable Keys**: Keys cannot be changed after creation
3. **Sequential Operations**: Operations run sequentially (not in a transaction)
4. **Partial Completion**: If an error occurs, some operations may have completed

## Troubleshooting

### "features must appear before products" Error

**Problem**: JSON property order is incorrect.

**Solution**: Ensure `features` array comes before `products` array in your JSON file.

### "Feature key 'X' referenced in product does not exist" Error

**Problem**: Product references a feature that's not in the features array.

**Solution**: Add the feature to the `features` array, or remove it from `product.features`.

### "Invalid feature value for numeric type" Error

**Problem**: Plan feature value doesn't match feature's valueType.

**Solution**: Ensure numeric features have numeric values, toggle features have "true"/"false".

### Sync Report Shows Errors

**Problem**: Some operations failed during sync.

**Solution**: 
1. Check `report.errors` for details
2. Fix the issues in your config
3. Re-run sync (operations are idempotent)

## Method Reference

### syncFromFile

#### Description
Loads configuration from a JSON file, validates it, and syncs products, features, plans, and billing cycles to the database. The `features` array must appear before the `products` array in the JSON file.

=== "TypeScript"
    #### Signature
    ```typescript
    syncFromFile(filePath: string): Promise<ConfigSyncReport>
    ```

    #### Inputs
    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filePath` | `string` | Yes | Path to the JSON configuration file. |

    #### Returns
    `Promise<ConfigSyncReport>` – sync report with counts for created, updated, archived, unarchived, and ignored entities.

    #### Return Properties
    | Field | Type | Description |
    | --- | --- | --- |
    | `created` | `{ features, products, plans, billingCycles }` | Counts of entities created. |
    | `updated` | `{ features, products, plans, billingCycles }` | Counts of entities updated. |
    | `archived` | `{ features, products, plans, billingCycles }` | Counts of entities archived. |
    | `unarchived` | `{ features, products, plans, billingCycles }` | Counts of entities unarchived. |
    | `ignored` | `{ features, products, plans, billingCycles }` | Counts of entities not in config (left unchanged). |
    | `errors` | `Array<{ message, entityType?, key? }>` | Errors encountered during sync. |
    | `warnings` | `string[]` | Non-fatal warnings. |

    #### Example
    ```typescript
    const report = await subscrio.configSync.syncFromFile('./config.json');
    console.log(`Created: ${report.created.features} features, ${report.created.products} products`);
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ConfigSyncReport> SyncFromFileAsync(string filePath)
    ```

    #### Inputs
    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filePath` | `string` | Yes | Path to the JSON configuration file. |

    #### Returns
    `Task<ConfigSyncReport>` – sync report with counts for created, updated, archived, unarchived, and ignored entities.

    #### Return Properties
    | Property | Type | Description |
    | --- | --- | --- |
    | `Created` | `ConfigSyncCounts` | Counts of entities created. |
    | `Updated` | `ConfigSyncCounts` | Counts of entities updated. |
    | `Archived` | `ConfigSyncCounts` | Counts of entities archived. |
    | `Unarchived` | `ConfigSyncCounts` | Counts of entities unarchived. |
    | `Ignored` | `ConfigSyncCounts` | Counts of entities not in config (left unchanged). |
    | `Errors` | `List<ConfigSyncError>` | Errors encountered during sync. |
    | `Warnings` | `List<string>` | Non-fatal warnings. |

    #### Example
    ```csharp
    var report = await subscrio.ConfigSync.SyncFromFileAsync("./config.json");
    Console.WriteLine($"Created: {report.Created.Features} features, {report.Created.Products} products");
    ```

#### Expected Results
- Reads and parses the JSON file.
- Validates JSON property order (`features` before `products`).
- Validates config schema and all references.
- Creates, updates, archives, and unarchives entities as needed.
- Returns a detailed report.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | File invalid, JSON property order incorrect, or config schema validation fails. |
| `Error` | File cannot be read (e.g., file not found). |

### syncFromJson

#### Description
Syncs configuration from a ConfigSyncDto object. Use when building config programmatically rather than loading from a file.

=== "TypeScript"
    #### Signature
    ```typescript
    syncFromJson(config: ConfigSyncDto): Promise<ConfigSyncReport>
    ```

    #### Inputs
    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `config` | `ConfigSyncDto` | Yes | Configuration object (features, products with nested plans and billing cycles). |

    #### Returns
    `Promise<ConfigSyncReport>` – same structure as `syncFromFile`.

    #### Return Properties
    Same as `syncFromFile` (see ConfigSyncReport).

    #### Example
    ```typescript
    const config: ConfigSyncDto = {
      version: '1.0',
      features: [{ key: 'max-projects', displayName: 'Max Projects', valueType: 'numeric', defaultValue: '10' }],
      products: [{ key: 'my-product', displayName: 'My Product', features: ['max-projects'], plans: [] }]
    };
    const report = await subscrio.configSync.syncFromJson(config);
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ConfigSyncReport> SyncFromJsonAsync(ConfigSyncDto config)
    ```

    #### Inputs
    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `config` | `ConfigSyncDto` | Yes | Configuration object (features, products with nested plans and billing cycles). |

    #### Returns
    `Task<ConfigSyncReport>` – same structure as `SyncFromFileAsync`.

    #### Return Properties
    Same as `SyncFromFileAsync` (see ConfigSyncReport).

    #### Example
    ```csharp
    var config = new ConfigSyncDto(
        Version: "1.0",
        Features: new List<FeatureConfig> { new("max-projects", "Max Projects", ValueType: "numeric", DefaultValue: "10") },
        Products: new List<ProductConfig> { new("my-product", "My Product", Features: new List<string> { "max-projects" }, Plans: new List<PlanConfig>()) }
    );
    var report = await subscrio.ConfigSync.SyncFromJsonAsync(config);
    ```

#### Expected Results
- Validates config schema and all references.
- Creates, updates, archives, and unarchives entities as needed.
- Returns a detailed report.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Config schema invalid or references break (e.g., plan references non-existent product). |

## See Also

- [Products Reference](./products.md)
- [Features Reference](./features.md)
- [Plans Reference](./plans.md)
- [Billing Cycles Reference](./billing-cycles.md)

