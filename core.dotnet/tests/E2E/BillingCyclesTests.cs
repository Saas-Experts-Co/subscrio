using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class BillingCyclesTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public BillingCyclesTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    public class CrudOperations : BillingCyclesTests
    {
        public CrudOperations(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task CreatesBillingCycleDays()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "billing-test-product",
                DisplayName: "Billing Test Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "billing-test-plan",
                DisplayName: "Billing Test Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "daily-cycle",
                DisplayName: "Daily Cycle",
                DurationValue: 1,
                DurationUnit: "days"
            ));

            cycle.Should().NotBeNull();
            cycle.ProductKey.Should().Be(product.Key);
            cycle.PlanKey.Should().Be(plan.Key);
            cycle.Key.Should().Be("daily-cycle");
            cycle.DurationValue.Should().Be(1);
            cycle.DurationUnit.Should().Be("days");
        }

        [Fact]
        public async Task CreatesBillingCycleWeeks()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "billing-weeks-product",
                DisplayName: "Billing Weeks Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "billing-weeks-plan",
                DisplayName: "Billing Weeks Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "weekly-cycle",
                DisplayName: "Weekly Cycle",
                DurationValue: 1,
                DurationUnit: "weeks"
            ));

            cycle.DurationUnit.Should().Be("weeks");
        }

        [Fact]
        public async Task CreatesBillingCycleMonths()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "billing-months-product",
                DisplayName: "Billing Months Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "billing-months-plan",
                DisplayName: "Billing Months Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "monthly-cycle",
                DisplayName: "Monthly Cycle",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            cycle.DurationUnit.Should().Be("months");
        }

        [Fact]
        public async Task CreatesBillingCycleYears()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "billing-years-product",
                DisplayName: "Billing Years Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "billing-years-plan",
                DisplayName: "Billing Years Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "yearly-cycle",
                DisplayName: "Yearly Cycle",
                DurationValue: 1,
                DurationUnit: "years"
            ));

            cycle.DurationUnit.Should().Be("years");
        }

        [Fact]
        public async Task CreatesBillingCycleWithExternalProductId()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "external-product",
                DisplayName: "External Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "external-plan",
                DisplayName: "External Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "stripe-monthly",
                DisplayName: "Stripe Monthly",
                DurationValue: 1,
                DurationUnit: "months",
                ExternalProductId: "price_1234567890"
            ));

            cycle.ExternalProductId.Should().Be("price_1234567890");
        }

        [Fact]
        public async Task RetrievesBillingCycleByKey()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "retrieve-cycle-product",
                DisplayName: "Retrieve Cycle Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "retrieve-cycle-plan",
                DisplayName: "Retrieve Cycle Plan"
            ));

            var created = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "retrieve-cycle",
                DisplayName: "Retrieve Cycle",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var retrieved = await _subscrio.BillingCycles.GetBillingCycleAsync(created.Key);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be(created.Key);
        }

        [Fact]
        public async Task UpdatesBillingCycleDisplayName()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "update-name-product",
                DisplayName: "Update Name Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "update-name-plan",
                DisplayName: "Update Name Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "update-name-cycle",
                DisplayName: "Original Name",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var updated = await _subscrio.BillingCycles.UpdateBillingCycleAsync(cycle.Key, new UpdateBillingCycleDto(
                DisplayName: "Updated Name"
            ));

            updated.DisplayName.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdatesBillingCycleDuration()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "update-duration-product",
                DisplayName: "Update Duration Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "update-duration-plan",
                DisplayName: "Update Duration Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "update-duration-cycle",
                DisplayName: "Update Duration",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var updated = await _subscrio.BillingCycles.UpdateBillingCycleAsync(cycle.Key, new UpdateBillingCycleDto(
                DurationValue: 3
            ));

            updated.DurationValue.Should().Be(3);
        }

        [Fact]
        public async Task ReturnsNullForNonExistentBillingCycle()
        {
            var result = await _subscrio.BillingCycles.GetBillingCycleAsync("non-existent-cycle");
            result.Should().BeNull();
        }
    }

    public class Validation : BillingCyclesTests
    {
        public Validation(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ThrowsErrorForDuplicateBillingCycleKey()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "duplicate-cycle-product",
                DisplayName: "Duplicate Cycle Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "duplicate-cycle-plan",
                DisplayName: "Duplicate Cycle Plan"
            ));

            await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "duplicate-cycle",
                DisplayName: "Cycle 1",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            await Assert.ThrowsAsync<ConflictException>(async () =>
            {
                await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                    PlanKey: plan.Key,
                    Key: "duplicate-cycle",
                    DisplayName: "Cycle 2",
                    DurationValue: 1,
                    DurationUnit: "months"
                ));
            });
        }

        [Fact]
        public async Task ThrowsErrorForNonExistentPlanKey()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                    PlanKey: "non-existent-plan",
                    Key: "test-cycle",
                    DisplayName: "Test Cycle",
                    DurationValue: 1,
                    DurationUnit: "months"
                ));
            });
        }
    }

    public class Lifecycle : BillingCyclesTests
    {
        public Lifecycle(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ArchivesAndUnarchivesBillingCycle()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "lifecycle-product",
                DisplayName: "Lifecycle Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "lifecycle-plan",
                DisplayName: "Lifecycle Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "lifecycle-cycle",
                DisplayName: "Lifecycle Cycle",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            await _subscrio.BillingCycles.ArchiveBillingCycleAsync(cycle.Key);
            var archived = await _subscrio.BillingCycles.GetBillingCycleAsync(cycle.Key);
            archived!.Status.Should().Be("archived");

            await _subscrio.BillingCycles.UnarchiveBillingCycleAsync(cycle.Key);
            var unarchived = await _subscrio.BillingCycles.GetBillingCycleAsync(cycle.Key);
            unarchived!.Status.Should().Be("active");
        }

        [Fact]
        public async Task DeletesArchivedBillingCycle()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "delete-cycle-product",
                DisplayName: "Delete Cycle Product"
            ));

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "delete-cycle-plan",
                DisplayName: "Delete Cycle Plan"
            ));

            var cycle = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "delete-cycle",
                DisplayName: "Delete Cycle",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            await _subscrio.BillingCycles.ArchiveBillingCycleAsync(cycle.Key);
            await _subscrio.BillingCycles.DeleteBillingCycleAsync(cycle.Key);

            var result = await _subscrio.BillingCycles.GetBillingCycleAsync(cycle.Key);
            result.Should().BeNull();
        }
    }
}


