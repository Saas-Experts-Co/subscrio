# Configuration Sync Service

The Configuration Sync Service allows you to define all products, features, plans, and billing cycles in a single JSON configuration file or programmatically, then sync them to the database. This is ideal for version-controlled configuration management and infrastructure-as-code workflows.

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

```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({
  database: { connectionString: process.env.DATABASE_URL! }
});

// Sync from a JSON file
const report = await subscrio.configSync.syncFromFile('./config.json');

console.log(`Created: ${report.created.features} features, ${report.created.products} products`);
console.log(`Updated: ${report.updated.features} features, ${report.updated.products} products`);
```

### Option 2: Sync from Programmatic Config

```typescript
import { Subscrio, ConfigSyncDto } from '@subscrio/core';

const subscrio = new Subscrio({
  database: { connectionString: process.env.DATABASE_URL! }
});

// Build config programmatically
const config: ConfigSyncDto = {
  version: '1.0',
  features: [
    {
      key: 'max-projects',
      displayName: 'Maximum Projects',
      valueType: 'numeric',
      defaultValue: '10'
    }
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
          featureValues: {
            'max-projects': '5'
          },
          billingCycles: [
            {
              key: 'monthly',
              displayName: 'Monthly',
              durationValue: 1,
              durationUnit: 'months'
            }
          ]
        }
      ]
    }
  ]
};

const report = await subscrio.configSync.syncFromJson(config);
```

## JSON Schema

### Root Configuration

**CRITICAL: The `features` array MUST appear before the `products` array in JSON files.**

```json
{
  "version": "1.0",
  "features": [...],
  "products": [...]
}
```

### Feature Configuration

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

**Example Usage:**
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

### Sync Errors

Errors during sync operations are collected in the report:

```typescript
const report = await subscrio.configSync.syncFromJson(config);

if (report.errors.length > 0) {
  // Some operations failed, but others may have succeeded
  // Operations are idempotent, so you can re-run sync
  console.error('Some operations failed:', report.errors);
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

### 3. Validate Before Production

Always validate your config before syncing to production:

```typescript
import { ConfigSyncDtoSchema } from '@subscrio/core';

try {
  const config = ConfigSyncDtoSchema.parse(jsonData);
  // Config is valid, safe to sync
  await subscrio.configSync.syncFromJson(config);
} catch (error) {
  console.error('Config validation failed:', error);
  process.exit(1);
}
```

### 4. Handle Errors Gracefully

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

### 5. Use Partial Syncs

Only include entities you want to update:

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

### Tiered Plans

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

## API Reference

### `syncFromFile(filePath: string): Promise<ConfigSyncReport>`

Loads configuration from a JSON file and syncs.

**Parameters:**
- `filePath`: Path to JSON configuration file

**Returns:** `Promise<ConfigSyncReport>`

**Throws:**
- `ValidationError` if file is invalid or JSON property order is incorrect
- `Error` if file cannot be read

### `syncFromJson(config: ConfigSyncDto): Promise<ConfigSyncReport>`

Syncs configuration from a ConfigSyncDto object.

**Parameters:**
- `config`: Configuration object (can be built programmatically)

**Returns:** `Promise<ConfigSyncReport>`

**Throws:**
- `ValidationError` if config is invalid

## See Also

- [Products Reference](./reference/products.md)
- [Features Reference](./reference/features.md)
- [Plans Reference](./reference/plans.md)
- [Billing Cycles Reference](./reference/billing-cycles.md)

