# Customer Management Service Reference

## Service Overview
Customers represent your end users or tenant accounts. This service manages creation, updates, lifecycle transitions (active/archived), uniqueness of customer keys, and optional `externalBillingId` (e.g., Stripe customer ID). Customers must be archived before deletion to ensure downstream data (subscriptions, invoices) can be reviewed.

- Customer keys are provided by your system and immutable once stored.
- `externalBillingId` is optional but must remain unique when present.
- Delete operations require the customer to be archived and pass entity `canDelete()` checks.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const customers = subscrio.customers;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var customers = subscrio.Customers;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `createCustomer` | Creates a new customer record | `Promise<CustomerDto>` |
    | `updateCustomer` | Updates mutable fields | `Promise<CustomerDto>` |
    | `getCustomer` | Retrieves a customer by key | `Promise<CustomerDto \| null>` |
    | `listCustomers` | Lists customers with filters | `Promise<CustomerDto[]>` |
    | `archiveCustomer` | Archives a customer | `Promise<void>` |
    | `unarchiveCustomer` | Reactivates an archived customer | `Promise<void>` |
    | `deleteCustomer` | Deletes an archived customer | `Promise<void>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `CreateCustomerAsync` | Creates a new customer record | `Task<CustomerDto>` |
    | `UpdateCustomerAsync` | Updates mutable fields | `Task<CustomerDto>` |
    | `GetCustomerAsync` | Retrieves a customer by key | `Task<CustomerDto?>` |
    | `ListCustomersAsync` | Lists customers with filters | `Task<List<CustomerDto>>` |
    | `ArchiveCustomerAsync` | Archives a customer | `Task` |
    | `UnarchiveCustomerAsync` | Reactivates an archived customer | `Task` |
    | `DeleteCustomerAsync` | Deletes an archived customer | `Task` |

## Method Reference

### createCustomer

#### Description
 Validates a new customer payload, ensures the key and `externalBillingId` (if provided) are unique, and persists the customer with `active` status.

#### Signature

=== "TypeScript"
    ```typescript
    createCustomer(dto: CreateCustomerDto): Promise<CustomerDto>
    ```

=== ".NET"
    ```csharp
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreateCustomerDto` | Yes | Customer definition supplied by your app. |

#### Input Properties

=== "TypeScript"
    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Immutable identifier (1–255 chars). |
    | `displayName` | `string` | No | Optional name (≤255 chars). |
    | `email` | `string` | No | Valid email for billing contact. |
    | `externalBillingId` | `string` | No | Unique ID from payment processor (≤255 chars). |
    | `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

=== ".NET"
    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `Key` | `string` | Yes | Immutable identifier (1–255 chars). |
    | `DisplayName` | `string` | No | Optional name (≤255 chars). |
    | `Email` | `string` | No | Valid email for billing contact. |
    | `ExternalBillingId` | `string` | No | Unique ID from payment processor (≤255 chars). |
    | `Metadata` | `Dictionary<string, object?>` | No | JSON-safe metadata. |

#### Returns

=== "TypeScript"
    `Promise<CustomerDto>` – persisted customer snapshot.

=== ".NET"
    `Task<CustomerDto>` – persisted customer snapshot.

#### Return Properties

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `key` | `string` | Customer key. |
    | `displayName` | `string \| null` | Display label. |
    | `email` | `string \| null` | Billing email. |
    | `externalBillingId` | `string \| null` | Stripe/processor customer ID. |
    | `status` | `string` | `active`, `archived`, etc. |
    | `metadata` | `Record<string, unknown> \| null` | Stored metadata. |
    | `createdAt` | `string` | ISO timestamp. |
    | `updatedAt` | `string` | ISO timestamp. |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `Key` | `string` | Customer key. |
    | `DisplayName` | `string?` | Display label. |
    | `Email` | `string?` | Billing email. |
    | `ExternalBillingId` | `string?` | Stripe/processor customer ID. |
    | `Status` | `string` | `active`, `archived`, etc. |
    | `Metadata` | `Dictionary<string, object?>?` | Stored metadata. |
    | `CreatedAt` | `string` | ISO timestamp. |
    | `UpdatedAt` | `string` | ISO timestamp. |

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

