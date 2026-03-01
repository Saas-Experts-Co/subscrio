# Product Management Service Reference

## Service Overview
The Product Management Service governs product lifecycles inside Subscrio: creation, updates, archival, deletion, and feature associations. Products sit at the top of the hierarchy—plans, billing cycles, and subscriptions all reference them.

- Product keys are global, immutable identifiers.
- Deletion is guarded by domain rules: products must be archived and free of plans before removal.
- Features must be associated with a product before plans under it can set feature values.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const products = subscrio.products;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `createProduct` | Validates and persists a new product | `Promise<ProductDto>` |
| `updateProduct` | Updates mutable fields on an existing product | `Promise<ProductDto>` |
| `getProduct` | Fetches a product by key | `Promise<ProductDto | null>` |
| `listProducts` | Lists products with filter/pagination | `Promise<ProductDto[]>` |
| `deleteProduct` | Permanently deletes an archived product without plans | `Promise<void>` |
| `archiveProduct` | Marks a product as archived | `Promise<ProductDto>` |
| `unarchiveProduct` | Re-activates an archived product | `Promise<ProductDto>` |
| `associateFeature` | Links a global feature to this product | `Promise<void>` |
| `dissociateFeature` | Removes an existing feature link | `Promise<void>` |

## Method Reference

### createProduct

#### Description
 Creates a new product after validating key format, display name, and optional metadata. Rejects duplicate keys.

#### Signature

```typescript
createProduct(dto: CreateProductDto): Promise<ProductDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `dto` | `CreateProductDto` | Yes | Contains the product key, display name, optional description, and metadata. |

#### Input Properties

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
| `displayName` | `string` | Yes | 1–255 char label. |
| `description` | `string` | No | ≤1000 chars. |
| `metadata` | `Record<string, unknown>` | No | JSON-safe metadata blob. |

#### Returns

Resolves with the persisted `ProductDto`, including timestamps and status (`active`).

#### Return Properties

| Field | Type | Description |
| --- | --- | --- |
| `id` | `string` | UUIDv7 identifier. |
| `key` | `string` | Immutable product key. |
| `displayName` | `string` | Display name. |
| `description` | <code>string &#124; null</code> | Optional description. |
| `status` | `string` | `active`, `inactive`, or `archived`. |
| `metadata` | <code>Record&lt;string, unknown&gt; &#124; null</code> | Metadata blob. |
| `createdAt` | `string` | ISO timestamp. |
| `updatedAt` | `string` | ISO timestamp. |

#### Expected Results

- Validates input via Zod schema.
- Ensures no product already exists with the provided key.
- Creates a domain entity with status `active` and persists it.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `ValidationError` | DTO fails schema checks (bad key format, display name length, etc.). |
| `ConflictError` | A product with the same key already exists. |

#### Example

```typescript
const product = await products.createProduct({
  key: 'pro-suite',
  displayName: 'Pro Suite',
  description: 'Advanced tier',
  metadata: { tier: 'pro' }
});
```

### updateProduct

#### Description
 Applies partial updates to display name, description, or metadata of an existing product.

#### Signature

```typescript
updateProduct(key: string, dto: UpdateProductDto): Promise<ProductDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Immutable product key to update. |
| `dto` | `UpdateProductDto` | Yes | Partial data containing new display name, description, or metadata. |

#### Input Properties

| Field | Type | Description |
| --- | --- | --- |
| `displayName` | `string` | Updated label (1–255 chars). |
| `description` | `string` | Replacement description (≤1000 chars). |
| `metadata` | `Record<string, unknown>` | Full metadata blob (overwrites stored value). |

#### Returns

Resolves with the updated `ProductDto`.

#### Return Properties

Same `ProductDto` fields listed under `createProduct`.

#### Expected Results

- Validates the DTO.
- Loads the product by key and mutates allowed fields.
- Updates `updatedAt` and persists the entity.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `ValidationError` | DTO fields fail schema (e.g., display name too short). |
| `NotFoundError` | Product key does not exist. |

#### Example

```typescript
const updated = await products.updateProduct('pro-suite', {
  displayName: 'Pro Suite (2025)',
  metadata: { tier: 'pro', version: '2025.1' }
});
```

### getProduct

#### Description
Fetches a product snapshot by key, returning `null` when it does not exist.

#### Signature
```typescript
getProduct(key: string): Promise<ProductDto | null>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Product key to retrieve. |

#### Returns
`Promise<ProductDto | null>`

#### Return Properties
- `ProductDto` fields defined under `createProduct`.
- `null` when the product is missing.

#### Expected Results
- Loads product by key and maps domain entity to DTO.

#### Potential Errors

| Error | When |
| --- | --- |
| _None_ | Method returns `null` for missing products. |

#### Example
```typescript
const product = await products.getProduct('starter');
```

### listProducts

#### Description
 Lists products with status, search, and pagination controls.

#### Signature

```typescript
listProducts(filters?: ProductFilterDto): Promise<ProductDto[]>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `filters` | `ProductFilterDto` | No | Optional filter object; defaults applied by schema (limit 50, offset 0, sort asc). |

