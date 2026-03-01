# Plan Management Service Reference

## Service Overview
The Plan Management Service manages purchasable tiers within each product. It handles plan lifecycle, feature value overrides, and transition settings that affect billing cycles and subscriptions.

- Plan keys are globally unique and immutable.
- Each plan belongs to exactly one product (`productKey`) and may define transition targets via `onExpireTransitionToBillingCycleKey`.
- Deletion is only allowed when a plan is archived and unused by billing cycles or subscriptions.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const plans = subscrio.plans;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `createPlan` | Creates a new plan for an existing product | `Promise<PlanDto>` |
| `updatePlan` | Updates mutable plan fields | `Promise<PlanDto>` |
| `getPlan` | Retrieves a plan by key | `Promise<PlanDto | null>` |
| `listPlans` | Lists plans with filters and pagination | `Promise<PlanDto[]>` |
| `getPlansByProduct` | Lists plans for a specific product | `Promise<PlanDto[]>` |
| `archivePlan` | Archives a plan | `Promise<void>` |
| `unarchivePlan` | Reactivates an archived plan | `Promise<void>` |
| `deletePlan` | Deletes an archived plan with no dependencies | `Promise<void>` |
| `setFeatureValue` | Sets a plan-level feature value | `Promise<void>` |
| `removeFeatureValue` | Removes a plan feature override | `Promise<void>` |
| `getFeatureValue` | Gets a plan’s value for a feature | `Promise<string | null>` |
| `getPlanFeatures` | Lists all feature values stored on a plan | `Promise<Array<{ featureKey: string; value: string }>>` |

## Method Reference

### createPlan

#### Description
Validates payload, ensures the product exists, and persists a new plan with `active` status.

#### Signature
```typescript
createPlan(dto: CreatePlanDto): Promise<PlanDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreatePlanDto` | Yes | Plan definition including product and metadata. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `productKey` | `string` | Yes | Product that owns the plan. |
| `key` | `string` | Yes | Immutable plan key (1–255 chars). |
| `displayName` | `string` | Yes | 1–255 char label. |
| `description` | `string` | No | ≤1000 characters. |
| `onExpireTransitionToBillingCycleKey` | `string` | No | Optional billing cycle key for automatic transitions. |
| `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

#### Returns
`Promise<PlanDto>` – persisted plan snapshot.

#### Return Properties
- See `PlanDto` in the DTO Reference.

#### Expected Results
- Validates DTO via Zod schema.
- Ensures product exists.
- Rejects duplicate plan keys.
- Persists plan with empty feature values array.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid. |
| `NotFoundError` | Product key not found. |
| `ConflictError` | Plan key already exists. |

#### Example
```typescript
await plans.createPlan({
  productKey: 'pro-suite',
  key: 'annual-pro',
  displayName: 'Annual Pro',
  metadata: { priceUsd: 499 }
});
```

### updatePlan

#### Description
Applies partial updates such as display name, description, transition target, or metadata.

#### Signature
```typescript
updatePlan(planKey: string, dto: UpdatePlanDto): Promise<PlanDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan to mutate. |
| `dto` | `UpdatePlanDto` | Yes | Partial update payload. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `displayName` | `string` | No | Updated label. |
| `description` | `string` | No | New description. |
| `onExpireTransitionToBillingCycleKey` | `string` | No | Replacement transition target. |
| `metadata` | `Record<string, unknown>` | No | Replaces metadata blob. |

#### Returns
`Promise<PlanDto>` – updated plan snapshot.

#### Expected Results
- Validates provided fields.
- Loads plan, applies updates, persists entity.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid. |
| `NotFoundError` | Plan key not found. |

#### Example
```typescript
await plans.updatePlan('annual-pro', {
  onExpireTransitionToBillingCycleKey: 'monthly-pro',
  metadata: { priceUsd: 399 }
});
```

### getPlan

#### Description
Retrieves a plan by key, returning `null` when it is missing.

