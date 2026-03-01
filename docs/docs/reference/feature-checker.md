
# Feature Checker Service Reference

## Service Overview
The Feature Checker service evaluates feature values at runtime. Every method enforces the hierarchy:

`subscription override → plan value → feature default`

The service answers questions at both subscription and customer levels, exposes plan-access helpers, and can summarize usage patterns. Results draw from `FeatureValueResolver`, so the hierarchy is consistent everywhere.

## Accessing the Service

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
    const featureChecker = subscrio.featureChecker;
    ```

=== ".NET"
    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(config);
    var featureChecker = subscrio.FeatureChecker;
    ```

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `getValueForSubscription` | Resolve a feature for one subscription | `Promise<T \| null>` |
    | `isEnabledForSubscription` | Boolean helper for toggle features | `Promise<boolean>` |
    | `getAllFeaturesForSubscription` | Resolve every feature for a subscription's product | `Promise<Map<string, string>>` |
    | `getValueForCustomer` | Resolve a feature across a customer's active/trial subscriptions for a product | `Promise<T \| null>` |
    | `isEnabledForCustomer` | Boolean helper for customer/product queries | `Promise<boolean>` |
    | `getAllFeaturesForCustomer` | Aggregate all feature values for a customer/product pair | `Promise<Map<string, string>>` |
    | `hasPlanAccess` | Check if a customer currently has an active/trial subscription to a plan | `Promise<boolean>` |
    | `getActivePlans` | List active/trial plan keys for a customer | `Promise<string[]>` |
    | `getFeatureUsageSummary` | Summarize enabled/disabled/numeric/text states | `Promise<FeatureUsageSummaryDto>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `GetValueForSubscriptionAsync` | Resolve a feature for one subscription | `Task<T?>` |
    | `IsEnabledForSubscriptionAsync` | Boolean helper for toggle features | `Task<bool>` |
    | `GetAllFeaturesForSubscriptionAsync` | Resolve every feature for a subscription's product | `Task<Dictionary<string, string>>` |
    | `GetValueForCustomerAsync` | Resolve a feature across a customer's active/trial subscriptions for a product | `Task<T?>` |
    | `IsEnabledForCustomerAsync` | Boolean helper for customer/product queries | `Task<bool>` |
    | `GetAllFeaturesForCustomerAsync` | Aggregate all feature values for a customer/product pair | `Task<Dictionary<string, string>>` |
    | `HasPlanAccessAsync` | Check if a customer currently has an active/trial subscription to a plan | `Task<bool>` |
    | `GetActivePlansAsync` | List active/trial plan keys for a customer | `Task<List<string>>` |
    | `GetFeatureUsageSummaryAsync` | Summarize enabled/disabled/numeric/text states | `Task<FeatureUsageSummaryDto>` |

## Method Reference

### getValueForSubscription

#### Description
Resolves a feature value for a single subscription using override → plan value → feature default precedence.

=== "TypeScript"
    #### Signature
    ```typescript
    getValueForSubscription<T = string>(
      subscriptionKey: string,
      featureKey: string,
      defaultValue?: T
    ): Promise<T | null>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `subscriptionKey` | `string` | Yes | Subscription identifier. |
    | `featureKey` | `string` | Yes | Feature key to resolve. |
    | `defaultValue` | `T` | No | Optional fallback when any entity is missing. |

    #### Returns
    `Promise<T | null>` – resolved value (cast to `T` when provided) or `defaultValue ?? null`.

    #### Example
    ```typescript
    const seats = await featureChecker.getValueForSubscription<number>(
      'sub_1001',
      'seat-limit',
      0
    );
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<T?> GetValueForSubscriptionAsync<T>(
        string subscriptionKey,
        string featureKey,
        T? defaultValue = default
    )
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `subscriptionKey` | `string` | Yes | Subscription identifier. |
    | `featureKey` | `string` | Yes | Feature key to resolve. |
    | `defaultValue` | `T?` | No | Optional fallback when any entity is missing. |

    #### Returns
    `Task<T?>` – resolved value or `defaultValue ?? null`.

    #### Example
    ```csharp
    var seats = await subscrio.FeatureChecker.GetValueForSubscriptionAsync<string>(
        "sub_1001",
        "seat-limit",
        "0"
    );
    ```

#### Expected Results
- Loads subscription, plan, and feature.
- Applies resolver hierarchy; if any entity is missing returns fallback rather than throwing.

#### Potential Errors
- None.

### isEnabledForSubscription

#### Description
Convenience helper for toggle features at the subscription level.

=== "TypeScript"
    #### Signature
    ```typescript
    isEnabledForSubscription(subscriptionKey: string, featureKey: string): Promise<boolean>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `subscriptionKey` | `string` | Yes | Subscription identifier. |
    | `featureKey` | `string` | Yes | Feature key to resolve. |

    #### Returns
    `Promise<boolean>` – `true` when the resolved value equals `'true'` (case-insensitive).

    #### Example
    ```typescript
    const hasBranding = await featureChecker.isEnabledForSubscription(
      'sub_enterprise',
      'custom-branding'
    );
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<bool> IsEnabledForSubscriptionAsync(string subscriptionKey, string featureKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `subscriptionKey` | `string` | Yes | Subscription identifier. |
    | `featureKey` | `string` | Yes | Feature key to resolve. |

    #### Returns
    `Task<bool>` – `true` when the resolved value equals `'true'` (case-insensitive).

    #### Example
    ```csharp
    var hasBranding = await subscrio.FeatureChecker.IsEnabledForSubscriptionAsync(
        "sub_enterprise",
        "custom-branding"
    );
    ```

#### Expected Results
- Wraps `getValueForSubscription` and checks for `'true'`.

#### Potential Errors
- None.

### getAllFeaturesForSubscription

#### Description
Resolves every feature for the subscription's product, returning a map of `featureKey → value`.

=== "TypeScript"
    #### Signature
    ```typescript
    getAllFeaturesForSubscription(subscriptionKey: string): Promise<Map<string, string>>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `subscriptionKey` | `string` | Yes | Subscription identifier. |

    #### Returns
    `Promise<Map<string, string>>`

    #### Example
    ```typescript
    const resolved = await featureChecker.getAllFeaturesForSubscription('sub_1001');
    console.log(resolved.get('max-projects'));
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<Dictionary<string, string>> GetAllFeaturesForSubscriptionAsync(string subscriptionKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `subscriptionKey` | `string` | Yes | Subscription identifier. |

    #### Returns
    `Task<Dictionary<string, string>>`

    #### Example
    ```csharp
    var resolved = await subscrio.FeatureChecker.GetAllFeaturesForSubscriptionAsync("sub_1001");
    Console.WriteLine(resolved.GetValueOrDefault("max-projects"));
    ```

#### Expected Results
- Loads subscription, plan, and product, then queries all product features.
- Resolves each feature via the hierarchy and populates the map.
- Returns empty map when the plan cannot be resolved.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Subscription missing or product cannot be resolved. |

### getValueForCustomer

#### Description
Resolves a feature for a customer/product pair by scanning active/trial subscriptions (up to `MAX_SUBSCRIPTIONS_PER_CUSTOMER`).

=== "TypeScript"
    #### Signature
    ```typescript
    getValueForCustomer<T = string>(
      customerKey: string,
      productKey: string,
      featureKey: string,
      defaultValue?: T
    ): Promise<T | null>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product containing the feature. |
    | `featureKey` | `string` | Yes | Feature to resolve. |
    | `defaultValue` | `T` | No | Optional fallback. |

    #### Returns
    `Promise<T | null>`

    #### Example
    ```typescript
    const maxProjects = await featureChecker.getValueForCustomer<number>(
      'acme-corp',
      'projecthub',
      'max-projects',
      0
    );
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<T?> GetValueForCustomerAsync<T>(
        string customerKey,
        string productKey,
        string featureKey,
        T? defaultValue = default
    )
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product containing the feature. |
    | `featureKey` | `string` | Yes | Feature to resolve. |
    | `defaultValue` | `T?` | No | Optional fallback. |

    #### Returns
    `Task<T?>`

    #### Example
    ```csharp
    var maxProjects = await subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
        "acme-corp",
        "projecthub",
        "max-projects",
        "0"
    );
    ```

#### Expected Results
- Loads customer, product, and feature; returns fallback when any missing.
- Fetches subscriptions for the customer, filters to active/trial entries for the product.
- Applies resolver across subscriptions, honoring override precedence if multiple subscriptions exist.

#### Potential Errors
- None.

### isEnabledForCustomer

#### Description
Boolean helper that wraps `getValueForCustomer`.

=== "TypeScript"
    #### Signature
    ```typescript
    isEnabledForCustomer(
      customerKey: string,
      productKey: string,
      featureKey: string
    ): Promise<boolean>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product containing the feature. |
    | `featureKey` | `string` | Yes | Feature to resolve. |

    #### Returns
    `Promise<boolean>` – `true` when the resolved value equals `'true'`.

    #### Example
    ```typescript
    const hasApiAccess = await featureChecker.isEnabledForCustomer(
      'acme-corp',
      'projecthub',
      'api-access'
    );
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<bool> IsEnabledForCustomerAsync(
        string customerKey,
        string productKey,
        string featureKey
    )
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product containing the feature. |
    | `featureKey` | `string` | Yes | Feature to resolve. |

    #### Returns
    `Task<bool>` – `true` when the resolved value equals `'true'`.

    #### Example
    ```csharp
    var hasApiAccess = await subscrio.FeatureChecker.IsEnabledForCustomerAsync(
        "acme-corp",
        "projecthub",
        "api-access"
    );
    ```

#### Expected Results
- Wraps `getValueForCustomer` and checks for `'true'`.

#### Potential Errors
- None.

### getAllFeaturesForCustomer

#### Description
Aggregates every feature value for a customer/product pair by considering all active/trial subscriptions.

=== "TypeScript"
    #### Signature
    ```typescript
    getAllFeaturesForCustomer(
      customerKey: string,
      productKey: string
    ): Promise<Map<string, string>>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product key. |

    #### Returns
    `Promise<Map<string, string>>` – defaults when no matching subscriptions exist.

    #### Example
    ```typescript
    const customerFeatures = await featureChecker.getAllFeaturesForCustomer(
      'acme-corp',
      'projecthub'
    );
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<Dictionary<string, string>> GetAllFeaturesForCustomerAsync(
        string customerKey,
        string productKey
    )
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product key. |

    #### Returns
    `Task<Dictionary<string, string>>` – defaults when no matching subscriptions exist.

    #### Example
    ```csharp
    var customerFeatures = await subscrio.FeatureChecker.GetAllFeaturesForCustomerAsync(
        "acme-corp",
        "projecthub"
    );
    ```

#### Expected Results
- Loads customer/product; returns empty map when either missing.
- Resolves all product features using the resolver across relevant subscriptions.

#### Potential Errors
- None.

### hasPlanAccess

#### Description
Checks whether a customer currently holds an active or trial subscription for a given plan.

=== "TypeScript"
    #### Signature
    ```typescript
    hasPlanAccess(
      customerKey: string,
      productKey: string,
      planKey: string
    ): Promise<boolean>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product identifier (used to validate plan ownership). |
    | `planKey` | `string` | Yes | Plan to check. |

    #### Returns
    `Promise<boolean>` – `false` when customer/product/plan missing or no qualifying subscription is found.

    #### Example
    ```typescript
    const hasPro = await featureChecker.hasPlanAccess('acme-corp', 'projecthub', 'professional');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<bool> HasPlanAccessAsync(
        string customerKey,
        string productKey,
        string planKey
    )
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product identifier (used to validate plan ownership). |
    | `planKey` | `string` | Yes | Plan to check. |

    #### Returns
    `Task<bool>` – `false` when customer/product/plan missing or no qualifying subscription is found.

    #### Example
    ```csharp
    var hasPro = await subscrio.FeatureChecker.HasPlanAccessAsync("acme-corp", "projecthub", "professional");
    ```

#### Expected Results
- Validates all entities exist.
- Loads subscriptions for the customer and searches for an active/trial entry referencing the plan.

#### Potential Errors
- None.

### getActivePlans

#### Description
Lists plan keys for every active/trial subscription held by a customer (across all products).

=== "TypeScript"
    #### Signature
    ```typescript
    getActivePlans(customerKey: string): Promise<string[]>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |

    #### Returns
    `Promise<string[]>` – empty array when customer missing or no active/trial subscriptions exist.

    #### Example
    ```typescript
    const plans = await featureChecker.getActivePlans('acme-corp');
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<List<string>> GetActivePlansAsync(string customerKey)
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |

    #### Returns
    `Task<List<string>>` – empty list when customer missing or no active/trial subscriptions exist.

    #### Example
    ```csharp
    var plans = await subscrio.FeatureChecker.GetActivePlansAsync("acme-corp");
    ```

#### Expected Results
- Loads customer and their subscriptions.
- Batch-fetches plans to avoid N+1 queries and returns plan keys.

#### Potential Errors
- None.

### getFeatureUsageSummary

#### Description
Produces a usage rollup showing how features resolve (enabled/disabled/numeric/text) for a customer/product pair and includes the customer's subscription count.

=== "TypeScript"
    #### Signature
    ```typescript
    getFeatureUsageSummary(
      customerKey: string,
      productKey: string
    ): Promise<{
      activeSubscriptions: number;
      enabledFeatures: string[];
      disabledFeatures: string[];
      numericFeatures: Map<string, number>;
      textFeatures: Map<string, string>;
    }>
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product key. |

    #### Returns
    Object containing `activeSubscriptions`, `enabledFeatures`, `disabledFeatures`, `numericFeatures` (`Map<string, number>`), `textFeatures` (`Map<string, string>`).

    #### Example
    ```typescript
    const summary = await featureChecker.getFeatureUsageSummary('acme-corp', 'projecthub');
    console.log(summary.enabledFeatures);
    ```

