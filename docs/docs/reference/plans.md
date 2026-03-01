# Plan Management Service Reference

## Service Overview
The Plan Management Service manages purchasable tiers within each product. It handles plan lifecycle, feature value overrides, and transition settings that affect billing cycles and subscriptions.

- Plan keys are globally unique and immutable.
- Each plan belongs to exactly one product (`productKey`) and may define transition targets via `onExpireTransitionToBillingCycleKey`.
- Deletion is only allowed when a plan is archived and unused by billing cycles or subscriptions.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const plans = subscrio.plans;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var plans = subscrio.Plans;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `createPlan` | Creates a new plan for an existing product | `Promise<PlanDto>` |
    | `updatePlan` | Updates mutable plan fields | `Promise<PlanDto>` |
    | `getPlan` | Retrieves a plan by key | `Promise<PlanDto \| null>` |
    | `listPlans` | Lists plans with filters and pagination | `Promise<PlanDto[]>` |
    | `getPlansByProduct` | Lists plans for a specific product | `Promise<PlanDto[]>` |
    | `archivePlan` | Archives a plan | `Promise<void>` |
    | `unarchivePlan` | Reactivates an archived plan | `Promise<void>` |
    | `deletePlan` | Deletes an archived plan with no dependencies | `Promise<void>` |
    | `setFeatureValue` | Sets a plan-level feature value | `Promise<void>` |
    | `removeFeatureValue` | Removes a plan feature override | `Promise<void>` |
    | `getFeatureValue` | Gets a plan's value for a feature | `Promise<string \| null>` |
    | `getPlanFeatures` | Lists all feature values stored on a plan | `Promise<Array<{featureKey: string, value: string}>>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `CreatePlanAsync` | Creates a new plan for an existing product | `Task<PlanDto>` |
    | `UpdatePlanAsync` | Updates mutable plan fields | `Task<PlanDto>` |
    | `GetPlanAsync` | Retrieves a plan by key | `Task<PlanDto?>` |
    | `ListPlansAsync` | Lists plans with filters and pagination | `Task<List<PlanDto>>` |
    | `GetPlansByProductAsync` | Lists plans for a specific product | `Task<List<PlanDto>>` |
    | `ArchivePlanAsync` | Archives a plan | `Task` |
    | `UnarchivePlanAsync` | Reactivates an archived plan | `Task` |
    | `DeletePlanAsync` | Deletes an archived plan with no dependencies | `Task` |
    | `SetFeatureValueAsync` | Sets a plan-level feature value | `Task` |
    | `RemoveFeatureValueAsync` | Removes a plan feature override | `Task` |
    | `GetFeatureValueAsync` | Gets a plan's value for a feature | `Task<string?>` |
    | `GetPlanFeaturesAsync` | Lists all feature values stored on a plan | `Task<List<PlanFeatureDto>>` |

## Method Reference

### createPlan

#### Description
Validates payload, ensures the product exists, and persists a new plan with `active` status.

=== "TypeScript"
    #### Signature
    ```typescript
    createPlan(dto: CreatePlanDto): Promise<PlanDto>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `dto` | `CreatePlanDto` | Yes | Plan definition including product and metadata. |

    #### Input Properties (CreatePlanDto)

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

    #### Example
    ```typescript
    await plans.createPlan({
      productKey: 'pro-suite',
      key: 'annual-pro',
      displayName: 'Annual Pro',
      metadata: { priceUsd: 499 }
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<PlanDto> CreatePlanAsync(CreatePlanDto dto)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `dto` | `CreatePlanDto` | Yes | Plan definition including product and metadata. |

    #### Input Properties (CreatePlanDto)

    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `ProductKey` | `string` | Yes | Product that owns the plan. |
    | `Key` | `string` | Yes | Immutable plan key (1–255 chars). |
    | `DisplayName` | `string` | Yes | 1–255 char label. |
    | `Description` | `string` | No | ≤1000 characters. |
    | `OnExpireTransitionToBillingCycleKey` | `string` | No | Optional billing cycle key for automatic transitions. |
    | `Metadata` | `Dictionary<string, object?>` | No | JSON-safe metadata. |

    #### Returns
    `Task<PlanDto>` – persisted plan snapshot.

    #### Return Properties
    - See `PlanDto` in the DTO Reference.

    #### Example
    ```csharp
    await subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
        ProductKey: "pro-suite",
        Key: "annual-pro",
        DisplayName: "Annual Pro",
        Metadata: new Dictionary<string, object?> { ["priceUsd"] = 499 }
    ));
    ```

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

### updatePlan

#### Description
Applies partial updates such as display name, description, transition target, or metadata.

=== "TypeScript"
    #### Signature
    ```typescript
    updatePlan(planKey: string, dto: UpdatePlanDto): Promise<PlanDto>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan to mutate. |
    | `dto` | `UpdatePlanDto` | Yes | Partial update payload. |

    #### Input Properties (UpdatePlanDto)

    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `displayName` | `string` | No | Updated label. |
    | `description` | `string` | No | New description. |
    | `onExpireTransitionToBillingCycleKey` | `string` | No | Replacement transition target. |
    | `metadata` | `Record<string, unknown>` | No | Replaces metadata blob. |

    #### Returns
    `Promise<PlanDto>` – updated plan snapshot.

    #### Example
    ```typescript
    await plans.updatePlan('annual-pro', {
      onExpireTransitionToBillingCycleKey: 'monthly-pro',
      metadata: { priceUsd: 399 }
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<PlanDto> UpdatePlanAsync(string planKey, UpdatePlanDto dto)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan to mutate. |
    | `dto` | `UpdatePlanDto` | Yes | Partial update payload. |

    #### Input Properties (UpdatePlanDto)

    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `DisplayName` | `string` | No | Updated label. |
    | `Description` | `string` | No | New description. |
    | `OnExpireTransitionToBillingCycleKey` | `string` | No | Replacement transition target. |
    | `Metadata` | `Dictionary<string, object?>` | No | Replaces metadata blob. |

    #### Returns
    `Task<PlanDto>` – updated plan snapshot.

    #### Example
    ```csharp
    await subscrio.Plans.UpdatePlanAsync("annual-pro", new UpdatePlanDto(
        OnExpireTransitionToBillingCycleKey: "monthly-pro",
        Metadata: new Dictionary<string, object?> { ["priceUsd"] = 399 }
    ));
    ```

#### Expected Results
- Validates provided fields.
- Loads plan, applies updates, persists entity.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid. |
| `NotFoundError` | Plan key not found. |

### getPlan

#### Description
Retrieves a plan by key, returning `null` when it is missing.

=== "TypeScript"
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

    #### Example
    ```typescript
    const plan = await plans.getPlan('annual-pro');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<PlanDto?> GetPlanAsync(string planKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan identifier. |

    #### Returns
    `Task<PlanDto?>`

    #### Return Properties
    - `PlanDto` when found; `null` otherwise.

    #### Example
    ```csharp
    var plan = await subscrio.Plans.GetPlanAsync("annual-pro");
    ```

#### Expected Results
- Loads plan from repository and maps to DTO.

#### Potential Errors

| Error | When |
| --- | --- |
| _None_ | Returns `null` when not found. |

### listPlans

#### Description
Lists plans using optional status, product, search, and pagination filters.

=== "TypeScript"
    #### Signature
    ```typescript
    listPlans(filters?: PlanFilterDto): Promise<PlanDto[]>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filters` | `PlanFilterDto` | No | Optional filter and pagination controls. |

    #### Input Properties (PlanFilterDto)

    | Field | Type | Description |
    | --- | --- | --- |
    | `productKey` | `string` | Restrict to a product. |
    | `status` | `'active' \| 'archived'` | Lifecycle filter. |
    | `search` | `string` | Text search across key/display name. |
    | `sortBy` | `'displayName' \| 'createdAt'` | Sort column. |
    | `sortOrder` | `'asc' \| 'desc'` | Sort direction (default `'asc'`). |
    | `limit` | `number` | 1–100 (default 50). |
    | `offset` | `number` | ≥0 (default 0). |

    #### Returns
    `Promise<PlanDto[]>`

    #### Return Properties
    - Array of `PlanDto` entries.

    #### Example
    ```typescript
    const archivedPlans = await plans.listPlans({ status: 'archived', limit: 20 });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<PlanDto>> ListPlansAsync(PlanFilterDto? filters = null)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filters` | `PlanFilterDto` | No | Optional filter and pagination controls. |

    #### Input Properties (PlanFilterDto)

    | Property | Type | Description |
    | --- | --- | --- |
    | `ProductKey` | `string` | Restrict to a product. |
    | `Status` | `string` | `active` or `archived`. |
    | `Search` | `string` | Text search across key/display name. |
    | `SortBy` | `string` | `displayName` or `createdAt`. |
    | `SortOrder` | `string` | `asc` or `desc`; default `asc`. |
    | `Limit` | `int` | 1–100 (default 50). |
    | `Offset` | `int` | ≥0 (default 0). |

    #### Returns
    `Task<List<PlanDto>>`

    #### Return Properties
    - `List<PlanDto>` of plan entries.

    #### Example
    ```csharp
    var archivedPlans = await subscrio.Plans.ListPlansAsync(new PlanFilterDto(
        Status: "archived",
        Limit: 20
    ));
    ```

#### Expected Results
- Validates filters.
- Executes query and maps results to DTOs.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters invalid. |

### getPlansByProduct

#### Description
Returns all plans owned by a product.

=== "TypeScript"
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

    #### Example
    ```typescript
    const proPlans = await plans.getPlansByProduct('pro-suite');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<PlanDto>> GetPlansByProductAsync(string productKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Product key to inspect. |

    #### Returns
    `Task<List<PlanDto>>`

    #### Return Properties
    - `List<PlanDto>` of plans scoped to the product.

    #### Example
    ```csharp
    var proPlans = await subscrio.Plans.GetPlansByProductAsync("pro-suite");
    ```

#### Expected Results
- Ensures product exists.
- Queries repository for all plans referencing the product.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product key missing. |

### archivePlan

#### Description
Marks a plan as archived so it cannot be sold to new customers.

=== "TypeScript"
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

    #### Example
    ```typescript
    await plans.archivePlan('legacy-tier');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task ArchivePlanAsync(string planKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan to archive. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Plans.ArchivePlanAsync("legacy-tier");
    ```

#### Expected Results
- Loads plan, calls entity `archive()`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

### unarchivePlan

#### Description
Restores an archived plan to `active`.

=== "TypeScript"
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

    #### Example
    ```typescript
    await plans.unarchivePlan('legacy-tier');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task UnarchivePlanAsync(string planKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Archived plan key. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Plans.UnarchivePlanAsync("legacy-tier");
    ```

#### Expected Results
- Loads plan, calls `unarchive()`, persists change.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

### deletePlan

#### Description
Deletes a plan after confirming it is archived and unused by billing cycles or subscriptions.

=== "TypeScript"
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

    #### Example
    ```typescript
    await plans.deletePlan('legacy-tier');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task DeletePlanAsync(string planKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan targeted for deletion. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Plans.DeletePlanAsync("legacy-tier");
    ```

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

### setFeatureValue

#### Description
Sets or updates a plan-level override for a feature.

#### Signature

=== "TypeScript"
    ```typescript
    setFeatureValue(planKey: string, featureKey: string, value: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task SetFeatureValueAsync(string planKey, string featureKey, string value)
    ```

#### Inputs

=== "TypeScript"
    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan receiving the override. |
    | `featureKey` | `string` | Yes | Feature key being overridden. |
    | `value` | `string` | Yes | Stored string value validated against feature type. |

=== ".NET"
    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan receiving the override. |
    | `featureKey` | `string` | Yes | Feature key being overridden. |
    | `value` | `string` | Yes | Stored string value validated against feature type. |

#### Input Properties
- `value` must satisfy the feature’s `valueType` and validator metadata.

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Example

=== "TypeScript"
    ```typescript
    await plans.setFeatureValue('annual-pro', 'max-projects', '100');
    ```

=== ".NET"
    ```csharp
    await subscrio.Plans.SetFeatureValueAsync("annual-pro", "max-projects", "100");
    ```

#### Expected Results
- Ensures plan and feature exist.
- Validates the value via `FeatureValueValidator`.
- Inserts or updates the plan’s feature value entry.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan or feature missing. |
| `ValidationError` | Value fails validation rules. |

### removeFeatureValue

#### Description
Removes a stored feature override from a plan.

=== "TypeScript"
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

    #### Example
    ```typescript
    await plans.removeFeatureValue('annual-pro', 'max-projects');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task RemoveFeatureValueAsync(string planKey, string featureKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan losing the override. |
    | `featureKey` | `string` | Yes | Feature key to remove. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Plans.RemoveFeatureValueAsync("annual-pro", "max-projects");
    ```

#### Expected Results
- Ensures plan and feature exist.
- Removes the stored value when present.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan or feature missing. |

### getFeatureValue

#### Description
Retrieves the value a plan has stored for a specific feature.

=== "TypeScript"
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

    #### Example
    ```typescript
    const value = await plans.getFeatureValue('annual-pro', 'max-projects');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<string?> GetFeatureValueAsync(string planKey, string featureKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan to inspect. |
    | `featureKey` | `string` | Yes | Feature key. |

    #### Returns
    `Task<string?>`

    #### Return Properties
    - `string` when a stored value exists.
    - `null` when the plan has no override.

    #### Example
    ```csharp
    var value = await subscrio.Plans.GetFeatureValueAsync("annual-pro", "max-projects");
    ```

#### Expected Results
- Loads plan and returns the stored feature value if present.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

### getPlanFeatures

#### Description
Lists all feature overrides configured on a plan.

=== "TypeScript"
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

    #### Example
    ```typescript
    const overrides = await plans.getPlanFeatures('annual-pro');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<PlanFeatureDto>> GetPlanFeaturesAsync(string planKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan identifier. |

    #### Returns
    `Task<List<PlanFeatureDto>>`

    #### Return Properties
    - `List<PlanFeatureDto>` with `FeatureKey` and `Value` properties.

    #### Example
    ```csharp
    var overrides = await subscrio.Plans.GetPlanFeaturesAsync("annual-pro");
    ```

#### Expected Results
- Loads plan and returns all feature value entries.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

## DTO Reference

### CreatePlanDto

=== "TypeScript"
    | Field | Type | Required | Notes |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Existing product key. |
    | `key` | `string` | Yes | Unique plan identifier. |
    | `displayName` | `string` | Yes | 1–255 characters. |
    | `description` | `string` | No | ≤1000 characters. |
    | `onExpireTransitionToBillingCycleKey` | `string` | No | Billing cycle key for auto transition. |
    | `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

=== ".NET"
    | Property | Type | Required | Notes |
    | --- | --- | --- | --- |
    | `ProductKey` | `string` | Yes | Existing product key. |
    | `Key` | `string` | Yes | Unique plan identifier. |
    | `DisplayName` | `string` | Yes | 1–255 characters. |
    | `Description` | `string` | No | ≤1000 characters. |
    | `OnExpireTransitionToBillingCycleKey` | `string` | No | Billing cycle key for auto transition. |
    | `Metadata` | `Dictionary<string, object?>` | No | JSON-safe metadata. |

### UpdatePlanDto
All `CreatePlanDto` fields become optional for updates.

### PlanDto

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `productKey` | `string` | Parent product key. |
    | `key` | `string` | Plan key. |
    | `displayName` | `string` | Human-friendly name. |
    | `description` | `string \| null` | Optional description. |
    | `status` | `string` | `active` or `archived`. |
    | `onExpireTransitionToBillingCycleKey` | `string \| null` | Transition target key. |
    | `metadata` | `Record<string, unknown> \| null` | Stored metadata. |
    | `createdAt` | `string` | ISO timestamp. |
    | `updatedAt` | `string` | ISO timestamp. |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `ProductKey` | `string` | Parent product key. |
    | `Key` | `string` | Plan key. |
    | `DisplayName` | `string` | Human-friendly name. |
    | `Description` | `string?` | Optional description. |
    | `Status` | `string` | `active` or `archived`. |
    | `OnExpireTransitionToBillingCycleKey` | `string?` | Transition target key. |
    | `Metadata` | `Dictionary<string, object?>?` | Stored metadata. |
    | `CreatedAt` | `string` | ISO timestamp. |
    | `UpdatedAt` | `string` | ISO timestamp. |

### PlanFilterDto

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `productKey` | `string` | Filter by product. |
    | `status` | `'active' \| 'archived'` | Lifecycle filter. |
    | `search` | `string` | Text search. |
    | `sortBy` | `'displayName' \| 'createdAt'` | Sort column. |
    | `sortOrder` | `'asc' \| 'desc'` | Sort direction. |
    | `limit` | `number` | 1–100 (default 50). |
    | `offset` | `number` | ≥0 (default 0). |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `ProductKey` | `string` | Filter by product. |
    | `Status` | `string` | `active` or `archived`. |
    | `Search` | `string` | Text search. |
    | `SortBy` | `string` | `displayName` or `createdAt`. |
    | `SortOrder` | `string` | `asc` or `desc`; default `asc`. |
    | `Limit` | `int` | 1–100 (default 50). |
    | `Offset` | `int` | ≥0 (default 0). |

## Related Workflows
- Products must exist before plans can be created (`ProductManagementService`).
- Plans cannot be deleted while billing cycles (`BillingCycleManagementService`) or subscriptions (`SubscriptionManagementService`) reference them.
- Plan feature values participate in the feature resolution hierarchy enforced by `FeatureCheckerService`.