#### Signature
```typescript
getPlan(planKey: string): Promise<PlanDto | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan identifier. |

#### Returns
`Promise<PlanDto | null>`

#### Return Properties
- `PlanDto` when found; `null` otherwise.

#### Expected Results
- Loads plan from repository and maps to DTO.

#### Potential Errors

| Error | When |
| --- | --- |
| _None_ | Returns `null` when not found. |

#### Example
```typescript
const plan = await plans.getPlan('annual-pro');
```

### listPlans

#### Description
Lists plans using optional status, product, search, and pagination filters.

#### Signature
```typescript
listPlans(filters?: PlanFilterDto): Promise<PlanDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `PlanFilterDto` | No | Optional filter and pagination controls. |

#### Input Properties

| Field | Type | Description |
| --- | --- | --- |
| `productKey` | `string` | Restrict to a product. |
| `status` | `'active' | 'archived'` | Lifecycle filter. |
| `search` | `string` | Text search across key/display name. |
| `sortBy` | `'displayName' | 'createdAt'` | Sort column. |
| `sortOrder` | `'asc' | 'desc'` | Sort direction (default `'asc'`). |
| `limit` | `number` | 1–100 (default 50). |
| `offset` | `number` | ≥0 (default 0). |

#### Returns
`Promise<PlanDto[]>`

#### Return Properties
- Array of `PlanDto` entries.

#### Expected Results
- Validates filters.
- Executes query and maps results to DTOs.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters invalid. |

#### Example
```typescript
const archivedPlans = await plans.listPlans({ status: 'archived', limit: 20 });
```

### getPlansByProduct

#### Description
Returns all plans owned by a product.

