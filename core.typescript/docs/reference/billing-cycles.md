# Billing Cycle Management Service Reference

## Service Overview
Billing cycles define how plans renew (duration, cadence, and external price IDs). Each cycle belongs to a plan, exposes derived `planKey`/`productKey` in DTOs, and enforces delete guards whenever subscriptions or plan transitions reference it.

- Duration units: `days`, `weeks`, `months`, `years`, or `forever` (when `forever`, `durationValue` must be omitted).
- Cycles can expose `externalProductId` (e.g., Stripe price) for payment processor mappings.
- Delete operations require the cycle to be archived and unused by subscriptions or plan transition settings.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const billingCycles = subscrio.billingCycles;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `createBillingCycle` | Creates a cycle for an existing plan | `Promise<BillingCycleDto>` |
| `updateBillingCycle` | Updates mutable fields on a cycle | `Promise<BillingCycleDto>` |
| `getBillingCycle` | Retrieves a cycle by key | Promise&lt;BillingCycleDto or null&gt; |
| `getBillingCyclesByPlan` | Lists cycles for a plan | `Promise<BillingCycleDto[]>` |
| `listBillingCycles` | Lists cycles with filters/pagination | `Promise<BillingCycleDto[]>` |
| `archiveBillingCycle` | Archives a cycle | `Promise<void>` |
| `unarchiveBillingCycle` | Reactivates an archived cycle | `Promise<void>` |
| `deleteBillingCycle` | Deletes an archived, unused cycle | `Promise<void>` |
| `calculateNextPeriodEnd` | Computes next renewal end date | Promise&lt;Date or null&gt; |
| `getBillingCyclesByDurationUnit` | Filters cycles by duration unit | `Promise<BillingCycleDto[]>` |
| `getDefaultBillingCycles` | Loads pre-installed defaults (monthly/quarterly/yearly) | `Promise<BillingCycleDto[]>` |

## Method Reference

### createBillingCycle

#### Description
 Validates a new billing cycle payload, ensures the plan exists, and persists the cycle with `active` status.

#### Signature
```typescript
createBillingCycle(dto: CreateBillingCycleDto): Promise<BillingCycleDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreateBillingCycleDto` | Yes | Cycle definition for an existing plan. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan owning the cycle (lowercase alphanumeric + `-`). |
| `key` | `string` | Yes | Globally unique billing cycle key. |
| `displayName` | `string` | Yes | 1–255 char label. |
| `description` | `string` | No | ≤1000 chars. |
| `durationValue` | `number` | Conditional | Required unless `durationUnit` is `forever`; positive integer. |
| `durationUnit` | `'days', 'weeks', 'months', 'years', 'forever'` | Yes | Renewal cadence. |
| `externalProductId` | `string` | No | Stripe price or other external ID (≤255 chars). |

#### Returns

`Promise<BillingCycleDto>` – persisted cycle snapshot.

#### Return Properties

| Field | Type | Description |
| --- | --- | --- |
| `key` | `string` | Cycle key. |
| `planKey` | `string` | Owning plan key. |
| `productKey` | `string` | Derived from plan. |
| `displayName` | `string` | Display label. |
| `description` | `string` or `null` | Optional description. |
| `status` | `string` | `active` or `archived`. |
| `durationValue` | `number` or `null` | `null` when unit is `forever`. |
| `durationUnit` | `string` | Duration unit. |
| `externalProductId` | `string` or `null` | Payment processor price ID. |
| `createdAt` | `string` | ISO timestamp. |
| `updatedAt` | `string` | ISO timestamp. |

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

### updateBillingCycle

#### Description
 Updates mutable fields (display name, description, duration config, pricing metadata) on an existing cycle.

#### Signature
```typescript
updateBillingCycle(key: string, dto: UpdateBillingCycleDto): Promise<BillingCycleDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key to update. |
| `dto` | `UpdateBillingCycleDto` | Yes | Partial update object. |

#### Input Properties
All fields mirror `CreateBillingCycleDto` but are optional. If `durationUnit` is set to `forever`, `durationValue` must be omitted.

#### Returns

`Promise<BillingCycleDto>` – updated cycle snapshot.

#### Return Properties
- Same `BillingCycleDto` fields described in `createBillingCycle`.

#### Expected Results
- Validates DTO.
- Loads cycle, applies permissible fields, saves.

#### Example
```typescript
await billingCycles.updateBillingCycle('annual-pro-12m', {
  displayName: 'Annual Plan (12 months)',
  externalProductId: 'price_UPDATED'
});
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
```typescript
getBillingCycle(key: string): Promise<BillingCycleDto | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key. |

#### Returns

`Promise<BillingCycleDto | null>`

#### Return Properties
- `BillingCycleDto` shape (see `createBillingCycle`) or `null` when not found.

#### Expected Results
- Loads cycle; if stored plan reference is missing (data corruption), throws `NotFoundError`.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing or plan reference cannot be resolved. |

#### Example
```typescript
const cycle = await billingCycles.getBillingCycle('annual-pro-12m');
```

### getBillingCyclesByPlan

#### Description
 Lists all billing cycles belonging to a plan.

#### Signature
```typescript
getBillingCyclesByPlan(planKey: string): Promise<BillingCycleDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `planKey` | `string` | Yes | Plan identifier. |

#### Returns

`Promise<BillingCycleDto[]>`

#### Return Properties
- Array of `BillingCycleDto` entries scoped to the plan.

