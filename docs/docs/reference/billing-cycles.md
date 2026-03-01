# Billing Cycle Management Service Reference

## Service Overview
Billing cycles define how plans renew (duration, cadence, and external price IDs). Each cycle belongs to a plan, exposes derived `planKey`/`productKey` in DTOs, and enforces delete guards whenever subscriptions or plan transitions reference it.

- Duration units: `days`, `weeks`, `months`, `years`, or `forever` (when `forever`, `durationValue` must be omitted).
- Cycles can expose `externalProductId` (e.g., Stripe price) for payment processor mappings.
- Delete operations require the cycle to be archived and unused by subscriptions or plan transition settings.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const billingCycles = subscrio.billingCycles;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var billingCycles = subscrio.BillingCycles;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `createBillingCycle` | Creates a cycle for an existing plan | `Promise<BillingCycleDto>` |
    | `updateBillingCycle` | Updates mutable fields on a cycle | `Promise<BillingCycleDto>` |
    | `getBillingCycle` | Retrieves a cycle by key | `Promise<BillingCycleDto \| null>` |
    | `getBillingCyclesByPlan` | Lists cycles for a plan | `Promise<BillingCycleDto[]>` |
    | `listBillingCycles` | Lists cycles with filters/pagination | `Promise<BillingCycleDto[]>` |
    | `archiveBillingCycle` | Archives a cycle | `Promise<void>` |
    | `unarchiveBillingCycle` | Reactivates an archived cycle | `Promise<void>` |
    | `deleteBillingCycle` | Deletes an archived, unused cycle | `Promise<void>` |
    | `calculateNextPeriodEnd` | Computes next renewal end date | `Promise<Date \| null>` |
    | `getBillingCyclesByDurationUnit` | Filters cycles by duration unit | `Promise<BillingCycleDto[]>` |
    | `getDefaultBillingCycles` | Loads pre-installed defaults (monthly/quarterly/yearly) | `Promise<BillingCycleDto[]>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `CreateBillingCycleAsync` | Creates a cycle for an existing plan | `Task<BillingCycleDto>` |
    | `UpdateBillingCycleAsync` | Updates mutable fields on a cycle | `Task<BillingCycleDto>` |
    | `GetBillingCycleAsync` | Retrieves a cycle by key | `Task<BillingCycleDto?>` |
    | `GetBillingCyclesByPlanAsync` | Lists cycles for a plan | `Task<List<BillingCycleDto>>` |
    | `ListBillingCyclesAsync` | Lists cycles with filters/pagination | `Task<List<BillingCycleDto>>` |
    | `ArchiveBillingCycleAsync` | Archives a cycle | `Task` |
    | `UnarchiveBillingCycleAsync` | Reactivates an archived cycle | `Task` |
    | `DeleteBillingCycleAsync` | Deletes an archived, unused cycle | `Task` |
    | `CalculateNextPeriodEndAsync` | Computes next renewal end date | `Task<DateTime?>` |
    | `GetBillingCyclesByDurationUnitAsync` | Filters cycles by duration unit | `Task<List<BillingCycleDto>>` |
    | `GetDefaultBillingCyclesAsync` | Loads pre-installed defaults (monthly/quarterly/yearly) | `Task<List<BillingCycleDto>>` |

## Method Reference

### createBillingCycle

#### Description
 Validates a new billing cycle payload, ensures the plan exists, and persists the cycle with `active` status.

#### Signature

=== "TypeScript"
    ```typescript
    createBillingCycle(dto: CreateBillingCycleDto): Promise<BillingCycleDto>
    ```

=== ".NET"
    ```csharp
    Task<BillingCycleDto> CreateBillingCycleAsync(CreateBillingCycleDto dto)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreateBillingCycleDto` | Yes | Cycle definition for an existing plan. |

#### Input Properties

=== "TypeScript"
    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `planKey` | `string` | Yes | Plan owning the cycle (lowercase alphanumeric + `-`). |
    | `key` | `string` | Yes | Globally unique billing cycle key. |
    | `displayName` | `string` | Yes | 1–255 char label. |
    | `description` | `string` | No | ≤1000 chars. |
    | `durationValue` | `number` | Conditional | Required unless `durationUnit` is `forever`; positive integer. |
    | `durationUnit` | `'days' \| 'weeks' \| 'months' \| 'years' \| 'forever'` | Yes | Renewal cadence. |
    | `externalProductId` | `string` | No | Stripe price or other external ID (≤255 chars). |

=== ".NET"
    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `PlanKey` | `string` | Yes | Plan owning the cycle. |
    | `Key` | `string` | Yes | Globally unique billing cycle key. |
    | `DisplayName` | `string` | Yes | 1–255 char label. |
    | `Description` | `string` | No | ≤1000 chars. |
    | `DurationValue` | `int?` | Conditional | Required unless `DurationUnit` is `forever`. |
    | `DurationUnit` | `string` | Yes | `days`, `weeks`, `months`, `years`, or `forever`. |
    | `ExternalProductId` | `string` | No | Stripe price or other external ID (≤255 chars). |

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto>` – persisted cycle snapshot.

