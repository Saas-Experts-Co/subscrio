
# Feature Checker Service Reference

## Service Overview
The Feature Checker service evaluates feature values at runtime. Every method enforces the hierarchy:

`subscription override → plan value → feature default`

The service answers questions at both subscription and customer levels, exposes plan-access helpers, and can summarize usage patterns. Results draw from `FeatureValueResolver`, so the hierarchy is consistent everywhere.

## Accessing the Service
```typescript
import { Subscrio } from '@subscrio/core';

const subscrio = new Subscrio({ database: { connectionString: process.env.DATABASE_URL! } });
const featureChecker = subscrio.featureChecker;
```

## Method Catalog

| Method | Description | Returns |
| --- | --- | --- |
| `getValueForSubscription` | Resolve a feature for one subscription | <code>Promise&lt;T &#124; null&gt;</code> |
| `isEnabledForSubscription` | Boolean helper for toggle features | `Promise<boolean>` |
| `getAllFeaturesForSubscription` | Resolve every feature for a subscription’s product | `Promise<Map<string, string>>` |
| `getValueForCustomer` | Resolve a feature across a customer’s active/trial subscriptions for a product | <code>Promise&lt;T &#124; null&gt;</code> |
| `isEnabledForCustomer` | Boolean helper for customer/product queries | `Promise<boolean>` |
| `getAllFeaturesForCustomer` | Aggregate all feature values for a customer/product pair | `Promise<Map<string, string>>` |
| `hasPlanAccess` | Check if a customer currently has an active/trial subscription to a plan | `Promise<boolean>` |
| `getActivePlans` | List active/trial plan keys for a customer | `Promise<string[]>` |
| `getFeatureUsageSummary` | Summarize enabled/disabled/numeric/text states | `Promise<FeatureUsageSummary>` |

## Method Reference

### getValueForSubscription

#### Description
Resolves a feature value for a single subscription using override → plan value → feature default precedence.

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

#### Expected Results
- Loads subscription, plan, and feature.
- Applies resolver hierarchy; if any entity is missing returns fallback rather than throwing.

#### Potential Errors
- None.

#### Example
```typescript
const seats = await featureChecker.getValueForSubscription<number>(
  'sub_1001',
  'seat-limit',
  0
);
```

### isEnabledForSubscription

#### Description
Convenience helper for toggle features at the subscription level.

#### Signature
```typescript
isEnabledForSubscription(subscriptionKey: string, featureKey: string): Promise<boolean>
```

#### Returns
`Promise<boolean>` – `true` when the resolved value equals `'true'` (case-insensitive).

#### Example
```typescript
const hasBranding = await featureChecker.isEnabledForSubscription(
  'sub_enterprise',
  'custom-branding'
);
```

### getAllFeaturesForSubscription

#### Description
Resolves every feature for the subscription’s product, returning a map of `featureKey → value`.

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

#### Expected Results
- Loads subscription, plan, and product, then queries all product features.
- Resolves each feature via the hierarchy and populates the map.
- Returns empty map when the plan cannot be resolved.

#### Potential Errors

| Error | When |
| --- | --- |
| `NotFoundError` | Subscription missing or product cannot be resolved. |

#### Example
```typescript
const resolved = await featureChecker.getAllFeaturesForSubscription('sub_1001');
console.log(resolved.get('max-projects'));
```

### getValueForCustomer

#### Description
Resolves a feature for a customer/product pair by scanning active/trial subscriptions (up to `MAX_SUBSCRIPTIONS_PER_CUSTOMER`).

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

#### Expected Results
- Loads customer, product, and feature; returns fallback when any missing.
- Fetches subscriptions for the customer, filters to active/trial entries for the product.
- Applies resolver across subscriptions, honoring override precedence if multiple subscriptions exist.

#### Potential Errors
- None.

#### Example
```typescript
const maxProjects = await featureChecker.getValueForCustomer<number>(
  'acme-corp',
  'projecthub',
  'max-projects',
  0
);
```

### isEnabledForCustomer

#### Description
Boolean helper that wraps `getValueForCustomer`.

#### Signature
```typescript
isEnabledForCustomer(
  customerKey: string,
  productKey: string,
  featureKey: string
): Promise<boolean>
```

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

### getAllFeaturesForCustomer

#### Description
Aggregates every feature value for a customer/product pair by considering all active/trial subscriptions.

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

#### Expected Results
- Loads customer/product; returns empty map when either missing.
- Resolves all product features using the resolver across relevant subscriptions.

#### Potential Errors
- None.

#### Example
```typescript
const customerFeatures = await featureChecker.getAllFeaturesForCustomer(
  'acme-corp',
  'projecthub'
);
```

### hasPlanAccess

#### Description
Checks whether a customer currently holds an active or trial subscription for a given plan.

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

#### Expected Results
- Validates all entities exist.
- Loads subscriptions for the customer and searches for an active/trial entry referencing the plan.

#### Potential Errors
- None.

#### Example
```typescript
const hasPro = await featureChecker.hasPlanAccess('acme-corp', 'projecthub', 'professional');
```

### getActivePlans

#### Description
Lists plan keys for every active/trial subscription held by a customer (across all products).

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

#### Expected Results
- Loads customer and their subscriptions.
- Batch-fetches plans to avoid N+1 queries and returns plan keys.

#### Example
```typescript
const plans = await featureChecker.getActivePlans('acme-corp');
```

### getFeatureUsageSummary

#### Description
Produces a usage rollup showing how features resolve (enabled/disabled/numeric/text) for a customer/product pair and includes the customer’s subscription count.

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
Object containing counts and maps classified by feature type.

#### Expected Results
- Counts the customer’s subscriptions (regardless of product filter).
- Resolves all product features (using defaults when customer/product missing) and classifies values by `FeatureDto.valueType`.

#### Potential Errors
- None.

#### Example
```typescript
const summary = await featureChecker.getFeatureUsageSummary('acme-corp', 'projecthub');
console.log(summary.enabledFeatures);
```

## Related Workflows
- Products must associate features and plans must set feature values for meaningful results; otherwise values fall back to feature defaults.
- Subscription-level overrides come from `SubscriptionManagementService.addFeatureOverride`.
- Cache high-traffic queries such as `getAllFeaturesForCustomer` to avoid recalculating the same maps repeatedly.
