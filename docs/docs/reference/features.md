# Feature Management Service Reference

## Service Overview
The Feature Management Service defines feature flags and typed configuration values that products expose to plans and subscriptions. Features are global and reusable across products once explicitly associated, and they always participate in the resolution hierarchy: subscription override → plan value → feature default.

- Feature keys are immutable and globally unique.
- `valueType` determines how defaults, plan values, and overrides are validated (`toggle`, `numeric`, `text`).
- Features cannot be deleted while referenced by products, plan feature values, or subscription overrides.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const features = subscrio.features;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var features = subscrio.Features;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `createFeature` | Validates and stores a new global feature | `Promise<FeatureDto>` |
    | `updateFeature` | Updates mutable fields on an existing feature | `Promise<FeatureDto>` |
    | `getFeature` | Retrieves a feature by key | `Promise<FeatureDto \| null>` |
    | `listFeatures` | Lists features with filters | `Promise<FeatureDto[]>` |
    | `archiveFeature` | Archives a feature | `Promise<void>` |
    | `unarchiveFeature` | Restores an archived feature | `Promise<void>` |
    | `deleteFeature` | Deletes an archived, unreferenced feature | `Promise<void>` |
    | `getFeaturesByProduct` | Lists features attached to a product | `Promise<FeatureDto[]>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `CreateFeatureAsync` | Validates and stores a new global feature | `Task<FeatureDto>` |
    | `UpdateFeatureAsync` | Updates mutable fields on an existing feature | `Task<FeatureDto>` |
    | `GetFeatureAsync` | Retrieves a feature by key | `Task<FeatureDto?>` |
    | `ListFeaturesAsync` | Lists features with filters | `Task<List<FeatureDto>>` |
    | `ArchiveFeatureAsync` | Archives a feature | `Task` |
    | `UnarchiveFeatureAsync` | Restores an archived feature | `Task` |
    | `DeleteFeatureAsync` | Deletes an archived, unreferenced feature | `Task` |
    | `GetFeaturesByProductAsync` | Lists features attached to a product | `Task<List<FeatureDto>>` |

## Method Reference

### createFeature

#### Description
Creates a new feature, validating keys, default values, and optional metadata before persisting it as `active`.

=== "TypeScript"
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
    | `valueType` | `'toggle' \| 'numeric' \| 'text'` | Yes | Controls validation rules. |
    | `defaultValue` | `string` | Yes | Must conform to `valueType`. |
    | `groupName` | `string` | No | Optional grouping label. |
    | `validator` | `Record<string, unknown>` | No | Custom metadata for downstream validation. |
    | `metadata` | `Record<string, unknown>` | No | JSON-safe metadata blob. |

    #### Returns
    `Promise<FeatureDto>` – persisted feature snapshot.

    #### Example
    ```typescript
    await features.createFeature({
      key: 'max-projects',
      displayName: 'Max Projects',
      valueType: 'numeric',
      defaultValue: '10'
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<FeatureDto> CreateFeatureAsync(CreateFeatureDto dto)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `dto` | `CreateFeatureDto` | Yes | Feature definition supplied by your app. |

    #### Input Properties

    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `Key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
    | `DisplayName` | `string` | Yes | 1–255 char label. |
    | `Description` | `string` | No | ≤1000 chars. |
    | `ValueType` | `string` | Yes | `toggle`, `numeric`, or `text`. |
    | `DefaultValue` | `string` | Yes | Must conform to `ValueType`. |
    | `GroupName` | `string` | No | Optional grouping label. |
    | `Validator` | `Dictionary<string, object?>` | No | Custom metadata for downstream validation. |
    | `Metadata` | `Dictionary<string, object?>` | No | JSON-safe metadata blob. |

    #### Returns
    `Task<FeatureDto>` – persisted feature snapshot.

    #### Example
    ```csharp
    await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
        Key: "max-projects",
        DisplayName: "Max Projects",
        ValueType: "numeric",
        DefaultValue: "10"
    ));
    ```

#### Expected Results
- Validates DTO fields and default value using `FeatureValueValidator`.
- Ensures key uniqueness.
- Persists feature with `active` status.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid or default mismatches `valueType`. |
| `ConflictError` | Feature key already exists. |

### updateFeature

#### Description
Applies partial updates (display name, description, default value, grouping, validator, metadata) to an existing feature.

=== "TypeScript"
    #### Signature
    ```typescript
    updateFeature(key: string, dto: UpdateFeatureDto): Promise<FeatureDto>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Feature key to mutate. |
    | `dto` | `UpdateFeatureDto` | Yes | Partial payload of fields to change. |

    #### Returns
    `Promise<FeatureDto>` – updated feature snapshot.

    #### Example
    ```typescript
    await features.updateFeature('max-projects', {
      defaultValue: '25',
      metadata: { tier: 'enterprise' }
    });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<FeatureDto> UpdateFeatureAsync(string key, UpdateFeatureDto dto)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Feature key to mutate. |
    | `dto` | `UpdateFeatureDto` | Yes | Partial payload of fields to change. |

    #### Returns
    `Task<FeatureDto>` – updated feature snapshot.

    #### Example
    ```csharp
    await subscrio.Features.UpdateFeatureAsync("max-projects", new UpdateFeatureDto(
        DefaultValue: "25",
        Metadata: new Dictionary<string, object?> { ["tier"] = "enterprise" }
    ));
    ```

#### Expected Results
- Validates provided fields and default/valueType compatibility.
- Loads feature, applies updates, recalculates timestamps, and saves.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | DTO invalid or default fails validation. |
| `NotFoundError` | Feature key not found. |

### getFeature

#### Description
Retrieves a feature by key, returning `null` when it does not exist.

=== "TypeScript"
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

    #### Example
    ```typescript
    const feature = await features.getFeature('gantt-charts');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<FeatureDto?> GetFeatureAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Feature identifier. |

    #### Returns
    `Task<FeatureDto?>`

    #### Example
    ```csharp
    var feature = await subscrio.Features.GetFeatureAsync("gantt-charts");
    ```

