# Subscrio Entity Relationships

This reference explains how the core domain objects relate to each other, plus the feature resolution order and customer key guidelines that were previously embedded in `getting-started.md`.

## Product, Plan, Feature, and Billing Cycle Relationships

- **Product → Features (many-to-many)**  
  Products can expose multiple features, and the same feature definition can be reused across multiple products. Use `ProductManagementService.associateFeature()` and `dissociateFeature()` to manage the join table.

- **Product → Plans (one-to-many)**  
  Plans always belong to a product (scoped by `productKey`). Plan keys remain globally unique even though they reference a product.

- **Plan → Features (via feature values)**  
  Plans set concrete values for their product’s features with `PlanManagementService.setFeatureValue()` and `removeFeatureValue()`. Each entry stores `{ featureKey, value }`.

- **Plan → Billing Cycles (one-to-many)**  
  Billing cycles belong to a single plan and capture cadence, duration, and optional external product IDs (e.g., Stripe price IDs). Keys are global and not scoped to the plan.

- **Customer → Subscriptions (one-to-many)**  
  Customers are identified by caller-supplied keys (your internal user IDs). A customer can hold multiple active/trial subscriptions simultaneously.

- **Subscription → Plan (many-to-one)**  
  Subscriptions reference the billing cycle, which derives the plan/product relationship. Subscriptions can optionally override feature values on top of the plan defaults.

See the dedicated service docs for CRUD and DTO details: `products.md`, `plans.md`, `features.md`, `billing-cycles.md`, and `subscriptions.md`.

## Feature Resolution Hierarchy

When calling `FeatureCheckerService`, values always resolve in this order:

1. **Subscription override** – Permanent or temporary override stored on the subscription.
2. **Plan value** – The plan’s configured feature value.
3. **Feature default** – The default defined on the feature itself.

If a subscription has multiple overrides for the same feature, the latest write replaces the prior value. When a customer holds multiple subscriptions, any subscription with an override wins; otherwise the first plan that supplies a value is used before falling back to the feature default. Refer to `feature-checker.md` for API-level guidance.

## Customer Keys

- Always pass **your application’s customer identifier** as `customerKey`. Subscrio never generates or mutates this value.
- Feature checker queries accept `customerKey` (not the internal UUID) so you can resolve features without fetching the customer record first.
- Customer management methods (`customers.md`) use `key` consistently across create/update/list APIs.

Maintaining consistent customer keys ensures migrations and feature checks remain predictable when you sync data from your source-of-truth system.
