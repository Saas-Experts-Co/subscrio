# Feature Management Service Reference

## Service Overview
The Feature Management Service defines feature flags and typed configuration values that products expose to plans and subscriptions. Features are global and reusable across products once explicitly associated, and they always participate in the resolution hierarchy: subscription override → plan value → feature default.

- Feature keys are immutable and globally unique.
- `valueType` determines how defaults, plan values, and overrides are validated (`toggle`, `numeric`, `text`).
- Features cannot be deleted while referenced by products, plan feature values, or subscription overrides.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const features = subscrio.features;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `createFeature` | Validates and stores a new global feature | `Promise<FeatureDto>` |
| `updateFeature` | Updates mutable fields on an existing feature | `Promise<FeatureDto>` |
| `getFeature` | Retrieves a feature by key | `Promise<FeatureDto | null>` |
| `listFeatures` | Lists features with filters | `Promise<FeatureDto[]>` |
| `archiveFeature` | Archives a feature | `Promise<void>` |
| `unarchiveFeature` | Restores an archived feature | `Promise<void>` |
| `deleteFeature` | Deletes an archived, unreferenced feature | `Promise<void>` |
| `getFeaturesByProduct` | Lists features attached to a product | `Promise<FeatureDto[]>` |

## Method Reference

### createFeature

#### Description
Creates a new feature, validating keys, default values, and optional metadata before persisting it as `active`.

#### Signature
```typescript
createFeature(dto: CreateFeatureDto): Promise<FeatureDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreateFeatureDto` | Yes | Feature definition supplied by your app. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
| `displayName` | `string` | Yes | 1–255 char label. |
| `description` | `string` | No | ≤1000 chars. |
| `valueType` | `'toggle' | 'numeric' | 'text'` | Yes | Controls validation rules. |
| `defaultValue` | `string` | Yes | Must conform to `valueType`. |
| `groupName` | `string` | No | Optional grouping label. |
| `validator` | `Record<string, unknown>` | No | Custom metadata for downstream validation. |
| `metadata` | `Record<string, unknown>` | No | JSON-safe metadata blob. |

#### Returns
`Promise<FeatureDto>` – persisted feature snapshot.

#### Return Properties

| Field | Type | Description |
| --- | --- | --- |
| `key` | `string` | Immutable feature key. |
| `displayName` | `string` | Display label. |
| `description` | `string | null` | Optional summary. |
| `valueType` | `string` | `toggle`, `numeric`, or `text`. |
| `defaultValue` | `string` | Stored default. |
| `groupName` | `string | null` | Group label when set. |
| `status` | `string` | `active` or `archived`. |
| `validator` | `Record<string, unknown> | null` | Validator metadata. |
| `metadata` | `Record<string, unknown> | null` | Arbitrary metadata. |
| `createdAt` | `string` | ISO timestamp. |
| `updatedAt` | `string` | ISO timestamp. |

#### Expected Results
- Validates DTO fields and default value using `FeatureValueValidator`.
- Ensures key uniqueness.
- Persists feature with `active` status.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid or default mismatches `valueType`. |
| `ConflictError` | Feature key already exists. |

#### Example
```typescript
await features.createFeature({
  key: 'max-projects',
  displayName: 'Max Projects',
  valueType: 'numeric',
  defaultValue: '10'
});
```

### updateFeature

#### Description
Applies partial updates (display name, description, default value, grouping, validator, metadata) to an existing feature.

#### Signature
```typescript
updateFeature(key: string, dto: UpdateFeatureDto): Promise<FeatureDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Feature key to mutate. |
| `dto` | `UpdateFeatureDto` | Yes | Partial payload of fields to change. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `displayName` | `string` | No | Updated label (1–255 chars). |
| `description` | `string` | No | ≤1000 chars. |
| `valueType` | `'toggle' | 'numeric' | 'text'` | No | Changing type also requires compatible defaults. |
| `defaultValue` | `string` | No | Must match existing/new `valueType`. |
| `groupName` | `string` | No | Optional grouping label. |
| `validator` | `Record<string, unknown>` | No | Replaces validator metadata. |
| `metadata` | `Record<string, unknown>` | No | Replaces metadata blob. |

#### Returns
`Promise<FeatureDto>` – updated feature snapshot.

#### Return Properties
- Same `FeatureDto` fields described in `createFeature`.

#### Expected Results
- Validates provided fields and default/valueType compatibility.
- Loads feature, applies updates, recalculates timestamps, and saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid or default fails validation. |
| `NotFoundError` | Feature key not found. |

#### Example
```typescript
await features.updateFeature('max-projects', {
  defaultValue: '25',
  metadata: { tier: 'enterprise' }
});
```

### getFeature

#### Description
Retrieves a feature by key, returning `null` when it does not exist.

#### Signature
```typescript
getFeature(key: string): Promise<FeatureDto | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Feature identifier. |

#### Returns
`Promise<FeatureDto | null>`

#### Return Properties
- `FeatureDto` when found; `null` otherwise.

#### Expected Results
- Queries repository and maps record to DTO or returns `null`.

#### Potential Errors

| Error | When |
| --- | --- |
| _None_ | Missing features return `null`. |

#### Example
```typescript
const feature = await features.getFeature('gantt-charts');
```

### listFeatures

#### Description
Lists features with optional filtering, search, and pagination controls.