#### Expected Results
- Queries repository and maps record to DTO or returns `null`.

#### Potential Errors
- None – missing features return `null`.

### listFeatures

#### Description
Lists features with optional filtering, search, and pagination controls.

=== "TypeScript"
    #### Signature
    ```typescript
    listFeatures(filters?: FeatureFilterDto): Promise<FeatureDto[]>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filters` | `FeatureFilterDto` | No | Status, type, group, search, and pagination settings. |

    #### Returns
    `Promise<FeatureDto[]>`

    #### Example
    ```typescript
    const toggles = await features.listFeatures({ valueType: 'toggle', limit: 20 });
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<FeatureDto>> ListFeaturesAsync(FeatureFilterDto? filters = null)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `filters` | `FeatureFilterDto` | No | Status, type, group, search, and pagination settings. |

    #### Returns
    `Task<List<FeatureDto>>`

    #### Example
    ```csharp
    var toggles = await subscrio.Features.ListFeaturesAsync(new FeatureFilterDto(
        ValueType: "toggle",
        Limit: 20
    ));
    ```

#### Expected Results
- Validates filters.
- Executes query and maps records to DTOs.

#### Potential Errors

| Error | When |
| --- | --- |
| `ValidationError` | Filters contain invalid values. |

### archiveFeature

#### Description
Marks a feature as archived so it cannot be used for new plan values or overrides.

=== "TypeScript"
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

    #### Example
    ```typescript
    await features.archiveFeature('legacy-beta');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task ArchiveFeatureAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Feature key to archive. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Features.ArchiveFeatureAsync("legacy-beta");
    ```

#### Expected Results
- Loads feature, invokes entity `archive()`, persists status change.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Feature key missing. |

### unarchiveFeature

#### Description
Restores an archived feature back to `active`.

=== "TypeScript"
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

    #### Example
    ```typescript
    await features.unarchiveFeature('legacy-beta');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task UnarchiveFeatureAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Feature key previously archived. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Features.UnarchiveFeatureAsync("legacy-beta");
    ```

#### Expected Results
- Loads feature, calls `unarchive()`, persists the change.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Feature key missing. |

### deleteFeature

#### Description
Deletes a feature permanently after verifying it is archived and unused.

=== "TypeScript"
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

    #### Example
    ```typescript
    await features.deleteFeature('sunset-flag');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task DeleteFeatureAsync(string key)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | Feature targeted for deletion. |

    #### Returns
    `Task`

    #### Example
    ```csharp
    await subscrio.Features.DeleteFeatureAsync("sunset-flag");
    ```

#### Expected Results
- Loads feature and checks `feature.canDelete()` (must be archived).
- Ensures no product associations, plan feature values, or subscription overrides remain.
- Deletes the feature record.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Feature missing. |
| `DomainError` | Feature still active or referenced. |

### getFeaturesByProduct

#### Description
Returns all features currently associated with a product.