=== ".NET"
    `Task<BillingCycleDto>` – persisted cycle snapshot.

#### Return Properties

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `key` | `string` | Cycle key. |
    | `planKey` | `string` | Owning plan key. |
    | `productKey` | `string` | Derived from plan. |
    | `displayName` | `string` | Display label. |
    | `description` | `string \| null` | Optional description. |
    | `status` | `string` | `active` or `archived`. |
    | `durationValue` | `number \| null` | `null` when unit is `forever`. |
    | `durationUnit` | `string` | Duration unit. |
    | `externalProductId` | `string \| null` | Payment processor price ID. |
    | `createdAt` | `string` | ISO timestamp. |
    | `updatedAt` | `string` | ISO timestamp. |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `Key` | `string` | Cycle key. |
    | `PlanKey` | `string` | Owning plan key. |
    | `ProductKey` | `string` | Derived from plan. |
    | `DisplayName` | `string` | Display label. |
    | `Description` | `string?` | Optional description. |
    | `Status` | `string` | `active` or `archived`. |
    | `DurationValue` | `int?` | `null` when unit is `forever`. |
    | `DurationUnit` | `string` | Duration unit. |
    | `ExternalProductId` | `string?` | Payment processor price ID. |
    | `CreatedAt` | `string` | ISO timestamp. |
    | `UpdatedAt` | `string` | ISO timestamp. |

#### Expected Results
- Validates DTO (including duration rules).
- Loads plan by `planKey` and fails if missing.
- Rejects duplicate billing cycle keys.
- Persists cycle with status `active`.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid or duration config inconsistent. |
| `NotFoundError` | Plan missing. |
| `ConflictError` | Billing cycle key already exists. |

#### Example

=== "TypeScript"
    ```typescript
    await billingCycles.createBillingCycle({
      planKey: 'annual-pro',
      key: 'annual-pro-12m',
      displayName: 'Annual (12 months)',
      durationValue: 12,
      durationUnit: 'months',
      externalProductId: 'price_ABC123'
    });
    ```

=== ".NET"
    ```csharp
    await subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
        PlanKey: "annual-pro",
        Key: "annual-pro-12m",
        DisplayName: "Annual (12 months)",
        DurationValue: 12,
        DurationUnit: "months",
        ExternalProductId: "price_ABC123"
    ));
    ```

### updateBillingCycle

#### Description
 Updates mutable fields (display name, description, duration config, pricing metadata) on an existing cycle.

#### Signature

=== "TypeScript"
    ```typescript
    updateBillingCycle(key: string, dto: UpdateBillingCycleDto): Promise<BillingCycleDto>
    ```

=== ".NET"
    ```csharp
    Task<BillingCycleDto> UpdateBillingCycleAsync(string key, UpdateBillingCycleDto dto)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key to update. |
| `dto` | `UpdateBillingCycleDto` | Yes | Partial update object. |

#### Input Properties

=== "TypeScript"
    All fields mirror `CreateBillingCycleDto` but are optional. If `durationUnit` is set to `forever`, `durationValue` must be omitted.

=== ".NET"
    All properties mirror `CreateBillingCycleDto` but are optional. If `DurationUnit` is set to `forever`, `DurationValue` must be omitted.

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto>` – updated cycle snapshot.