#### Input Properties

| Field | Type | Description |
| --- | --- | --- |
| `status` | <code>'active' &#124; 'archived'</code> | Filter by lifecycle state. |
| `search` | `string` | Performs key/displayName search. |
| `limit` | `number` | 1–100, default 50. |
| `offset` | `number` | ≥0, default 0. |
| `sortBy` | <code>'displayName' &#124; 'createdAt'</code> | Sort column. |
| `sortOrder` | <code>'asc' &#124; 'desc'</code> | Sort direction; default `'asc'`. |

#### Returns

`Promise<ProductDto[]>` containing all products matching the filters.

#### Return Properties

- Each entry matches the `ProductDto` fields defined in `createProduct`.

#### Expected Results

- Validates filters.
- Delegates to repository for query and maps each entity to DTO.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `ValidationError` | Filters contain invalid values (e.g., limit > 100). |

#### Example

```typescript
const activeProducts = await products.listProducts({
  status: 'active',
  search: 'suite',
  limit: 25
});
```

### deleteProduct

#### Description
 Permanently deletes an archived product that has no associated plans.

#### Signature

```typescript
deleteProduct(key: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Product key targeted for deletion. |

#### Input Properties

- None beyond the raw key string.

#### Returns

Resolves with `void` after the product is deleted.

#### Return Properties

- `void`

#### Expected Results

- Fetches product and verifies it exists.
- Calls `product.canDelete()` (must be archived).
- Ensures the product has zero plans before deletion.
- Removes the product record.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `NotFoundError` | Product does not exist. |
| `DomainError` | Product is not archived or still has plans. |

#### Example

```typescript
await products.archiveProduct('legacy-tier');
await products.deleteProduct('legacy-tier');
```

### archiveProduct

#### Description
 Transitions a product to the archived state.

#### Signature

```typescript
archiveProduct(key: string): Promise<ProductDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Product key to archive. |

#### Input Properties

- None beyond the key string.

#### Returns

Resolved `ProductDto` reflecting the archived status.

#### Return Properties

- Same `ProductDto` fields listed under `createProduct`, with `status: 'archived'`.

#### Expected Results

- Loads product, calls the entity’s `archive()` method, persists, and returns DTO.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `NotFoundError` | Product key not found. |

#### Example

```typescript
const archived = await products.archiveProduct('starter');
console.log(archived.status); // 'archived'
```

### unarchiveProduct

#### Description
 Reverses `archiveProduct` by calling the entity’s `unarchive()` method and restoring the product to `active`.

#### Signature

```typescript
unarchiveProduct(key: string): Promise<ProductDto>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `key` | `string` | Yes | Product key previously archived. |

#### Input Properties

- None beyond the key string.

#### Returns

Updated `ProductDto` now active again.

#### Return Properties

- Same `ProductDto` fields listed under `createProduct`, with `status: 'active'`.

#### Expected Results

- Loads product, ensures it exists, calls `unarchive()`, and persists.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `NotFoundError` | Product key missing. |
| `NotFoundError` | Product key missing. |

#### Example

```typescript
await products.unarchiveProduct('starter');
```

### associateFeature

#### Description
 Links an existing feature to a product so plans under the product can set values for it.

#### Signature

```typescript
associateFeature(productKey: string, featureKey: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `productKey` | `string` | Yes | Product to update. |
| `featureKey` | `string` | Yes | Feature to associate. |

#### Input Properties

- None; both values are raw keys.

#### Returns

`Promise<void>`

#### Return Properties

- `void`

#### Expected Results

- Validates both product and feature exist.
- Inserts association in repository layer.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `NotFoundError` | Product or feature is missing. |

#### Example

```typescript
await products.associateFeature('pro-suite', 'max-users');
```

### dissociateFeature

#### Description
 Removes an existing feature association from a product.

#### Signature

```typescript
dissociateFeature(productKey: string, featureKey: string): Promise<void>
```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `productKey` | `string` | Yes | Product losing the feature. |
| `featureKey` | `string` | Yes | Feature to remove. |

#### Input Properties

- None; both parameters are raw keys.

#### Returns

`Promise<void>`

#### Return Properties

- `void`

#### Expected Results

- Validates both entities exist.
- Deletes the join row if present.

#### Potential Errors

| Error | When it is thrown |
| --- | --- |
| `NotFoundError` | Product or feature missing. |

#### Example

```typescript
await products.dissociateFeature('pro-suite', 'legacy-flag');
```

## Related Workflows
- Products must exist before you create plans (`PlanManagementService` references `productKey`).
- Deleting a product requires archival and removal of all plans to avoid `DomainError`.
- Feature associations determine which features plans under the product can set values for—coordinate with `FeatureManagementService`.
