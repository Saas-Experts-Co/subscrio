using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class FeatureCheckerCachingTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public FeatureCheckerCachingTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    [Fact]
    public async Task ResolvesFeaturesForMultipleSubscriptionsEfficiently()
    {
        // Create a product with multiple plans
        var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
            Key: "caching-test-product",
            DisplayName: "Caching Test Product"
        ));

        // Create feature
        var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
            Key: "test-feature",
            DisplayName: "Test Feature",
            ValueType: "toggle",
            DefaultValue: "false"
        ));

        await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

        // Create multiple plans
        var plans = new List<PlanDto>();
        for (int i = 0; i < 3; i++)
        {
            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: $"plan-{i}",
                DisplayName: $"Plan {i}"
            ));
            plans.Add(plan);
            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "true");
        }

        // Create customer with multiple subscriptions
        var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
            Key: "caching-test-customer",
            DisplayName: "Caching Test Customer"
        ));

        // Create subscriptions for all plans
        var subscriptions = new List<SubscriptionDto>();
        for (int i = 0; i < plans.Count; i++)
        {
            var plan = plans[i];
            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"caching-cycle-{i}",
                DisplayName: $"Caching Cycle {i}",
                DurationUnit: "month"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: $"sub-{i}",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));
            subscriptions.Add(subscription);
        }

        // Get all features for customer - should use caching
        var startTime = DateTime.UtcNow;
        var allFeatures = await _subscrio.FeatureChecker.GetAllFeaturesForCustomerAsync(
            customer.Key,
            product.Key
        );
        var endTime = DateTime.UtcNow;

        // Should complete quickly (caching should help)
        var duration = endTime - startTime;
        duration.TotalSeconds.Should().BeLessThan(5); // Should be fast

        // Verify feature values are correct
        allFeatures.Should().ContainKey(feature.Key);
        allFeatures[feature.Key].Should().Be("true");
    }
}