=== "TypeScript"
    ```typescript
    await customers.createCustomer({
      key: 'cust_123',
      displayName: 'Acme Corp',
      email: 'billing@acme.test'
    });
    ```

=== ".NET"
    ```csharp
    await subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
        Key: "cust_123",
        DisplayName: "Acme Corp",
        Email: "billing@acme.test"
    ));
    ```

### updateCustomer

#### Description
 Applies partial updates to display name, email, `externalBillingId`, or metadata.

#### Signature

=== "TypeScript"
    ```typescript
    updateCustomer(key: string, dto: UpdateCustomerDto): Promise<CustomerDto>
    ```

=== ".NET"
    ```csharp
    Task<CustomerDto> UpdateCustomerAsync(string key, UpdateCustomerDto dto)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key to mutate. |
| `dto` | `UpdateCustomerDto` | Yes | Partial update payload. |

#### Input Properties

=== "TypeScript"
    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `displayName` | `string` | No | Updated label (≤255 chars). |
    | `email` | `string` | No | Valid email address. |
    | `externalBillingId` | `string` | No | Unique processor/customer ID. |
    | `metadata` | `Record<string, unknown>` | No | Replaces stored metadata blob. |

=== ".NET"
    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `DisplayName` | `string` | No | Updated label (≤255 chars). |
    | `Email` | `string` | No | Valid email address. |
    | `ExternalBillingId` | `string` | No | Unique processor/customer ID. |
    | `Metadata` | `Dictionary<string, object?>` | No | Replaces stored metadata blob. |

#### Returns

=== "TypeScript"
    `Promise<CustomerDto>` – updated snapshot (same fields as above).

=== ".NET"
    `Task<CustomerDto>` – updated snapshot (same fields as above).

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

=== "TypeScript"
    ```typescript
    await customers.updateCustomer('cust_123', {
      displayName: 'Acme Corp (2025)',
      metadata: { segment: 'enterprise' }
    });
    ```

=== ".NET"
    ```csharp
    await subscrio.Customers.UpdateCustomerAsync("cust_123", new UpdateCustomerDto(
        DisplayName: "Acme Corp (2025)",
        Metadata: new Dictionary<string, object?> { ["segment"] = "enterprise" }
    ));
    ```

### getCustomer

#### Description
 Fetches a customer by key; returns `null` when missing.

#### Signature

=== "TypeScript"
    ```typescript
    getCustomer(key: string): Promise<CustomerDto | null>
    ```

=== ".NET"
    ```csharp
    Task<CustomerDto?> GetCustomerAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key. |

#### Returns

=== "TypeScript"
    `Promise<CustomerDto | null>`

=== ".NET"
    `Task<CustomerDto?>`

#### Return Properties

=== "TypeScript"
    - `CustomerDto` shape described in `createCustomer`.

=== ".NET"
    - `CustomerDto` shape described in `createCustomer`.

#### Expected Results
- Reads from repository and returns DTO; `null` when key missing.

#### Potential Errors

| Error | When |
| --- | --- |
| _None_ | Method returns `null` when the customer is missing. |

#### Example

=== "TypeScript"
    ```typescript
    const customer = await customers.getCustomer('cust_123');
    if (!customer) throw new Error('Missing customer');
    ```

=== ".NET"
    ```csharp
    var customer = await subscrio.Customers.GetCustomerAsync("cust_123");
    if (customer == null) throw new InvalidOperationException("Missing customer");
    ```

### listCustomers

#### Description
 Lists customers with optional status/search filters and pagination.

#### Signature

=== "TypeScript"
    ```typescript
    listCustomers(filters?: CustomerFilterDto): Promise<CustomerDto[]>
    ```

