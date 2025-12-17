using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class SubscriptionsTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public SubscriptionsTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    public class CrudOperations : SubscriptionsTests
    {
        public CrudOperations(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task CreatesSubscriptionWithValidData()
        {
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "sub-customer-1",
                DisplayName: "Sub Customer 1"
            ));

            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "sub-product-1",
                DisplayName: "Sub Product 1"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "sub-plan-1",
                DisplayName: "Sub Plan 1"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "subscription-1",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            subscription.Should().NotBeNull();
            subscription.Key.Should().Be("subscription-1");
            subscription.CustomerKey.Should().Be(customer.Key);
            subscription.ProductKey.Should().Be(product.Key);
            subscription.PlanKey.Should().Be(plan.Key);
            subscription.BillingCycleKey.Should().Be(billingCycle.Key);
            subscription.Status.Should().Be("active");
        }

        [Fact]
        public async Task CreatesSubscriptionWithTrialPeriod()
        {
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "trial-customer",
                DisplayName: "Trial Customer"
            ));

            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "trial-product",
                DisplayName: "Trial Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "trial-plan",
                DisplayName: "Trial Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var trialEnd = DateTime.UtcNow.AddDays(14);

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "trial-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key,
                TrialEndDate: trialEnd
            ));

            subscription.TrialEndDate.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RetrievesSubscriptionByKey()
        {
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "retrieve-sub-customer",
                DisplayName: "Retrieve Sub Customer"
            ));

            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "retrieve-sub-product",
                DisplayName: "Retrieve Sub Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "retrieve-sub-plan",
                DisplayName: "Retrieve Sub Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var created = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "retrieve-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            var retrieved = await _subscrio.Subscriptions.GetSubscriptionAsync(created.Key);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be(created.Key);
        }

        [Fact]
        public async Task UpdatesSubscriptionMetadata()
        {
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "update-sub-customer",
                DisplayName: "Update Sub Customer"
            ));

            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "update-sub-product",
                DisplayName: "Update Sub Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "update-sub-plan",
                DisplayName: "Update Sub Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "update-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            var updated = await _subscrio.Subscriptions.UpdateSubscriptionAsync(subscription.Key, new UpdateSubscriptionDto(
                Metadata: new Dictionary<string, object> { ["updated"] = true }
            ));

            updated.Metadata.Should().ContainKey("updated");
        }

        [Fact]
        public async Task ReturnsNullForNonExistentSubscription()
        {
            var result = await _subscrio.Subscriptions.GetSubscriptionAsync("non-existent-subscription");
            result.Should().BeNull();
        }
    }

    public class FeatureOverrides : SubscriptionsTests
    {
        public FeatureOverrides(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task AddsAndRemovesFeatureOverride()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "override-product",
                DisplayName: "Override Product"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "override-feature",
                DisplayName: "Override Feature",
                ValueType: "numeric",
                DefaultValue: "10"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "override-plan",
                DisplayName: "Override Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "override-customer",
                DisplayName: "Override Customer"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "override-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Add override
            await _subscrio.Subscriptions.AddFeatureOverrideAsync(
                subscription.Key,
                feature.Key,
                "100",
                OverrideType.Permanent
            );

            // Verify override is applied
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("100");

            // Remove override
            await _subscrio.Subscriptions.RemoveFeatureOverrideAsync(subscription.Key, feature.Key);

            // Should fall back to default
            value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("10");
        }

        [Fact]
        public async Task ClearsTemporaryOverrides()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "temp-override-product",
                DisplayName: "Temp Override Product"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "temp-override-feature",
                DisplayName: "Temp Override Feature",
                ValueType: "numeric",
                DefaultValue: "10"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "temp-override-plan",
                DisplayName: "Temp Override Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "temp-override-customer",
                DisplayName: "Temp Override Customer"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "temp-override-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            // Add temporary override
            await _subscrio.Subscriptions.AddFeatureOverrideAsync(
                subscription.Key,
                feature.Key,
                "50",
                OverrideType.Temporary
            );

            // Clear temporary overrides
            await _subscrio.Subscriptions.ClearTemporaryOverridesAsync(subscription.Key);

            // Should fall back to default
            var value = await _subscrio.FeatureChecker.GetValueForCustomerAsync<string>(
                customer.Key,
                product.Key,
                feature.Key
            );
            value.Should().Be("10");
        }
    }

    public class Lifecycle : SubscriptionsTests
    {
        public Lifecycle(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ArchivesAndUnarchivesSubscription()
        {
            var customer = await _subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
                Key: "archive-sub-customer",
                DisplayName: "Archive Sub Customer"
            ));

            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "archive-sub-product",
                DisplayName: "Archive Sub Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "archive-sub-plan",
                DisplayName: "Archive Sub Plan"
            ));

            var billingCycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: $"test-monthly-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Test Monthly",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var subscription = await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "archive-subscription",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle.Key
            ));

            await _subscrio.Subscriptions.ArchiveSubscriptionAsync(subscription.Key);
            var archived = await _subscrio.Subscriptions.GetSubscriptionAsync(subscription.Key);
            archived!.IsArchived.Should().BeTrue();

            await _subscrio.Subscriptions.UnarchiveSubscriptionAsync(subscription.Key);
            var unarchived = await _subscrio.Subscriptions.GetSubscriptionAsync(subscription.Key);
            unarchived!.IsArchived.Should().BeFalse();
        }
    }
}