#### Signature
```typescript
getPlansByProduct(productKey: string): Promise<PlanDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `productKey` | `string` | Yes | Product key to inspect. |

#### Returns
`Promise<PlanDto[]>`

#### Return Properties
- Array of `PlanDto` entries scoped to the product.

#### Expected Results
- Ensures product exists.
- Queries repository for all plans referencing the product.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product key missing. |

#### Example
```typescript
const proPlans = await plans.getPlansByProduct('pro-suite');
```

### archivePlan

#### Description
Marks a plan as archived so it cannot be sold to new customers.

#### Signature
```typescript
archivePlan(planKey: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan to archive. |

#### Returns
`Promise<void>`

#### Expected Results
- Loads plan, calls entity `archive()`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

#### Example
```typescript
await plans.archivePlan('legacy-tier');
```

### unarchivePlan

#### Description
Restores an archived plan to `active`.

#### Signature
```typescript
unarchivePlan(planKey: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Archived plan key. |

#### Returns
`Promise<void>`

#### Expected Results
- Loads plan, calls `unarchive()`, persists change.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

#### Example
```typescript
await plans.unarchivePlan('legacy-tier');
```

### deletePlan

#### Description
Deletes a plan after confirming it is archived and unused by billing cycles or subscriptions.

#### Signature
```typescript
deletePlan(planKey: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan targeted for deletion. |

#### Returns
`Promise<void>`

#### Expected Results
- Loads plan and checks `plan.canDelete()` (requires archived status).
- Ensures no billing cycles reference the plan.
- Ensures no subscriptions exist for the plan.
- Deletes plan record.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |
| `DomainError` | Plan still active or has dependent records. |

#### Example
```typescript
await plans.deletePlan('legacy-tier');
```

### setFeatureValue

#### Description
Sets or updates a plan-level override for a feature.

#### Signature
```typescript
setFeatureValue(planKey: string, featureKey: string, value: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan receiving the override. |
| `featureKey` | `string` | Yes | Feature key being overridden. |
| `value` | `string` | Yes | Stored string value validated against feature type. |

#### Input Properties
- `value` must satisfy the feature’s `valueType` and validator metadata.

#### Returns
`Promise<void>`

#### Expected Results
- Ensures plan and feature exist.
- Validates the value via `FeatureValueValidator`.
- Inserts or updates the plan’s feature value entry.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan or feature missing. |
| `ValidationError` | Value fails validation rules. |

#### Example
```typescript
await plans.setFeatureValue('annual-pro', 'max-projects', '100');
```

### removeFeatureValue

#### Description
Removes a stored feature override from a plan.

#### Signature
```typescript
removeFeatureValue(planKey: string, featureKey: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan losing the override. |
| `featureKey` | `string` | Yes | Feature key to remove. |

#### Returns
`Promise<void>`

#### Expected Results
- Ensures plan and feature exist.
- Removes the stored value when present.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan or feature missing. |

#### Example
```typescript
await plans.removeFeatureValue('annual-pro', 'max-projects');
```

### getFeatureValue

#### Description
Retrieves the value a plan has stored for a specific feature.

#### Signature
```typescript
getFeatureValue(planKey: string, featureKey: string): Promise<string | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan to inspect. |
| `featureKey` | `string` | Yes | Feature key. |

#### Returns
`Promise<string | null>`

#### Return Properties
- `string` when a stored value exists.
- `null` when the plan has no override.

#### Expected Results
- Loads plan and returns the stored feature value if present.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

#### Example
```typescript
const value = await plans.getFeatureValue('annual-pro', 'max-projects');
```

### getPlanFeatures

#### Description
Lists all feature overrides configured on a plan.

#### Signature
```typescript
getPlanFeatures(planKey: string): Promise<Array<{ featureKey: string; value: string }>>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan identifier. |

#### Returns
`Promise<Array<{ featureKey: string; value: string }>>`

#### Return Properties
- Array of `{ featureKey, value }` pairs for overrides stored on the plan.

#### Expected Results
- Loads plan and returns all feature value entries.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

#### Example
```typescript
const overrides = await plans.getPlanFeatures('annual-pro');
```

## DTO Reference

### CreatePlanDto

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `productKey` | `string` | Yes | Existing product key. |
| `key` | `string` | Yes | Unique plan identifier. |
| `displayName` | `string` | Yes | 1–255 characters. |
| `description` | `string` | No | ≤1000 characters. |
| `onExpireTransitionToBillingCycleKey` | `string` | No | Billing cycle key for auto transition. |
| `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

### UpdatePlanDto
All `CreatePlanDto` fields become optional for updates.

### PlanDto

| Field | Type | Description |
| --- | --- | --- |
| `productKey` | `string` | Parent product key. |
| `key` | `string` | Plan key. |
| `displayName` | `string` | Human-friendly name. |
| `description` | <code>string &#124; null</code> | Optional description. |
| `status` | `string` | `active` or `archived`. |
| `onExpireTransitionToBillingCycleKey` | <code>string &#124; null</code> | Transition target key. |
| `metadata` | <code>Record&lt;string, unknown&gt; &#124; null</code> | Stored metadata. |
| `createdAt` | `string` | ISO timestamp. |
| `updatedAt` | `string` | ISO timestamp. |

### PlanFilterDto

| Field | Type | Description |
| --- | --- | --- |
| `productKey` | `string` | Filter by product. |
| `status` | <code>'active' &#124; 'archived'</code> | Lifecycle filter. |
| `search` | `string` | Text search. |
| `sortBy` | <code>'displayName' &#124; 'createdAt'</code> | Sort column. |
| `sortOrder` | <code>'asc' &#124; 'desc'</code> | Sort direction. |
| `limit` | `number` | 1–100 (default 50). |
| `offset` | `number` | ≥0 (default 0). |

## Related Workflows
- Products must exist before plans can be created (`ProductManagementService`).
- Plans cannot be deleted while billing cycles (`BillingCycleManagementService`) or subscriptions (`SubscriptionManagementService`) reference them.
- Plan feature values participate in the feature resolution hierarchy enforced by `FeatureCheckerService`.