=== ".NET"
    `Task<BillingCycleDto>` – updated cycle snapshot.

#### Return Properties

=== "TypeScript"
    - Same `BillingCycleDto` fields described in `createBillingCycle`.

=== ".NET"
    - Same `BillingCycleDto` fields described in `createBillingCycle`.

#### Expected Results
- Validates DTO.
- Loads cycle, applies permissible fields, saves.

#### Example

=== "TypeScript"
    ```typescript
    await billingCycles.updateBillingCycle('annual-pro-12m', {
      displayName: 'Annual Plan (12 months)',
      externalProductId: 'price_UPDATED'
    });
    ```

=== ".NET"
    ```csharp
    await subscrio.BillingCycles.UpdateBillingCycleAsync("annual-pro-12m", new UpdateBillingCycleDto(
        DisplayName: "Annual Plan (12 months)",
        ExternalProductId: "price_UPDATED"
    ));
    ```

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid. |
| `NotFoundError` | Cycle missing. |

### getBillingCycle

#### Description
 Retrieves a single billing cycle by key (returns `null` if not found).

#### Signature

=== "TypeScript"
    ```typescript
    getBillingCycle(key: string): Promise<BillingCycleDto | null>
    ```

=== ".NET"
    ```csharp
    Task<BillingCycleDto?> GetBillingCycleAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key. |

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto | null>`

=== ".NET"
    `Task<BillingCycleDto?>`

#### Return Properties

=== "TypeScript"
    - `BillingCycleDto` shape (see `createBillingCycle`) or `null` when not found.

=== ".NET"
    - `BillingCycleDto` shape (see `createBillingCycle`) or `null` when not found.

#### Expected Results
- Loads cycle; if stored plan reference is missing (data corruption), throws `NotFoundError`.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing or plan reference cannot be resolved. |

#### Example

=== "TypeScript"
    ```typescript
    const cycle = await billingCycles.getBillingCycle('annual-pro-12m');
    ```

=== ".NET"
    ```csharp
    var cycle = await subscrio.BillingCycles.GetBillingCycleAsync("annual-pro-12m");
    ```

### getBillingCyclesByPlan

#### Description
 Lists all billing cycles belonging to a plan.

#### Signature

=== "TypeScript"
    ```typescript
    getBillingCyclesByPlan(planKey: string): Promise<BillingCycleDto[]>
    ```

=== ".NET"
    ```csharp
    Task<List<BillingCycleDto>> GetBillingCyclesByPlanAsync(string planKey)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan identifier. |

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto[]>`

=== ".NET"
    `Task<List<BillingCycleDto>>`

#### Return Properties

=== "TypeScript"
    - Array of `BillingCycleDto` entries scoped to the plan.

=== ".NET"
    - `List<BillingCycleDto>` scoped to the plan.

#### Expected Results
- Ensures plan exists.
- Returns all cycles mapped to the plan (with derived product key).

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

#### Example

=== "TypeScript"
    ```typescript
    const cycles = await billingCycles.getBillingCyclesByPlan('annual-pro');
    ```

=== ".NET"
    ```csharp
    var cycles = await subscrio.BillingCycles.GetBillingCyclesByPlanAsync("annual-pro");
    ```

### listBillingCycles

#### Description
 Paginates billing cycles with optional filters.

#### Signature

=== "TypeScript"
    ```typescript
    listBillingCycles(filters?: BillingCycleFilterDto): Promise<BillingCycleDto[]>
    ```

