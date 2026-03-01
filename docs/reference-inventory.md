# Subscrio API Reference Inventory

Source of truth for documentation verification. Extracted from local `core.typescript/` and `core.dotnet/`—never from npm or NuGet.

**Source paths:**
- `core.typescript/src/application/services/*.ts`
- `core.typescript/src/application/dtos/*.ts`
- `core.typescript/src/Subscrio.ts`
- `core.dotnet/src/Application/Services/*.cs`
- `core.dotnet/src/Application/DTOs/*.cs`
- `core.dotnet/src/Subscrio.cs`

---

## Reference Pages & Scope

| Page | Service / Class | Method Count | Doc Status |
|------|-----------------|--------------|------------|
| `subscriptions.md` | SubscriptionManagementService | 13 | ✓ Complete |
| `products.md` | ProductManagementService | 9 | ✓ Complete |
| `features.md` | FeatureManagementService | 8 | ✓ Complete |
| `plans.md` | PlanManagementService | 12 | ✓ Complete |
| `billing-cycles.md` | BillingCycleManagementService | 11 | ✓ Complete |
| `customers.md` | CustomerManagementService | 7 | ✓ Complete |
| `feature-checker.md` | FeatureCheckerService | 9 | ✓ Complete |
| `config-sync.md` | ConfigSyncService | 2 | Pending |
| `stripe-integration.md` | StripeIntegrationService | 3 | Pending |
| `core-overview.md` | Subscrio (root) | 5 | ✓ Complete |

---

## SubscriptionManagementService (subscriptions.md)

**Access:** `subscrio.subscriptions` (TS) / `subscrio.Subscriptions` (.NET)

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `createSubscription(dto)` | `CreateSubscriptionAsync(dto)` | CreateSubscriptionDto | SubscriptionDto |
| 2 | `updateSubscription(subscriptionKey, dto)` | `UpdateSubscriptionAsync(subscriptionKey, dto)` | string, UpdateSubscriptionDto | SubscriptionDto |
| 3 | `getSubscription(subscriptionKey)` | `GetSubscriptionAsync(subscriptionKey)` | string | SubscriptionDto \| null |
| 4 | `listSubscriptions(filters?)` | `ListSubscriptionsAsync(filters?)` | SubscriptionFilterDto? | SubscriptionDto[] |
| 5 | `findSubscriptions(filters)` | `FindSubscriptionsAsync(filters)` | DetailedSubscriptionFilterDto | SubscriptionDto[] |
| 6 | `getSubscriptionsByCustomer(customerKey)` | `GetSubscriptionsByCustomerAsync(customerKey)` | string | SubscriptionDto[] |
| 7 | `archiveSubscription(subscriptionKey)` | `ArchiveSubscriptionAsync(subscriptionKey)` | string | void |
| 8 | `unarchiveSubscription(subscriptionKey)` | `UnarchiveSubscriptionAsync(subscriptionKey)` | string | void |
| 9 | `deleteSubscription(subscriptionKey)` | `DeleteSubscriptionAsync(subscriptionKey)` | string | void |
| 10 | `addFeatureOverride(subscriptionKey, featureKey, value, overrideType?)` | `AddFeatureOverrideAsync(...)` | string, string, string, OverrideType? | void |
| 11 | `removeFeatureOverride(subscriptionKey, featureKey)` | `RemoveFeatureOverrideAsync(...)` | string, string | void |
| 12 | `clearTemporaryOverrides(subscriptionKey)` | `ClearTemporaryOverridesAsync(subscriptionKey)` | string | void |
| 13 | `transitionExpiredSubscriptions()` | `TransitionExpiredSubscriptionsAsync()` | — | TransitionExpiredSubscriptionsReport |

---

## ProductManagementService (products.md)

**Access:** `subscrio.products` / `subscrio.Products`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `createProduct(dto)` | `CreateProductAsync(dto)` | CreateProductDto | ProductDto |
| 2 | `updateProduct(key, dto)` | `UpdateProductAsync(key, dto)` | string, UpdateProductDto | ProductDto |
| 3 | `getProduct(key)` | `GetProductAsync(key)` | string | ProductDto \| null |
| 4 | `listProducts(filters?)` | `ListProductsAsync(filters?)` | ProductFilterDto? | ProductDto[] |
| 5 | `deleteProduct(key)` | `DeleteProductAsync(key)` | string | void |
| 6 | `archiveProduct(key)` | `ArchiveProductAsync(key)` | string | ProductDto |
| 7 | `unarchiveProduct(key)` | `UnarchiveProductAsync(key)` | string | ProductDto |
| 8 | `associateFeature(productKey, featureKey)` | `AssociateFeatureAsync(...)` | string, string | void |
| 9 | `dissociateFeature(productKey, featureKey)` | `DissociateFeatureAsync(...)` | string, string | void |

