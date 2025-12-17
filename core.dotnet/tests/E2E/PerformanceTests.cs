using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class PerformanceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public PerformanceTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    [Fact]
    public async Task ListsProductsEfficiently()
    {
        // Create multiple products
        for (int i = 0; i < 10; i++)
        {
            await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: $"perf-product-{i}",
                DisplayName: $"Performance Product {i}"
            ));
        }

        // List products - should complete quickly
        var startTime = DateTime.UtcNow;
        var products = await _subscrio.Products.ListProductsAsync();
        var endTime = DateTime.UtcNow;

        var duration = endTime - startTime;
        duration.TotalSeconds.Should().BeLessThan(2); // Should be fast

        products.Should().HaveCountGreaterOrEqualTo(10);
    }

    [Fact]
    public async Task ResolvesFeaturesEfficiently()
    {
        // Create product with features
        var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
            Key: "perf-feature-product",
            DisplayName: "Performance Feature Product"
        ));

        // Create multiple features
        var features = new List<FeatureDto>();
        for (int i = 0; i < 5; i++)
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: $"perf-feature-{i}",
                DisplayName: $"Performance Feature {i}",
                ValueType: "numeric",
                DefaultValue: "10"
            ));
            features.Add(feature);
            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);
        }

        // Create plan and set feature values
        var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
            ProductKey: product.Key,
            Key: "perf-plan",
            DisplayName: "Performance Plan"
        ));

        foreach (var feature in features)
        {
            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "50");
        }

        // Create customer and subscription
        var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
            Key: "perf-customer",
            DisplayName: "Performance Customer"
        ));

        var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
            PlanKey: plan.Key,
            Key: "perf-cycle",
            DisplayName: "Performance Cycle",
            DurationUnit: "month"
        ));

        await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
            Key: "perf-subscription",
            CustomerKey: customer.Key,
            BillingCycleKey: billingCycle.Key
        ));

        // Resolve all features - should complete quickly
        var startTime = DateTime.UtcNow;
        var allFeatures = await _subscrio.FeatureChecker.GetAllFeaturesForCustomerAsync(
            customer.Key,
            product.Key
        );
        var endTime = DateTime.UtcNow;

        var duration = endTime - startTime;
        duration.TotalSeconds.Should().BeLessThan(2); // Should be fast

        allFeatures.Should().HaveCount(5);
    }
}