#### Expected Results
- Ensures plan exists.
- Returns all cycles mapped to the plan (with derived product key).

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Plan missing. |

#### Example
```typescript
const cycles = await billingCycles.getBillingCyclesByPlan('annual-pro');
```

### listBillingCycles

#### Description
 Paginates billing cycles with optional filters.

#### Signature
```typescript
listBillingCycles(filters?: BillingCycleFilterDto): Promise<BillingCycleDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `BillingCycleFilterDto` | No | Status, duration unit, plan key, search, pagination, sorting. |

#### Input Properties

| Field | Type | Description |
| --- | --- | --- |
| `planKey` | `string` | Limit to a plan. |
| `status` | `'active'` or `'archived'` | Filter by state. |
| `durationUnit` | `'days', 'weeks', 'months', 'years', 'forever'` | Filter by unit. |
| `search` | `string` | Text search across key/display name. |
| `limit` | `number` | 1–100 (default 50). |
| `offset` | `number` | ≥0 (default 0). |
| `sortBy` | `'displayName'` or `'createdAt'` | Sort column. |
| `sortOrder` | `'asc'` or `'desc'` | Sort direction, default `'asc'`. |

#### Returns

`Promise<BillingCycleDto[]>`

#### Return Properties
- Array of `BillingCycleDto` entries respecting the supplied filters.

#### Expected Results
- Validates filters.
- Executes query, returning DTO array (same schema as `createBillingCycle` result).

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters invalid. |

#### Example
```typescript
const paged = await billingCycles.listBillingCycles({
  status: 'active',
  durationUnit: 'months',
  limit: 20
});
```

### archiveBillingCycle

#### Description
 Marks a billing cycle as archived (cannot be used for new subscriptions).

#### Signature
```typescript
archiveBillingCycle(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key. |

#### Returns

`Promise<void>`

#### Return Properties
- None.

#### Expected Results
- Loads cycle, sets status `archived`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |

#### Example
```typescript
await billingCycles.archiveBillingCycle('annual-pro-12m');
```

### unarchiveBillingCycle

#### Description
 Restores an archived cycle to `active`.

#### Signature
```typescript
unarchiveBillingCycle(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle key. |

#### Returns

`Promise<void>`

#### Return Properties
- None.

#### Expected Results
- Loads cycle, sets status `active`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |

#### Example
```typescript
await billingCycles.unarchiveBillingCycle('annual-pro-12m');
```

### deleteBillingCycle

#### Description
 Permanently deletes a billing cycle after ensuring it is archived and unused by subscriptions or plan transitions.

#### Signature
```typescript
deleteBillingCycle(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Cycle to delete. |

#### Returns

`Promise<void>`

#### Return Properties
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
```typescript
await billingCycles.deleteBillingCycle('legacy-quarterly');
```

### calculateNextPeriodEnd

#### Description
 Computes the next period end for a billing cycle, given the current period end.

#### Signature
```typescript
calculateNextPeriodEnd(
  billingCycleKey: string,
  currentPeriodEnd: Date
): Promise<Date | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `billingCycleKey` | `string` | Yes | Cycle to use for calculation. |
| `currentPeriodEnd` | `Date` | Yes | Current period end date. |

#### Returns

`Promise<Date | null>` – `null` for `forever` cycles.

#### Return Properties
- `Date`: calculated next period end.
- `null`: returned when the cycle duration unit is `forever`.

#### Expected Results
- Loads cycle, applies duration arithmetic (e.g., add N months) or returns `null` when unit is `forever`.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Cycle missing. |

#### Example
```typescript
const nextEnd = await billingCycles.calculateNextPeriodEnd(
  'annual-pro-12m',
  new Date('2025-01-01T00:00:00Z')
);
```

### getBillingCyclesByDurationUnit

#### Description
 Provides all cycles already stored with a specific duration unit.

#### Signature
```typescript
getBillingCyclesByDurationUnit(durationUnit: DurationUnit): Promise<BillingCycleDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `durationUnit` | `DurationUnit` | Yes | `'days'`, `'weeks'`, `'months'`, `'years'`, or `'forever'`. |

#### Returns

`Promise<BillingCycleDto[]>`

#### Return Properties
- Array of `BillingCycleDto` entries limited to the requested duration unit.

#### Expected Results
- Filters existing cycles by unit (no errors thrown).

#### Example
```typescript
const monthlyCycles = await billingCycles.getBillingCyclesByDurationUnit('months');
```

### getDefaultBillingCycles

#### Description
 Retrieves pre-installed cycles (monthly/quarterly/yearly) when present.

#### Signature
```typescript
getDefaultBillingCycles(): Promise<BillingCycleDto[]>
```

#### Returns

`Promise<BillingCycleDto[]>`

#### Return Properties
- Array of default `BillingCycleDto` entries that were seeded (or empty array).

#### Expected Results
- Attempts to load keys such as `monthly`, `quarterly`, `yearly`; returns whichever exist.

#### Potential Errors
- None (returns empty array when defaults not installed).

#### Example
```typescript
const defaults = await billingCycles.getDefaultBillingCycles();
```

## Related Workflows
- Plans must exist before creating billing cycles (`PlanManagementService`).
- Subscriptions reference billing cycles; deletion is blocked when subscriptions are present (`SubscriptionManagementService`).
- Stripe integration uses `externalProductId` to map cycles to Stripe prices (`StripeIntegrationService`).