---

## FeatureManagementService (features.md)

**Access:** `subscrio.features` / `subscrio.Features`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `createFeature(dto)` | `CreateFeatureAsync(dto)` | CreateFeatureDto | FeatureDto |
| 2 | `updateFeature(key, dto)` | `UpdateFeatureAsync(key, dto)` | string, UpdateFeatureDto | FeatureDto |
| 3 | `getFeature(key)` | `GetFeatureAsync(key)` | string | FeatureDto \| null |
| 4 | `listFeatures(filters?)` | `ListFeaturesAsync(filters?)` | FeatureFilterDto? | FeatureDto[] |
| 5 | `archiveFeature(key)` | `ArchiveFeatureAsync(key)` | string | void |
| 6 | `unarchiveFeature(key)` | `UnarchiveFeatureAsync(key)` | string | void |
| 7 | `deleteFeature(key)` | `DeleteFeatureAsync(key)` | string | void |
| 8 | `getFeaturesByProduct(productKey)` | `GetFeaturesByProductAsync(productKey)` | string | FeatureDto[] |

---

## PlanManagementService (plans.md)

**Access:** `subscrio.plans` / `subscrio.Plans`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `createPlan(dto)` | `CreatePlanAsync(dto)` | CreatePlanDto | PlanDto |
| 2 | `updatePlan(planKey, dto)` | `UpdatePlanAsync(planKey, dto)` | string, UpdatePlanDto | PlanDto |
| 3 | `getPlan(planKey)` | `GetPlanAsync(planKey)` | string | PlanDto \| null |
| 4 | `listPlans(filters?)` | `ListPlansAsync(filters?)` | PlanFilterDto? | PlanDto[] |
| 5 | `getPlansByProduct(productKey)` | `GetPlansByProductAsync(productKey)` | string | PlanDto[] |
| 6 | `archivePlan(planKey)` | `ArchivePlanAsync(planKey)` | string | void |
| 7 | `unarchivePlan(planKey)` | `UnarchivePlanAsync(planKey)` | string | void |
| 8 | `deletePlan(planKey)` | `DeletePlanAsync(planKey)` | string | void |
| 9 | `setFeatureValue(planKey, featureKey, value)` | `SetFeatureValueAsync(...)` | string, string, string | void |
| 10 | `removeFeatureValue(planKey, featureKey)` | `RemoveFeatureValueAsync(...)` | string, string | void |
| 11 | `getFeatureValue(planKey, featureKey)` | `GetFeatureValueAsync(...)` | string, string | string \| null |
| 12 | `getPlanFeatures(planKey)` | `GetPlanFeaturesAsync(planKey)` | string | PlanFeatureDto[] / Array<{featureKey,value}> |

---

## BillingCycleManagementService (billing-cycles.md)

**Access:** `subscrio.billingCycles` / `subscrio.BillingCycles`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `createBillingCycle(dto)` | `CreateBillingCycleAsync(dto)` | CreateBillingCycleDto | BillingCycleDto |
| 2 | `updateBillingCycle(key, dto)` | `UpdateBillingCycleAsync(key, dto)` | string, UpdateBillingCycleDto | BillingCycleDto |
| 3 | `getBillingCycle(key)` | `GetBillingCycleAsync(key)` | string | BillingCycleDto \| null |
| 4 | `getBillingCyclesByPlan(planKey)` | `GetBillingCyclesByPlanAsync(planKey)` | string | BillingCycleDto[] |
| 5 | `listBillingCycles(filters?)` | `ListBillingCyclesAsync(filters?)` | BillingCycleFilterDto? | BillingCycleDto[] |
| 6 | `archiveBillingCycle(key)` | `ArchiveBillingCycleAsync(key)` | string | void |
| 7 | `unarchiveBillingCycle(key)` | `UnarchiveBillingCycleAsync(key)` | string | void |
| 8 | `deleteBillingCycle(key)` | `DeleteBillingCycleAsync(key)` | string | void |
| 9 | `calculateNextPeriodEnd(billingCycleKey, fromDate?)` | `CalculateNextPeriodEndAsync(...)` | string, Date? | Date \| null |
| 10 | `getBillingCyclesByDurationUnit(durationUnit)` | `GetBillingCyclesByDurationUnitAsync(...)` | DurationUnit | BillingCycleDto[] |
| 11 | `getDefaultBillingCycles()` | `GetDefaultBillingCyclesAsync()` | — | BillingCycleDto[] |

---

## CustomerManagementService (customers.md)