=== "TypeScript"
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

    #### Example
    ```typescript
    const productFeatures = await features.getFeaturesByProduct('pro-suite');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<FeatureDto>> GetFeaturesByProductAsync(string productKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `productKey` | `string` | Yes | Product key to inspect. |

    #### Returns
    `Task<List<FeatureDto>>`

    #### Example
    ```csharp
    var productFeatures = await subscrio.Features.GetFeaturesByProductAsync("pro-suite");
    ```

#### Expected Results
- Validates the product exists.
- Queries associations and maps features to DTOs.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Product key missing. |

## DTO Reference

### CreateFeatureDto / UpdateFeatureDto

=== "TypeScript"
    | Field | Type | Required | Constraints |
    | --- | --- | --- | --- |
    | `key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
    | `displayName` | `string` | Yes | 1–255 chars. |
    | `description` | `string` | No | ≤1000 chars. |
    | `valueType` | `'toggle' \| 'numeric' \| 'text'` | Yes | Determines validation rules. |
    | `defaultValue` | `string` | Yes | Must match `valueType`. |
    | `groupName` | `string` | No | ≤255 chars. |
    | `validator` | `Record<string, unknown>` | No | Custom validation metadata. |
    | `metadata` | `Record<string, unknown>` | No | JSON-safe metadata. |

    *UpdateFeatureDto*: Same fields, all optional; `valueType` and `defaultValue` must remain compatible when both provided.

=== ".NET"
    | Property | Type | Required | Constraints |
    | --- | --- | --- | --- |
    | `Key` | `string` | Yes | 1–255 chars, lowercase alphanumeric plus `-`. |
    | `DisplayName` | `string` | Yes | 1–255 chars. |
    | `Description` | `string` | No | ≤1000 chars. |
    | `ValueType` | `string` | Yes | `toggle`, `numeric`, or `text`. |
    | `DefaultValue` | `string` | Yes | Must match `ValueType`. |
    | `GroupName` | `string` | No | ≤255 chars. |
    | `Validator` | `Dictionary<string, object?>` | No | Custom validation metadata. |
    | `Metadata` | `Dictionary<string, object?>` | No | JSON-safe metadata. |

    *UpdateFeatureDto*: Same fields, all optional.

### FeatureDto

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `key` | `string` | Immutable key. |
    | `displayName` | `string` | Human-readable name. |
    | `description` | `string \| null` | Optional description. |
    | `valueType` | `string` | `toggle`, `numeric`, or `text`. |
    | `defaultValue` | `string` | Stored default value. |
    | `groupName` | `string \| null` | Group label. |
    | `status` | `string` | `active` or `archived`. |
    | `validator` | `Record<string, unknown> \| null` | Validator metadata. |
    | `metadata` | `Record<string, unknown> \| null` | Arbitrary metadata. |
    | `createdAt` | `string` | ISO timestamp. |
    | `updatedAt` | `string` | ISO timestamp. |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `Key` | `string` | Immutable key. |
    | `DisplayName` | `string` | Human-readable name. |
    | `Description` | `string?` | Optional description. |
    | `ValueType` | `string` | `toggle`, `numeric`, or `text`. |
    | `DefaultValue` | `string` | Stored default value. |
    | `GroupName` | `string?` | Group label. |
    | `Status` | `string` | `active` or `archived`. |
    | `Validator` | `Dictionary<string, object?>?` | Validator metadata. |
    | `Metadata` | `Dictionary<string, object?>?` | Arbitrary metadata. |
    | `CreatedAt` | `string` | ISO timestamp. |
    | `UpdatedAt` | `string` | ISO timestamp. |

### FeatureFilterDto

=== "TypeScript"
    | Field | Type | Description |
    | --- | --- | --- |
    | `status` | `'active' \| 'archived'` | Lifecycle filter. |
    | `valueType` | `'toggle' \| 'numeric' \| 'text'` | Type filter. |
    | `groupName` | `string` | Group filter. |
    | `search` | `string` | Text search term. |
    | `limit` | `number` | 1–100 (default 50). |
    | `offset` | `number` | ≥0 (default 0). |
    | `sortBy` | `'displayName' \| 'createdAt'` | Sort column. |
    | `sortOrder` | `'asc' \| 'desc'` | Sort direction. |

=== ".NET"
    | Property | Type | Description |
    | --- | --- | --- |
    | `Status` | `string` | `active` or `archived`. |
    | `ValueType` | `string` | `toggle`, `numeric`, or `text`. |
    | `GroupName` | `string` | Group filter. |
    | `Search` | `string` | Text search term. |
    | `Limit` | `int` | 1–100 (default 50). |
    | `Offset` | `int` | ≥0 (default 0). |
    | `SortBy` | `string` | `displayName` or `createdAt`. |
    | `SortOrder` | `string` | `asc` or `desc`; default `asc`. |

## Related Workflows
- Products must associate features before plans can set values (`ProductManagementService.associateFeature`).
- Plan-level values (`PlanManagementService.setFeatureValue`) override defaults but are superseded by subscription overrides.
- Subscription overrides (`SubscriptionManagementService.addFeatureOverride`) take precedence in the feature resolution hierarchy enforced by `FeatureCheckerService`.