=== ".NET"
    ```csharp
    Task<List<CustomerDto>> ListCustomersAsync(CustomerFilterDto? filters = null)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `CustomerFilterDto` | No | Status filter, search term, pagination, sorting. |

#### Input Properties

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `status` | `'active' \| 'archived' \| 'suspended'` | Filter by lifecycle (default all). |
    | `search` | `string` | Matches key, displayName, or email. |
    | `sortBy` | `'displayName' \| 'key' \| 'createdAt'` | Sort column. |
    | `sortOrder` | `'asc' \| 'desc'` | Sort direction, default `'asc'`. |
    | `limit` | `number` | 1–100 (default 50). |
    | `offset` | `number` | ≥0 (default 0). |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `Status` | `string` | Filter by lifecycle: `active`, `archived`, `suspended`. |
    | `Search` | `string` | Matches key, display name, or email. |
    | `SortBy` | `CustomerSortBy` | Sort column. |
    | `SortOrder` | `SortOrder` | Sort direction, default `asc`. |
    | `Limit` | `int` | 1–100 (default 50). |
    | `Offset` | `int` | ≥0 (default 0). |

#### Returns

=== "TypeScript"
    `Promise<CustomerDto[]>`

=== ".NET"
    `Task<List<CustomerDto>>`

#### Return Properties

=== "TypeScript"
    - Array of `CustomerDto` entries (see `createCustomer`).

=== ".NET"
    - `List<CustomerDto>` (see `createCustomer`).

#### Expected Results
- Validates filters and returns DTO array.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters invalid. |

#### Example

=== "TypeScript"
    ```typescript
    const customersPage = await customers.listCustomers({
      status: 'active',
      search: 'acme',
      limit: 25
    });
    ```

=== ".NET"
    ```csharp
    var customersPage = await subscrio.Customers.ListCustomersAsync(new CustomerFilterDto(
        Status: "active",
        Search: "acme",
        Limit: 25
    ));
    ```

### archiveCustomer

#### Description
 Marks a customer as archived (no new subscriptions should be issued).

#### Signature

=== "TypeScript"
    ```typescript
    archiveCustomer(key: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task ArchiveCustomerAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key. |

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Expected Results
- Loads customer, calls entity `archive()`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer missing. |

#### Example

=== "TypeScript"
    ```typescript
    await customers.archiveCustomer('cust_legacy');
    ```

=== ".NET"
    ```csharp
    await subscrio.Customers.ArchiveCustomerAsync("cust_legacy");
    ```

### unarchiveCustomer

#### Description
 Restores an archived customer to `active`.

#### Signature

=== "TypeScript"
    ```typescript
    unarchiveCustomer(key: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task UnarchiveCustomerAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer key. |

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Expected Results
- Loads customer, calls `unarchive()`, saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer missing. |

#### Example

=== "TypeScript"
    ```typescript
    await customers.unarchiveCustomer('cust_legacy');
    ```

=== ".NET"
    ```csharp
    await subscrio.Customers.UnarchiveCustomerAsync("cust_legacy");
    ```

### deleteCustomer

#### Description
 Permanently deletes a customer that has already been archived and passes domain checks.

#### Signature

=== "TypeScript"
    ```typescript
    deleteCustomer(key: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task DeleteCustomerAsync(string key)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Customer to delete. |

#### Returns

=== "TypeScript"
    `Promise<void>`

=== ".NET"
    `Task`

#### Expected Results
- Loads customer, ensures `customer.canDelete()` (must be archived and free of blocking relationships).
- Deletes record via repository.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Customer missing. |
| `DomainError` | Customer cannot be deleted (still active or domain rule failed). |

#### Example

=== "TypeScript"
    ```typescript
    await customers.deleteCustomer('cust_retired');
    ```

=== ".NET"
    ```csharp
    await subscrio.Customers.DeleteCustomerAsync("cust_retired");
    ```

## Related Workflows
- Subscriptions reference customers by ID; deleting a customer does not cascade—clean related subscriptions manually if needed.
- Stripe integration (`StripeIntegrationService`) expects `externalBillingId` to store the Stripe customer ID.
- Feature checker APIs use customer keys to resolve entitlements; keep keys stable for the life of the user/account.