=== ".NET"
    ```csharp
    Task<List<BillingCycleDto>> ListBillingCyclesAsync(BillingCycleFilterDto? filters = null)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `BillingCycleFilterDto` | No | Status, duration unit, plan key, search, pagination, sorting. |

#### Input Properties

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `planKey` | `string` | Limit to a plan. |
    | `status` | `'active'` or `'archived'` | Filter by state. |
    | `durationUnit` | `'days'`, `'weeks'`, `'months'`, `'years'`, `'forever'` | Filter by unit. |
    | `search` | `string` | Text search across key/display name. |
    | `limit` | `number` | 1–100 (default 50). |
    | `offset` | `number` | ≥0 (default 0). |
    | `sortBy` | `'displayName'` or `'createdAt'` | Sort column. |
    | `sortOrder` | `'asc'` or `'desc'` | Sort direction, default `'asc'`. |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `PlanKey` | `string` | Limit to a plan. |
    | `Status` | `string` | `active` or `archived`. |
    | `DurationUnit` | `string` | Filter by unit. |
    | `Search` | `string` | Text search across key/display name. |
    | `Limit` | `int` | 1–100 (default 50). |
    | `Offset` | `int` | ≥0 (default 0). |
    | `SortBy` | `BillingCycleSortBy` | Sort column. |
    | `SortOrder` | `SortOrder` | Sort direction, default `asc`. |

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto[]>`

=== ".NET"
    `Task<List<BillingCycleDto>>`

#### Return Properties

=== "TypeScript"
    - Array of `BillingCycleDto` entries respecting the supplied filters.

=== ".NET"
    - `List<BillingCycleDto>` respecting the supplied filters.

#### Expected Results
- Validates filters.
- Executes query, returning DTO array (same schema as `createBillingCycle` result).

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters invalid. |

#### Example

=== "TypeScript"
    ```typescript
    const paged = await billingCycles.listBillingCycles({
      status: 'active',
      durationUnit: 'months',
      limit: 20
    });
    ```

=== ".NET"
    ```csharp
    var paged = await subscrio.BillingCycles.ListBillingCyclesAsync(new BillingCycleFilterDto(
        Status: "active",
        DurationUnit: "months",
        Limit: 20
    ));
    ```

### archiveBillingCycle

#### Description
 Marks a billing cycle as archived (cannot be used for new subscriptions).

#### Signature

