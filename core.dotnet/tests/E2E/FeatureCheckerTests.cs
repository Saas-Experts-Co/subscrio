using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class FeatureCheckerTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public FeatureCheckerTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    public class BasicResolution : FeatureCheckerTests
    {
        public BasicResolution(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ResolvesFeatureFromDefaultValue()
        {
            // Create product and feature
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "basic-product",
                DisplayName: "Basic Product"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "max-users",
                DisplayName: "Max Users",
                ValueType: "numeric",
                DefaultValue: "10"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            // Create customer with no subscription
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "test-customer-1",
                DisplayName: "Test Customer"
            ));

            // Should return feature default
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("10");
        }

        [Fact]
        public async Task ResolvesFeatureFromPlanValue()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "plan-product",
                DisplayName: "Plan Product"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "storage-gb",
                DisplayName: "Storage GB",
                ValueType: "numeric",
                DefaultValue: "5"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "pro-plan",
                DisplayName: "Pro Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-fc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly FC",
                DurationUnit: "month"
            ));

            // Set plan value
            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "50");

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "plan-customer",
                DisplayName: "Plan Customer"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "sub-plan",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Should return plan value
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("50");
        }

        [Fact]
        public async Task ResolvesFeatureFromSubscriptionOverride()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: $"override-product-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Override Product"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: $"api-calls-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "API Calls",
                ValueType: "numeric",
                DefaultValue: "100"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "basic",
                DisplayName: "Basic"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-fc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly FC",
                DurationUnit: "month"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "1000");

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: $"override-customer-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Override Customer"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "sub-override",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Add subscription override
            await _subscrio.Subscriptions.AddFeatureOverrideAsync(
                subscription.Key,
                feature.Key,
                "5000",
                OverrideType.Permanent
            );

            // Should return subscription override
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("5000");
        }

        [Fact]
        public async Task ReturnsNullForNonExistentCustomer()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "null-test-product",
                DisplayName: "Null Test Product"
            ));

            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                "non-existent",
                product.Key,
                "any-feature"
            );
            value.Should().BeNull();
        }

        [Fact]
        public async Task ReturnsNullForNonExistentFeature()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "null-feature-product",
                DisplayName: "Null Feature Product"
            ));

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "null-test-customer",
                DisplayName: "Null Test"
            ));

            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                "non-existent-feature"
            );
            value.Should().BeNull();
        }
    }

    public class ResolutionHierarchy : FeatureCheckerTests
    {
        public ResolutionHierarchy(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task SubscriptionOverrideTakesPrecedenceOverPlanValue()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "hierarchy-product-1",
                DisplayName: "Hierarchy Product 1"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "projects",
                DisplayName: "Projects",
                ValueType: "numeric",
                DefaultValue: "3"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "standard",
                DisplayName: "Standard"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-fc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly FC",
                DurationUnit: "month"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "10");

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "hierarchy-customer-1",
                DisplayName: "Hierarchy Customer 1"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "sub-hierarchy-1",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Add override
            await _subscrio.Subscriptions.AddFeatureOverrideAsync(
                subscription.Key,
                feature.Key,
                "25",
                OverrideType.Permanent
            );

            // Should return override (25), not plan (10) or default (3)
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("25");
        }

        [Fact]
        public async Task PlanValueTakesPrecedenceOverFeatureDefault()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "hierarchy-product-2",
                DisplayName: "Hierarchy Product 2"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "storage",
                DisplayName: "Storage",
                ValueType: "numeric",
                DefaultValue: "5"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "premium",
                DisplayName: "Premium"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-fc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly FC",
                DurationUnit: "month"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "100");

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "hierarchy-customer-2",
                DisplayName: "Hierarchy Customer 2"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "sub-hierarchy-2",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Should return plan value (100), not default (5)
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("100");
        }

        [Fact]
        public async Task FallsBackToFeatureDefaultWhenNoPlanValue()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "hierarchy-product-3",
                DisplayName: "Hierarchy Product 3"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "api-limit",
                DisplayName: "API Limit",
                ValueType: "numeric",
                DefaultValue: "1000"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "basic-plan",
                DisplayName: "Basic Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-fc-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly FC",
                DurationUnit: "month"
            ));

            // Don't set plan value - should use default

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "hierarchy-customer-3",
                DisplayName: "Hierarchy Customer 3"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "sub-hierarchy-3",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Should return feature default (1000)
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("1000");
        }
    }

    public class CompleteResolutionHierarchy : FeatureCheckerTests
    {
        public CompleteResolutionHierarchy(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ResolvesFromSubscriptionOverridePlanValueFeatureDefault()
        {
            // 1. Create product
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "test-product",
                DisplayName: "Test Product"
            ));

            // 2. Create feature with default value
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "max-projects",
                DisplayName: "Max Projects",
                ValueType: "numeric",
                DefaultValue: "10"
            ));

            // 3. Associate feature with product
            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            // 4. Create plan and set feature value
            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "pro",
                DisplayName: "Pro Plan"
            ));
            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "50");

            // 5. Create customer
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "test-customer"
            ));

            // 6. Create billing cycle
            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "monthly",
                DisplayName: "Monthly",
                DurationUnit: "month"
            ));

            // 7. Create subscription
            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "test-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // TEST: Should resolve from plan value
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("50");

            // 8. Add subscription override
            await _subscrio.Subscriptions.AddFeatureOverrideAsync(
                subscription.Key,
                feature.Key,
                "100",
                OverrideType.Permanent
            );

            // TEST: Should now resolve from subscription override
            value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("100");

            // 9. Remove override
            await _subscrio.Subscriptions.RemoveFeatureOverrideAsync(subscription.Key, feature.Key);

            // TEST: Should fall back to plan value
            value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("50");

            // 10. Remove plan feature value
            await _subscrio.Plans.RemoveFeatureValueAsync(plan.Key, feature.Key);

            // TEST: Should fall back to feature default
            value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("10");
        }
    }
}

