# Product Management Service Reference

## Service Overview
The Product Management Service governs product lifecycles inside Subscrio: creation, updates, archival, deletion, and feature associations. Products sit at the top of the hierarchy—plans, billing cycles, and subscriptions all reference them.

- Product keys are global, immutable identifiers.
- Deletion is guarded by domain rules: products must be archived and free of plans before removal.
- Features must be associated with a product before plans under it can set feature values.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const products = subscrio.products;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var products = subscrio.Products;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `createProduct` | Validates and persists a new product | `Promise<ProductDto>` |
    | `updateProduct` | Updates mutable fields on an existing product | `Promise<ProductDto>` |
    | `getProduct` | Fetches a product by key | `Promise<ProductDto \| null>` |
    | `listProducts` | Lists products with filter/pagination | `Promise<ProductDto[]>` |
    | `deleteProduct` | Permanently deletes an archived product without plans | `Promise<void>` |
    | `archiveProduct` | Marks a product as archived | `Promise<ProductDto>` |
    | `unarchiveProduct` | Re-activates an archived product | `Promise<ProductDto>` |
    | `associateFeature` | Links a global feature to this product | `Promise<void>` |
    | `dissociateFeature` | Removes an existing feature link | `Promise<void>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `CreateProductAsync` | Validates and persists a new product | `Task<ProductDto>` |
    | `UpdateProductAsync` | Updates mutable fields on an existing product | `Task<ProductDto>` |
    | `GetProductAsync` | Fetches a product by key | `Task<ProductDto?>` |
    | `ListProductsAsync` | Lists products with filter/pagination | `Task<List<ProductDto>>` |
    | `DeleteProductAsync` | Permanently deletes an archived product without plans | `Task` |
    | `ArchiveProductAsync` | Marks a product as archived | `Task<ProductDto>` |
    | `UnarchiveProductAsync` | Re-activates an archived product | `Task<ProductDto>` |
    | `AssociateFeatureAsync` | Links a global feature to this product | `Task` |
    | `DissociateFeatureAsync` | Removes an existing feature link | `Task` |

## Method Reference

### createProduct

#### Description
Creates a new product after validating key format, display name, and optional metadata. Rejects duplicate keys.

=== "TypeScript"
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
    `Promise<ProductDto>` – persisted product with timestamps and status (`active`).

    #### Return Properties

    | Field | Type | Description |
    | --- | --- | --- |
    | `key` | `string` | Immutable product key. |
    | `displayName` | `string` | Display name. |
    | `description` | `string \| null` | Optional description. |
    | `status` | `string` | `active`, `inactive`, or `archived`. |
    | `metadata` | `Record<string, unknown> \| null` | Metadata blob. |
    | `createdAt` | `string` | ISO timestamp. |
    | `updatedAt` | `string` | ISO timestamp. |

    #### Example
    ```typescript
    const product = await products.createProduct({
      key: 'pro-suite',
      displayName: 'Pro Suite',
      description: 'Advanced tier',
      metadata: { tier: 'pro' }
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `dto` | `CreateProductDto` | Yes | Contains the product key, display name, optional description, and metadata. |

    #### Input Properties

    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `Key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
    | `DisplayName` | `string` | Yes | 1–255 char label. |
    | `Description` | `string` | No | ≤1000 chars. |
    | `Metadata` | `Dictionary<string, object?>` | No | JSON-safe metadata blob. |

    #### Returns
    `Task<ProductDto>` – persisted product with timestamps and status (`active`).

    #### Return Properties

    | Property | Type | Description |
    | --- | --- | --- |
    | `Key` | `string` | Immutable product key. |
    | `DisplayName` | `string` | Display name. |
    | `Description` | `string?` | Optional description. |
    | `Status` | `string` | `active`, `inactive`, or `archived`. |
    | `Metadata` | `Dictionary<string, object?>?` | Metadata blob. |
    | `CreatedAt` | `string` | ISO timestamp. |
    | `UpdatedAt` | `string` | ISO timestamp. |

    #### Example
    ```csharp
    var product = await subscrio.Products.CreateProductAsync(new CreateProductDto(
        Key: "pro-suite",
        DisplayName: "Pro Suite",
        Description: "Advanced tier",
        Metadata: new Dictionary<string, object?> { ["tier"] = "pro" }
    ));
    ```

#### Expected Results
- Validates input via Zod schema (TS) / validation (C#).
- Ensures no product already exists with the provided key.
- Creates a domain entity with status `active` and persists it.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO fails schema checks (bad key format, display name length, etc.). |
| `ConflictError` | A product with the same key already exists. |

### updateProduct

#### Description
Applies partial updates to display name, description, or metadata of an existing product.

=== "TypeScript"
    #### Signature
    ```typescript
    updateProduct(key: string, dto: UpdateProductDto): Promise<ProductDto>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Immutable product key to update. |
    | `dto` | `UpdateProductDto` | Yes | Partial data containing new display name, description, or metadata. |

    #### Input Properties (UpdateProductDto)

    | Field | Type | Description |
    | --- | --- | --- |
    | `displayName` | `string` | Updated label (1–255 chars). |
    | `description` | `string` | Replacement description (≤1000 chars). |
    | `metadata` | `Record<string, unknown>` | Full metadata blob (overwrites stored value). |

    #### Returns
    `Promise<ProductDto>` – updated product snapshot.

    #### Example
    ```typescript
    const updated = await products.updateProduct('pro-suite', {
      displayName: 'Pro Suite (2025)',
      metadata: { tier: 'pro', version: '2025.1' }
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ProductDto> UpdateProductAsync(string key, UpdateProductDto dto)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Immutable product key to update. |
    | `dto` | `UpdateProductDto` | Yes | Partial data containing new display name, description, or metadata. |

    #### Input Properties

    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `DisplayName` | `string?` | No | Updated label (1–255 chars). |
    | `Description` | `string?` | No | Replacement description (≤1000 chars). |
    | `Metadata` | `Dictionary<string, object?>?` | No | Full metadata blob (overwrites stored value). |

    #### Returns
    `Task<ProductDto>` – updated product snapshot.

    #### Example
    ```csharp
    var updated = await subscrio.Products.UpdateProductAsync("pro-suite", new UpdateProductDto(
        DisplayName: "Pro Suite (2025)",
        Metadata: new Dictionary<string, object?> { ["tier"] = "pro", ["version"] = "2025.1" }
    ));
    ```

#### Expected Results
- Validates the DTO.
- Loads the product by key and mutates allowed fields.
- Updates `updatedAt` and persists the entity.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO fields fail schema (e.g., display name too short). |
| `NotFoundError` | Product key does not exist. |

### getProduct

#### Description
Fetches a product snapshot by key, returning `null` when it does not exist.

=== "TypeScript"
    #### Signature
    ```typescript
    getProduct(key: string): Promise<ProductDto | null>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key to retrieve. |

    #### Returns
    `Promise<ProductDto | null>` – `null` when the product is missing.

    #### Example
    ```typescript
    const product = await products.getProduct('starter');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ProductDto?> GetProductAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key to retrieve. |

    #### Returns
    `Task<ProductDto?>` – `null` when the product is missing.

    #### Example
    ```csharp
    var product = await subscrio.Products.GetProductAsync("starter");
    ```

#### Expected Results
- Loads product by key and maps domain entity to DTO.

#### Potential Errors
- None – method returns `null` for missing products.

### listProducts

#### Description
Lists products with status, search, and pagination controls.

=== "TypeScript"
    #### Signature
    ```typescript
    listProducts(filters?: ProductFilterDto): Promise<ProductDto[]>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filters` | `ProductFilterDto` | No | Optional filter object; defaults applied (limit 50, offset 0, sort asc). |

    #### Input Properties

    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `status` | `'active' \| 'archived'` | No | Filter by lifecycle state. |
    | `search` | `string` | No | Performs key/displayName search. |
    | `limit` | `number` | No | 1–100, default 50. |
    | `offset` | `number` | No | ≥0, default 0. |
    | `sortBy` | `'displayName' \| 'createdAt'` | No | Sort column. |
    | `sortOrder` | `'asc' \| 'desc'` | No | Sort direction; default `'asc'`. |

    #### Returns
    `Promise<ProductDto[]>` – all products matching the filters.

    #### Example
    ```typescript
    const activeProducts = await products.listProducts({
      status: 'active',
      search: 'suite',
      limit: 25
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<ProductDto>> ListProductsAsync(ProductFilterDto? filters = null)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filters` | `ProductFilterDto?` | No | Optional filter object; defaults applied. |

    #### Input Properties

    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `Status` | `string?` | No | `active` or `archived`. |
    | `Search` | `string?` | No | Performs key/displayName search. |
    | `Limit` | `int` | No | 1–100, default 50. |
    | `Offset` | `int` | No | ≥0, default 0. |
    | `SortBy` | `string?` | No | `displayName` or `createdAt`. |
    | `SortOrder` | `string` | No | `asc` or `desc`; default `asc`. |

    #### Returns
    `Task<List<ProductDto>>` – all products matching the filters.

    #### Example
    ```csharp
    var activeProducts = await subscrio.Products.ListProductsAsync(new ProductFilterDto(
        Status: "active",
        Search: "suite",
        Limit: 25
    ));
    ```

#### Expected Results
- Validates filters.
- Delegates to repository for query and maps each entity to DTO.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters contain invalid values (e.g., limit > 100). |

### deleteProduct

#### Description
Permanently deletes an archived product that has no associated plans.

=== "TypeScript"
    #### Signature
    ```typescript
    deleteProduct(key: string): Promise<void>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key targeted for deletion. |

    #### Returns
    `Promise<void>`

    #### Example
    ```typescript
    await products.archiveProduct('legacy-tier');
    await products.deleteProduct('legacy-tier');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task DeleteProductAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key targeted for deletion. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Products.ArchiveProductAsync("legacy-tier");
    await subscrio.Products.DeleteProductAsync("legacy-tier");
    ```

#### Expected Results
- Fetches product and verifies it exists.
- Calls `product.canDelete()` (must be archived).
- Ensures the product has zero plans before deletion.
- Removes the product record.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product does not exist. |
| `DomainError` | Product is not archived or still has plans. |

### archiveProduct

#### Description
Transitions a product to the archived state.

=== "TypeScript"
    #### Signature
    ```typescript
    archiveProduct(key: string): Promise<ProductDto>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key to archive. |

    #### Returns
    `Promise<ProductDto>` – `status: 'archived'`.

    #### Example
    ```typescript
    const archived = await products.archiveProduct('starter');
    console.log(archived.status); // 'archived'
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ProductDto> ArchiveProductAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key to archive. |

    #### Returns
    `Task<ProductDto>` – `Status: "archived"`.

    #### Example
    ```csharp
    var archived = await subscrio.Products.ArchiveProductAsync("starter");
    Console.WriteLine(archived.Status); // "archived"
    ```

#### Expected Results
- Loads product, calls the entity's `archive()` method, persists, and returns DTO.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product key not found. |

### unarchiveProduct

#### Description
Reverses `archiveProduct` by calling the entity's `unarchive()` method and restoring the product to `active`.

=== "TypeScript"
    #### Signature
    ```typescript
    unarchiveProduct(key: string): Promise<ProductDto>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key previously archived. |

    #### Returns
    `Promise<ProductDto>` – `status: 'active'`.

    #### Example
    ```typescript
    await products.unarchiveProduct('starter');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<ProductDto> UnarchiveProductAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Product key previously archived. |

    #### Returns
    `Task<ProductDto>` – `Status: "active"`.

    #### Example
    ```csharp
    await subscrio.Products.UnarchiveProductAsync("starter");
    ```

#### Expected Results
- Loads product, ensures it exists, calls `unarchive()`, and persists.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product key missing. |

### associateFeature

#### Description
Links an existing feature to a product so plans under the product can set values for it.

=== "TypeScript"
    #### Signature
    ```typescript
    associateFeature(productKey: string, featureKey: string): Promise<void>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Product to update. |
    | `featureKey` | `string` | Yes | Feature to associate. |

    #### Returns
    `Promise<void>`

    #### Example
    ```typescript
    await products.associateFeature('pro-suite', 'max-users');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task AssociateFeatureAsync(string productKey, string featureKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Product to update. |
    | `featureKey` | `string` | Yes | Feature to associate. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Products.AssociateFeatureAsync("pro-suite", "max-users");
    ```

#### Expected Results
- Validates both product and feature exist.
- Inserts association in repository layer.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product or feature is missing. |

### dissociateFeature

#### Description
Removes an existing feature association from a product.

=== "TypeScript"
    #### Signature
    ```typescript
    dissociateFeature(productKey: string, featureKey: string): Promise<void>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Product losing the feature. |
    | `featureKey` | `string` | Yes | Feature to remove. |

    #### Returns
    `Promise<void>`

    #### Example
    ```typescript
    await products.dissociateFeature('pro-suite', 'legacy-flag');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task DissociateFeatureAsync(string productKey, string featureKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Product losing the feature. |
    | `featureKey` | `string` | Yes | Feature to remove. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Products.DissociateFeatureAsync("pro-suite", "legacy-flag");
    ```

#### Expected Results
- Validates both entities exist.
- Deletes the join row if present.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product or feature missing. |

## Related Workflows
- Products must exist before you create plans (`PlanManagementService` references `productKey`).
- Deleting a product requires archival and removal of all plans to avoid `DomainError`.
- Feature associations determine which features plans under the product can set values for—coordinate with `FeatureManagementService`.
