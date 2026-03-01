# Customer Management Service Reference

## Service Overview
Customers represent your end users or tenant accounts. This service manages creation, updates, lifecycle transitions (active/archived), uniqueness of customer keys, and optional `externalBillingId` (e.g., Stripe customer ID). Customers must be archived before deletion to ensure downstream data (subscriptions, invoices) can be reviewed.

- Customer keys are provided by your system and immutable once stored.
- `externalBillingId` is optional but must remain unique when present.
- Delete operations require the customer to be archived and pass entity `canDelete()` checks.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const customers = subscrio.customers;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `createCustomer` | Creates a new customer record | `Promise<CustomerDto>` |
| `updateCustomer` | Updates mutable fields | `Promise<CustomerDto>` |
| `getCustomer` | Retrieves a customer by key | `Promise<CustomerDto | null>` |
| `listCustomers` | Lists customers with filters | `Promise<CustomerDto[]>` |
| `archiveCustomer` | Archives a customer | `Promise<void>` |
| `unarchiveCustomer` | Reactivates an archived customer | `Promise<void>` |
| `deleteCustomer` | Deletes an archived customer | `Promise<void>` |

## Method Reference

### createCustomer

#### Description
 Validates a new customer payload, ensures the key and `externalBillingId` (if provided) are unique, and persists the customer with `active` status.

#### Signature
```typescript
createCustomer(dto: CreateCustomerDto): Promise<CustomerDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreateCustomerDto` | Yes | Customer definition supplied by your app. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Immutable identifier (1–255 chars). |
| `displayName` | `string` | No | Optional name (≤255 chars). |
| `email` | `string` | No | Valid email for billing contact. |
| `externalBillingId` | `string` | No | Unique ID from payment processor (≤255 chars). |
| `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

#### Returns
`Promise<CustomerDto>` – persisted customer snapshot.

#### Return Properties

| Field | Type | Description |
| --- | --- | --- |
| `key` | `string` | Customer key. |
| `displayName` | `string | null` | Display label. |
| `email` | `string | null` | Billing email. |
| `externalBillingId` | `string | null` | Stripe/processor customer ID. |
| `status` | `string` | `active`, `archived`, etc. |
| `metadata` | `Record<string, unknown> | null` | Stored metadata. |
| `createdAt` | `string` | ISO timestamp. |
| `updatedAt` | `string` | ISO timestamp. |

#### Expected Results
- Validates DTO via schema.
- Ensures customer key and `externalBillingId` (if provided) are unique.
- Persists customer with `status = 'active'`.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid. |
| `ConflictError` | Key or `externalBillingId` already in use. |

#### Example
```typescript
await customers.createCustomer({
  key: 'cust_123',
  displayName: 'Acme Corp',
  email: 'billing@acme.test'
});
```

### updateCustomer

#### Description
 Applies partial updates to display name, email, `externalBillingId`, or metadata.

#### Signature
```typescript
updateCustomer(key: string, dto: UpdateCustomerDto): Promise<CustomerDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key to mutate. |
| `dto` | `UpdateCustomerDto` | Yes | Partial update payload. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `displayName` | `string` | No | Updated label (≤255 chars). |
| `email` | `string` | No | Valid email address. |
| `externalBillingId` | `string` | No | Unique processor/customer ID. |
| `metadata` | `Record<string, unknown>` | No | Replaces stored metadata blob. |

#### Returns
`Promise<CustomerDto>` – updated snapshot (same fields as above).

#### Expected Results
- Validates DTO.
- Loads customer by key.
- Ensures new `externalBillingId` is unique before saving.
- Applies updates and refreshes `updatedAt`.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid. |
| `NotFoundError` | Customer key not found. |
| `ConflictError` | `externalBillingId` already used elsewhere. |

#### Example
```typescript
await customers.updateCustomer('cust_123', {
  displayName: 'Acme Corp (2025)',
  metadata: { segment: 'enterprise' }
});
```

### getCustomer

#### Description
 Fetches a customer by key; returns `null` when missing.

#### Signature
```typescript
getCustomer(key: string): Promise<CustomerDto | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key. |

#### Returns
`Promise<CustomerDto | null>`

#### Return Properties
- `CustomerDto` shape described in `createCustomer`.

#### Expected Results
- Reads from repository and returns DTO; `null` when key missing.

#### Potential Errors

| Error | When |
| --- | --- |
| _None_ | Method returns `null` when the customer is missing. |

#### Example
```typescript
const customer = await customers.getCustomer('cust_123');
if (!customer) throw new Error('Missing customer');
```

### listCustomers

#### Description
 Lists customers with optional status/search filters and pagination.

#### Signature
```typescript
listCustomers(filters?: CustomerFilterDto): Promise<CustomerDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `CustomerFilterDto` | No | Status filter, search term, pagination, sorting. |

#### Input Properties

| Field | Type | Description |
| --- | --- | --- |
| `status` | `'active' | 'archived' | 'suspended'` | Filter by lifecycle (default all). |
| `search` | `string` | Matches key, displayName, or email. |
| `sortBy` | `'displayName' | 'key' | 'createdAt'` | Sort column. |
| `sortOrder` | `'asc' | 'desc'` | Sort direction, default `'asc'`. |
| `limit` | `number` | 1–100 (default 50). |
| `offset` | `number` | ≥0 (default 0). |

#### Returns
`Promise<CustomerDto[]>`

#### Return Properties
- Array of `CustomerDto` entries (see `createCustomer`).

#### Expected Results
- Validates filters and returns DTO array.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters invalid. |

#### Example
```typescript
const customersPage = await customers.listCustomers({
  status: 'active',
  search: 'acme',
  limit: 25
});
```

### archiveCustomer

#### Description
 Marks a customer as archived (no new subscriptions should be issued).

#### Signature
```typescript
archiveCustomer(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key. |

#### Returns
`Promise<void>`

#### Expected Results
- Loads customer, calls entity `archive()`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer missing. |

#### Example
```typescript
await customers.archiveCustomer('cust_legacy');
```

### unarchiveCustomer

#### Description
 Restores an archived customer to `active`.

#### Signature
```typescript
unarchiveCustomer(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key. |

#### Returns
`Promise<void>`

#### Expected Results
- Loads customer, calls `unarchive()`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer missing. |

#### Example
```typescript
await customers.unarchiveCustomer('cust_legacy');
```

### deleteCustomer

#### Description
 Permanently deletes a customer that has already been archived and passes domain checks.

#### Signature
```typescript
deleteCustomer(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer to delete. |

#### Returns
`Promise<void>`

#### Expected Results
- Loads customer, ensures `customer.canDelete()` (must be archived and free of blocking relationships).
- Deletes record via repository.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer missing. |
| `DomainError` | Customer cannot be deleted (still active or domain rule failed). |

#### Example
```typescript
await customers.deleteCustomer('cust_retired');
```

## Related Workflows
- Subscriptions reference customers by ID; deleting a customer does not cascade—clean related subscriptions manually if needed.
- Stripe integration (`StripeIntegrationService`) expects `externalBillingId` to store the Stripe customer ID.
- Feature checker APIs use customer keys to resolve entitlements; keep keys stable for the life of the user/account.