#### Signature
```typescript
listFeatures(filters?: FeatureFilterDto): Promise<FeatureDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `FeatureFilterDto` | No | Status, type, group, search, and pagination settings. |

#### Input Properties

| Field | Type | Description |
| --- | --- | --- |
| `status` | `'active' | 'archived'` | Filter by lifecycle state. |
| `valueType` | `'toggle' | 'numeric' | 'text'` | Restrict to a type. |
| `groupName` | `string` | Limit to a group label. |
| `search` | `string` | Text search across key/display name. |
| `limit` | `number` | 1–100 (default 50). |
| `offset` | `number` | ≥0 (default 0). |
| `sortBy` | `'displayName' | 'createdAt'` | Sort column. |
| `sortOrder` | `'asc' | 'desc'` | Sort direction (default `'asc'`). |

#### Returns
`Promise<FeatureDto[]>`

#### Return Properties
- Array of `FeatureDto` entries respecting the supplied filters.

#### Expected Results
- Validates filters.
- Executes query and maps records to DTOs.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters contain invalid values. |

#### Example
```typescript
const toggles = await features.listFeatures({ valueType: 'toggle', limit: 20 });
```

### archiveFeature

#### Description
Marks a feature as archived so it cannot be used for new plan values or overrides.

#### Signature
```typescript
archiveFeature(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Feature key to archive. |

#### Returns
`Promise<void>`

#### Return Properties
- None.

#### Expected Results
- Loads feature, invokes entity `archive()`, persists status change.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Feature key missing. |

#### Example
```typescript
await features.archiveFeature('legacy-beta');
```

### unarchiveFeature

#### Description
Restores an archived feature back to `active`.

#### Signature
```typescript
unarchiveFeature(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Feature key previously archived. |

#### Returns
`Promise<void>`

#### Return Properties
- None.

#### Expected Results
- Loads feature, calls `unarchive()`, persists the change.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Feature key missing. |

#### Example
```typescript
await features.unarchiveFeature('legacy-beta');
```

### deleteFeature

#### Description
Deletes a feature permanently after verifying it is archived and unused.

#### Signature
```typescript
deleteFeature(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Feature targeted for deletion. |

#### Returns
`Promise<void>`

#### Return Properties
- None.

#### Expected Results
- Loads feature and checks `feature.canDelete()` (must be archived).
- Ensures no product associations, plan feature values, or subscription overrides remain.
- Deletes the feature record.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Feature missing. |
| `DomainError` | Feature still active or referenced. |

#### Example
```typescript
await features.deleteFeature('sunset-flag');
```

### getFeaturesByProduct

#### Description
Returns all features currently associated with a product.

#### Signature
```typescript
getFeaturesByProduct(productKey: string): Promise<FeatureDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `productKey` | `string` | Yes | Product key to inspect. |

#### Returns
`Promise<FeatureDto[]>`

#### Return Properties
- Array of `FeatureDto` entries linked to the product.

#### Expected Results
- Validates the product exists.
- Queries associations and maps features to DTOs.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product key missing. |

#### Example
```typescript
const productFeatures = await features.getFeaturesByProduct('pro-suite');
```

## DTO Reference

### CreateFeatureDto

| Field | Type | Required | Constraints |
| --- | --- | --- | --- |
| `key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
| `displayName` | `string` | Yes | 1–255 chars. |
| `description` | `string` | No | ≤1000 chars. |
| `valueType` | `'toggle' | 'numeric' | 'text'` | Yes | Determines validation rules. |
| `defaultValue` | `string` | Yes | Must match `valueType`. |
| `groupName` | `string` | No | ≤255 chars. |
| `validator` | `Record<string, unknown>` | No | Custom validation metadata. |
| `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

### UpdateFeatureDto

Identical fields as `CreateFeatureDto`, but all optional; when both `valueType` and `defaultValue` are provided they must remain compatible.

### FeatureDto

| Field | Type | Description |
| --- | --- | --- |
| `key` | `string` | Immutable key. |
| `displayName` | `string` | Human-readable name. |
| `description` | `string | null` | Optional description. |
| `valueType` | `string` | `toggle`, `numeric`, or `text`. |
| `defaultValue` | `string` | Stored default value. |
| `groupName` | `string | null` | Group label. |
| `status` | `string` | `active` or `archived`. |
| `validator` | `Record<string, unknown> | null` | Validator metadata. |
| `metadata` | `Record<string, unknown> | null` | Arbitrary metadata. |
| `createdAt` | `string` | ISO timestamp. |
| `updatedAt` | `string` | ISO timestamp. |

### FeatureFilterDto

| Field | Type | Description |
| --- | --- | --- |
| `status` | `'active' | 'archived'` | Lifecycle filter. |
| `valueType` | `'toggle' | 'numeric' | 'text'` | Type filter. |
| `groupName` | `string` | Group filter. |
| `search` | `string` | Text search term. |
| `limit` | `number` | 1–100 (default 50). |
| `offset` | `number` | ≥0 (default 0). |
| `sortBy` | `'displayName' | 'createdAt'` | Sort column. |
| `sortOrder` | `'asc' | 'desc'` | Sort direction. |

## Related Workflows
- Products must associate features before plans can set values (`ProductManagementService.associateFeature`).
- Plan-level values (`PlanManagementService.setFeatureValue`) override defaults but are superseded by subscription overrides.
- Subscription overrides (`SubscriptionManagementService.addFeatureOverride`) take precedence in the feature resolution hierarchy enforced by `FeatureCheckerService`.