=== ".NET"
    #### Signature
    ```csharp
    Task<FeatureUsageSummaryDto> GetFeatureUsageSummaryAsync(
        string customerKey,
        string productKey
    )
    ```

    #### Inputs

    | Name | Type | Required | Description |
    | --- | --- | --- | --- |
    | `customerKey` | `string` | Yes | Customer identifier. |
    | `productKey` | `string` | Yes | Product key. |

    #### Returns
    `Task<FeatureUsageSummaryDto>` – contains `ActiveSubscriptions`, `EnabledFeatures`, `DisabledFeatures`, `NumericFeatures` (`Dictionary<string, int>`), `TextFeatures` (`Dictionary<string, string>`).

    #### Example
    ```csharp
    var summary = await subscrio.FeatureChecker.GetFeatureUsageSummaryAsync("acme-corp", "projecthub");
    Console.WriteLine(string.Join(", ", summary.EnabledFeatures));
    ```

#### Expected Results
- Counts the customer's subscriptions (regardless of product filter).
- Resolves all product features (using defaults when customer/product missing) and classifies values by `FeatureDto.valueType`.

#### Potential Errors
- None.

## Related Workflows
- Products must associate features and plans must set feature values for meaningful results; otherwise values fall back to feature defaults.
- Subscription-level overrides come from `SubscriptionManagementService.addFeatureOverride`.
- Cache high-traffic queries such as `getAllFeaturesForCustomer` to avoid recalculating the same maps repeatedly.