**Access:** `subscrio.customers` / `subscrio.Customers`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `createCustomer(dto)` | `CreateCustomerAsync(dto)` | CreateCustomerDto | CustomerDto |
| 2 | `updateCustomer(key, dto)` | `UpdateCustomerAsync(key, dto)` | string, UpdateCustomerDto | CustomerDto |
| 3 | `getCustomer(key)` | `GetCustomerAsync(key)` | string | CustomerDto \| null |
| 4 | `listCustomers(filters?)` | `ListCustomersAsync(filters?)` | CustomerFilterDto? | CustomerDto[] |
| 5 | `archiveCustomer(key)` | `ArchiveCustomerAsync(key)` | string | void |
| 6 | `unarchiveCustomer(key)` | `UnarchiveCustomerAsync(key)` | string | void |
| 7 | `deleteCustomer(key)` | `DeleteCustomerAsync(key)` | string | void |

---

## FeatureCheckerService (feature-checker.md)

**Access:** `subscrio.featureChecker` / `subscrio.FeatureChecker`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `getValueForSubscription(subscriptionKey, featureKey, defaultValue?)` | `GetValueForSubscriptionAsync<T>(...)` | string, string, T? | T \| null |
| 2 | `isEnabledForSubscription(subscriptionKey, featureKey)` | `IsEnabledForSubscriptionAsync(...)` | string, string | boolean |
| 3 | `getAllFeaturesForSubscription(subscriptionKey)` | `GetAllFeaturesForSubscriptionAsync(...)` | string | Map<string,string> |
| 4 | `getValueForCustomer(customerKey, productKey, featureKey, defaultValue?)` | `GetValueForCustomerAsync<T>(...)` | string, string, string, T? | T \| null |
| 5 | `isEnabledForCustomer(customerKey, productKey, featureKey)` | `IsEnabledForCustomerAsync(...)` | string, string, string | boolean |
| 6 | `getAllFeaturesForCustomer(customerKey, productKey)` | `GetAllFeaturesForCustomerAsync(...)` | string, string | Map<string,string> |
| 7 | `hasPlanAccess(customerKey, productKey, planKey)` | `HasPlanAccessAsync(...)` | string, string, string | boolean |
| 8 | `getActivePlans(customerKey)` | `GetActivePlansAsync(customerKey)` | string | string[] |
| 9 | `getFeatureUsageSummary(customerKey, productKey)` | `GetFeatureUsageSummaryAsync(...)` | string, string | FeatureUsageSummaryDto |

---

## ConfigSyncService (config-sync.md)

**Access:** `subscrio.configSync` / `subscrio.ConfigSync`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `syncFromFile(filePath)` | `SyncFromFileAsync(filePath)` | string | ConfigSyncReport |
| 2 | `syncFromJson(config)` | `SyncFromJsonAsync(config)` | ConfigSyncDto | ConfigSyncReport |

---

## StripeIntegrationService (stripe-integration.md)

**Access:** `subscrio.stripe` / `subscrio.Stripe`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `processStripeEvent(event)` | `ProcessStripeEventAsync(event)` | Stripe.Event | void |
| 2 | `createStripeSubscription(customerKey, planKey, billingCycleKey, stripePriceId)` | `CreateStripeSubscriptionAsync(...)` | string, string, string, string | SubscriptionDto / Subscription |
| 3 | `createCheckoutSession(params)` | `CreateCheckoutSessionAsync(params)` | CreateCheckoutSessionParams | { url, sessionId } |

---

## Subscrio (core-overview.md)

**Root class:** `new Subscrio(config)`

| # | TS Method | .NET Method | Inputs | Returns |
|---|-----------|-------------|--------|---------|
| 1 | `installSchema(adminPassphrase?)` | `InstallSchemaAsync(adminPassphrase?)` | string? | void |
| 2 | `verifySchema()` | `VerifySchemaAsync()` | — | string \| null |
| 3 | `migrate()` | `MigrateAsync()` | — | number |
| 4 | `dropSchema()` | `DropSchemaAsync()` | — | void |
| 5 | `close()` | `Dispose()` | — | void |

---

## Documentation Format Rules (from subscriptions)

Every method section MUST follow:
1. **Description** (shared)
2. **=== "TypeScript"** tab: Signature, Inputs, Input Properties (if DTO), Returns, Return Properties (if DTO), Example
3. **=== ".NET"** tab: Same structure with PascalCase / C# types
4. **Expected Results** (shared)
5. **Potential Errors** (shared)

- TypeScript: camelCase, column header `Field`
- .NET: PascalCase, column header `Property`
- Verify against `core.typescript/` and `core.dotnet/` before marking complete