=== "TypeScript"
    ```typescript
    archiveBillingCycle(key: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task ArchiveBillingCycleAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key. |

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Return Properties

=== "TypeScript"
    - None.

=== ".NET"
    - None.

#### Expected Results
- Loads cycle, sets status `archived`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |

#### Example

=== "TypeScript"
    ```typescript
    await billingCycles.archiveBillingCycle('annual-pro-12m');
    ```

=== ".NET"
    ```csharp
    await subscrio.BillingCycles.ArchiveBillingCycleAsync("annual-pro-12m");
    ```

### unarchiveBillingCycle

#### Description
 Restores an archived cycle to `active`.

#### Signature

=== "TypeScript"
    ```typescript
    unarchiveBillingCycle(key: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task UnarchiveBillingCycleAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key. |

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Return Properties

=== "TypeScript"
    - None.

=== ".NET"
    - None.

#### Expected Results
- Loads cycle, sets status `active`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |

#### Example

=== "TypeScript"
    ```typescript
    await billingCycles.unarchiveBillingCycle('annual-pro-12m');
    ```

=== ".NET"
    ```csharp
    await subscrio.BillingCycles.UnarchiveBillingCycleAsync("annual-pro-12m");
    ```

### deleteBillingCycle

#### Description
 Permanently deletes a billing cycle after ensuring it is archived and unused by subscriptions or plan transitions.

#### Signature

=== "TypeScript"
    ```typescript
    deleteBillingCycle(key: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task DeleteBillingCycleAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle to delete. |

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Return Properties

=== "TypeScript"
    - None.

=== ".NET"
    - None.

#### Expected Results
- Loads cycle, calls `billingCycle.canDelete()` (requires archived status).
- Verifies no subscriptions reference the cycle.
- Ensures no plan has `onExpireTransitionToBillingCycleKey` pointing to it.
- Deletes record.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |
| `DomainError` | Cycle still active or referenced. |

#### Example

=== "TypeScript"
    ```typescript
    await billingCycles.deleteBillingCycle('legacy-quarterly');
    ```

=== ".NET"
    ```csharp
    await subscrio.BillingCycles.DeleteBillingCycleAsync("legacy-quarterly");
    ```

### calculateNextPeriodEnd

#### Description
 Computes the next period end for a billing cycle, given the current period end.

#### Signature

=== "TypeScript"
    ```typescript
    calculateNextPeriodEnd(
      billingCycleKey: string,
      currentPeriodEnd: Date
    ): Promise<Date | null>
    ```

=== ".NET"
    ```csharp
    Task<DateTime?> CalculateNextPeriodEndAsync(string billingCycleKey, DateTime currentPeriodEnd)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `billingCycleKey` | `string` | Yes | Cycle to use for calculation. |
| `currentPeriodEnd` | `Date` | Yes | Current period end date. |

#### Returns

=== "TypeScript"
    `Promise<Date | null>` – `null` for `forever` cycles.

=== ".NET"
    `Task<DateTime?>` – `null` for `forever` cycles.

#### Return Properties

=== "TypeScript"
    - `Date`: calculated next period end.
    - `null`: returned when the cycle duration unit is `forever`.

=== ".NET"
    - `DateTime`: calculated next period end.
    - `null`: returned when the cycle duration unit is `forever`.

#### Expected Results
- Loads cycle, applies duration arithmetic (e.g., add N months) or returns `null` when unit is `forever`.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |

#### Example

=== "TypeScript"
    ```typescript
    const nextEnd = await billingCycles.calculateNextPeriodEnd(
      'annual-pro-12m',
      new Date('2025-01-01T00:00:00Z')
    );
    ```

=== ".NET"
    ```csharp
    var nextEnd = await subscrio.BillingCycles.CalculateNextPeriodEndAsync(
        "annual-pro-12m",
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    );
    ```

### getBillingCyclesByDurationUnit

#### Description
 Provides all cycles already stored with a specific duration unit.

#### Signature

=== "TypeScript"
    ```typescript
    getBillingCyclesByDurationUnit(durationUnit: DurationUnit): Promise<BillingCycleDto[]>
    ```

=== ".NET"
    ```csharp
    Task<List<BillingCycleDto>> GetBillingCyclesByDurationUnitAsync(DurationUnit durationUnit)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `durationUnit` | `DurationUnit` | Yes | `'days'`, `'weeks'`, `'months'`, `'years'`, or `'forever'`. |

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto[]>`

=== ".NET"
    `Task<List<BillingCycleDto>>`

#### Return Properties

=== "TypeScript"
    - Array of `BillingCycleDto` entries limited to the requested duration unit.

=== ".NET"
    - `List<BillingCycleDto>` limited to the requested duration unit.

#### Expected Results
- Filters existing cycles by unit (no errors thrown).

#### Example

=== "TypeScript"
    ```typescript
    const monthlyCycles = await billingCycles.getBillingCyclesByDurationUnit('months');
    ```

=== ".NET"
    ```csharp
    var monthlyCycles = await subscrio.BillingCycles.GetBillingCyclesByDurationUnitAsync(DurationUnit.Months);
    ```

### getDefaultBillingCycles

#### Description
 Retrieves pre-installed cycles (monthly/quarterly/yearly) when present.

#### Signature

=== "TypeScript"
    ```typescript
    getDefaultBillingCycles(): Promise<BillingCycleDto[]>
    ```

=== ".NET"
    ```csharp
    Task<List<BillingCycleDto>> GetDefaultBillingCyclesAsync()
    ```

#### Returns

=== "TypeScript"
    `Promise<BillingCycleDto[]>`

=== ".NET"
    `Task<List<BillingCycleDto>>`

#### Return Properties

=== "TypeScript"
    - Array of default `BillingCycleDto` entries that were seeded (or empty).

=== ".NET"
    - `List<BillingCycleDto>` of defaults that were seeded (or empty).

#### Expected Results
- Attempts to load keys such as `monthly`, `quarterly`, `yearly`; returns whichever exist.

#### Potential Errors
- None (returns empty array when defaults not installed).

#### Example

=== "TypeScript"
    ```typescript
    const defaults = await billingCycles.getDefaultBillingCycles();
    ```

=== ".NET"
    ```csharp
    var defaults = await subscrio.BillingCycles.GetDefaultBillingCyclesAsync();
    ```

## Related Workflows
- Plans must exist before creating billing cycles (`PlanManagementService`).
- Subscriptions reference billing cycles; deletion is blocked when subscriptions are present (`SubscriptionManagementService`).
- Stripe integration uses `externalProductId` to map cycles to Stripe prices (`StripeIntegrationService`).
