using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Tests.Setup;
using Xunit;
using System.Collections.Generic;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class PlansTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public PlansTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    public class CrudOperations : PlansTests
    {
        public CrudOperations(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task CreatesPlanWithValidData()
        {
            var product = await _fixtures.CreateProductAsync();
            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: $"basic-plan-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Basic Plan",
                Description: "A basic plan"
            ));

            plan.Should().NotBeNull();
            plan.Key.Should().StartWith("basic-plan-");
            plan.ProductKey.Should().Be(product.Key);
            plan.DisplayName.Should().Be("Basic Plan");
            plan.Status.Should().Be("active");
        }

        [Fact]
        public async Task RetrievesPlanByPlanKey()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "retrieve-product" },
                { "DisplayName", "Retrieve Product" }
            });

            var created = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "retrieve-plan",
                DisplayName: "Retrieve Plan"
            ));

            var retrieved = await _subscrio.Plans.GetPlanAsync(created.Key);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be(created.Key);
            retrieved.ProductKey.Should().Be(product.Key);
        }

        [Fact]
        public async Task UpdatesPlanDisplayName()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "update-name-product" },
                { "DisplayName", "Update Name Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "update-name-plan",
                DisplayName: "Original Name"
            ));

            var updated = await _subscrio.Plans.UpdatePlanAsync(plan.Key, new UpdatePlanDto(
                DisplayName: "Updated Name"
            ));

            updated.DisplayName.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdatesPlanDescription()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "update-desc-product" },
                { "DisplayName", "Update Desc Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "update-desc-plan",
                DisplayName: "Update Desc",
                Description: "Old description"
            ));

            var updated = await _subscrio.Plans.UpdatePlanAsync(plan.Key, new UpdatePlanDto(
                Description: "New description"
            ));

            updated.Description.Should().Be("New description");
        }

        [Fact]
        public async Task ReturnsNullForNonExistentPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "null-plan-product" },
                { "DisplayName", "Null Plan Product" }
            });

            var result = await _subscrio.Plans.GetPlanAsync("non-existent-plan");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ThrowsErrorWhenUpdatingNonExistentPlan()
        {
            await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", $"error-plan-product-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" },
                { "DisplayName", "Error Plan Product" }
            });

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _subscrio.Plans.UpdatePlanAsync("non-existent", new UpdatePlanDto(
                    DisplayName: "New Name"
                ))
            );
        }
    }

    public class ValidationTests : PlansTests
    {
        public ValidationTests(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ThrowsErrorForEmptyPlanKey()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "empty-key-product" },
                { "DisplayName", "Empty Key Product" }
            });

            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                    ProductKey: product.Key,
                    Key: "",
                    DisplayName: "Test"
                ))
            );
        }

        [Fact]
        public async Task ThrowsErrorForInvalidKeyFormat()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", $"invalid-key-product-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" },
                { "DisplayName", "Invalid Key Product" }
            });

            await Assert.ThrowsAsync<ValidationException>(async () =>
                await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                    ProductKey: product.Key,
                    Key: "Invalid Key!",
                    DisplayName: "Invalid"
                ))
            );
        }

        [Fact]
        public async Task ThrowsErrorForDuplicatePlanKeyGlobally()
        {
            var product1 = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "product-1-dup" },
                { "DisplayName", "Product 1" }
            });

            var product2 = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "product-2-dup" },
                { "DisplayName", "Product 2" }
            });

            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product1.Key,
                Key: "duplicate-plan",
                DisplayName: "Plan 1"
            ));

            await Assert.ThrowsAsync<ConflictException>(async () =>
                await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                    ProductKey: product2.Key,
                    Key: "duplicate-plan",
                    DisplayName: "Plan 2"
                ))
            );
        }

        [Fact]
        public async Task ThrowsErrorForNonExistentProductKey()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                    ProductKey: "non-existent-product",
                    Key: "test-plan",
                    DisplayName: "Test Plan"
                ))
            );
        }
    }

    public class LifecycleStatusTests : PlansTests
    {
        public LifecycleStatusTests(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ActivatesAPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "activate-plan-product" },
                { "DisplayName", "Activate Plan Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "activate-plan",
                DisplayName: "Activate Plan"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);
            await _subscrio.Plans.UnarchivePlanAsync(plan.Key);

            var retrieved = await _subscrio.Plans.GetPlanAsync(plan.Key);
            retrieved!.Status.Should().Be("active");
        }

        [Fact]
        public async Task DeactivatesAPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "deactivate-plan-product" },
                { "DisplayName", "Deactivate Plan Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "deactivate-plan",
                DisplayName: "Deactivate Plan"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);

            var retrieved = await _subscrio.Plans.GetPlanAsync(plan.Key);
            retrieved!.Status.Should().Be("archived");
        }

        [Fact]
        public async Task ArchivesAPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "archive-plan-product" },
                { "DisplayName", "Archive Plan Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "archive-plan",
                DisplayName: "Archive Plan"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);

            var retrieved = await _subscrio.Plans.GetPlanAsync(plan.Key);
            retrieved!.Status.Should().Be("archived");
        }

        [Fact]
        public async Task DeletesAnArchivedPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "delete-plan-product" },
                { "DisplayName", "Delete Plan Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "delete-plan",
                DisplayName: "Delete Plan"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);
            await _subscrio.Plans.DeletePlanAsync(plan.Key);

            var retrieved = await _subscrio.Plans.GetPlanAsync(plan.Key);
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task ThrowsErrorWhenDeletingActivePlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "delete-active-plan-product" },
                { "DisplayName", "Delete Active Plan Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "delete-active-plan",
                DisplayName: "Delete Active Plan"
            ));

            await Assert.ThrowsAsync<DomainException>(async () =>
                await _subscrio.Plans.DeletePlanAsync(plan.Key)
            );
        }

        [Fact]
        public async Task DeletesArchivedPlanSuccessfully()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "delete-archived-plan-product" },
                { "DisplayName", "Delete Archived Plan Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "delete-archived-plan",
                DisplayName: "Delete Archived Plan"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);
            await _subscrio.Plans.DeletePlanAsync(plan.Key);

            var retrieved = await _subscrio.Plans.GetPlanAsync(plan.Key);
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task PreventsDeletionOfPlanWithBillingCycles()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "plan-with-cycles-product" },
                { "DisplayName", "Plan With Cycles Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "plan-with-cycles",
                DisplayName: "Plan With Cycles"
            ));

            await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "cycle-for-plan",
                DisplayName: "Cycle For Plan",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);

            await Assert.ThrowsAsync<DomainException>(async () =>
                await _subscrio.Plans.DeletePlanAsync(plan.Key)
            );
        }

        [Fact]
        public async Task PreventsDeletionOfPlanWithSubscriptions()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "plan-with-subs-product" },
                { "DisplayName", "Plan With Subs Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "plan-with-subs",
                DisplayName: "Plan With Subs"
            ));

            var billingCycle1 = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "cycle-1-for-subs",
                DisplayName: "Cycle 1 For Subs",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var billingCycle2 = await _subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                PlanKey: plan.Key,
                Key: "cycle-2-for-subs",
                DisplayName: "Cycle 2 For Subs",
                DurationValue: 1,
                DurationUnit: "months"
            ));

            var customer = await _fixtures.CreateCustomerAsync(new Dictionary<string, object>
            {
                { "Key", "customer-for-plan" },
                { "DisplayName", "Customer For Plan" }
            });

            // Create subscription using cycle 1
            await _subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                Key: "sub-for-plan",
                CustomerKey: customer.Key,
                BillingCycleKey: billingCycle1.Key
            ));

            // Archive and delete cycle 1 (this will fail because it has subscriptions)
            await _subscrio.BillingCycles.ArchiveBillingCycleAsync(billingCycle1.Key);
            await Assert.ThrowsAsync<DomainException>(async () =>
                await _subscrio.BillingCycles.DeleteBillingCycleAsync(billingCycle1.Key)
            );

            // Archive and delete cycle 2 (should work - no subscriptions)
            await _subscrio.BillingCycles.ArchiveBillingCycleAsync(billingCycle2.Key);
            await _subscrio.BillingCycles.DeleteBillingCycleAsync(billingCycle2.Key);

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);

            // Now try to delete plan - should fail because it has subscriptions (even though cycle 1 is archived)
            await Assert.ThrowsAsync<DomainException>(async () =>
                await _subscrio.Plans.DeletePlanAsync(plan.Key)
            );
        }
    }

    public class ListFilterTests : PlansTests
    {
        public ListFilterTests(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ListsAllPlans()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "list-plans-product" },
                { "DisplayName", "List Plans Product" }
            });

            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "list-plan-1",
                DisplayName: "List Plan 1"
            ));
            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "list-plan-2",
                DisplayName: "List Plan 2"
            ));

            var plans = await _subscrio.Plans.ListPlansAsync();
            plans.Should().HaveCountGreaterOrEqualTo(2);
        }

        [Fact]
        public async Task FiltersPlansByProductKey()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "filter-product-key" },
                { "DisplayName", "Filter Product Key" }
            });

            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "filter-plan-by-product",
                DisplayName: "Filter Plan"
            ));

            var plans = await _subscrio.Plans.ListPlansAsync(new PlanFilterDto(
                ProductKey: product.Key
            ));
            plans.Should().AllSatisfy(p => p.ProductKey.Should().Be(product.Key));
        }

        [Fact]
        public async Task FiltersPlansByStatusActive()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "filter-active-product" },
                { "DisplayName", "Filter Active Product" }
            });

            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "filter-active-plan",
                DisplayName: "Filter Active Plan"
            ));

            var activePlans = await _subscrio.Plans.ListPlansAsync(new PlanFilterDto(
                Status: "active"
            ));
            activePlans.Should().AllSatisfy(p => p.Status.Should().Be("active"));
        }

        [Fact]
        public async Task FiltersPlansByStatusArchived()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "filter-archived-product" },
                { "DisplayName", "Filter Archived Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "filter-archived-plan",
                DisplayName: "Filter Archived Plan"
            ));

            await _subscrio.Plans.ArchivePlanAsync(plan.Key);

            var archivedPlans = await _subscrio.Plans.ListPlansAsync(new PlanFilterDto(
                Status: "archived"
            ));
            archivedPlans.Should().Contain(p => p.Key == plan.Key);
        }

        [Fact]
        public async Task SearchesPlansByKeyOrDisplayName()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "search-plans-product" },
                { "DisplayName", "Search Plans Product" }
            });

            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "search-unique-plan",
                DisplayName: "Search Unique Plan"
            ));

            var plans = await _subscrio.Plans.ListPlansAsync(new PlanFilterDto(
                Search: "search-unique"
            ));
            plans.Should().Contain(p => p.Key == "search-unique-plan");
        }

        [Fact]
        public async Task PaginatesPlanList()
        {
            var plans = await _subscrio.Plans.ListPlansAsync(new PlanFilterDto(
                Limit: 5
            ));
            plans.Should().HaveCountLessOrEqualTo(5);
        }

        [Fact]
        public async Task GetsPlansByProductKey()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "get-by-product" },
                { "DisplayName", "Get By Product" }
            });

            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "plan-by-product-1",
                DisplayName: "Plan 1"
            ));
            await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "plan-by-product-2",
                DisplayName: "Plan 2"
            ));

            var plans = await _subscrio.Plans.GetPlansByProductAsync(product.Key);
            plans.Should().HaveCount(2);
        }
    }

    public class FeatureValueManagement : PlansTests
    {
        public FeatureValueManagement(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task SetsFeatureValueForPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "set-value-product" },
                { "DisplayName", "Set Value Product" }
            });

            var feature = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "set-value-feature" },
                { "DisplayName", "Set Value Feature" },
                { "ValueType", "numeric" },
                { "DefaultValue", "10" }
            });

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "set-value-plan",
                DisplayName: "Set Value Plan"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "50");

            var value = await _subscrio.Plans.GetFeatureValueAsync(plan.Key, feature.Key);
            value.Should().Be("50");
        }

        [Fact]
        public async Task UpdatesExistingFeatureValue()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "update-value-product" },
                { "DisplayName", "Update Value Product" }
            });

            var feature = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "update-value-feature" },
                { "DisplayName", "Update Value Feature" },
                { "ValueType", "numeric" },
                { "DefaultValue", "10" }
            });

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "update-value-plan",
                DisplayName: "Update Value Plan"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "50");
            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "100");

            var value = await _subscrio.Plans.GetFeatureValueAsync(plan.Key, feature.Key);
            value.Should().Be("100");
        }

        [Fact]
        public async Task RemovesFeatureValueFromPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "remove-value-product" },
                { "DisplayName", "Remove Value Product" }
            });

            var feature = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "remove-value-feature" },
                { "DisplayName", "Remove Value Feature" },
                { "ValueType", "numeric" },
                { "DefaultValue", "10" }
            });

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "remove-value-plan",
                DisplayName: "Remove Value Plan"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "50");
            await _subscrio.Plans.RemoveFeatureValueAsync(plan.Key, feature.Key);

            var value = await _subscrio.Plans.GetFeatureValueAsync(plan.Key, feature.Key);
            value.Should().BeNull();
        }

        [Fact]
        public async Task GetsFeatureValueForPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "get-value-product" },
                { "DisplayName", "Get Value Product" }
            });

            var feature = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "get-value-feature" },
                { "DisplayName", "Get Value Feature" },
                { "ValueType", "toggle" },
                { "DefaultValue", "false" }
            });

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "get-value-plan",
                DisplayName: "Get Value Plan"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature.Key, "true");

            var value = await _subscrio.Plans.GetFeatureValueAsync(plan.Key, feature.Key);
            value.Should().Be("true");
        }

        [Fact]
        public async Task ReturnsNullForNonExistentFeatureValue()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "null-value-product" },
                { "DisplayName", "Null Value Product" }
            });

            var feature = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "null-value-feature" },
                { "DisplayName", "Null Value Feature" },
                { "ValueType", "numeric" },
                { "DefaultValue", "10" }
            });

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "null-value-plan",
                DisplayName: "Null Value Plan"
            ));

            var value = await _subscrio.Plans.GetFeatureValueAsync(plan.Key, feature.Key);
            value.Should().BeNull();
        }

        [Fact]
        public async Task GetsAllPlanFeatures()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "all-features-product" },
                { "DisplayName", "All Features Product" }
            });

            var feature1 = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "all-features-1" },
                { "DisplayName", "All Features 1" },
                { "ValueType", "numeric" },
                { "DefaultValue", "10" }
            });

            var feature2 = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "all-features-2" },
                { "DisplayName", "All Features 2" },
                { "ValueType", "toggle" },
                { "DefaultValue", "false" }
            });

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature1.Key);
            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature2.Key);

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "all-features-plan",
                DisplayName: "All Features Plan"
            ));

            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature1.Key, "50");
            await _subscrio.Plans.SetFeatureValueAsync(plan.Key, feature2.Key, "true");

            var features = await _subscrio.Plans.GetPlanFeaturesAsync(plan.Key);
            features.Should().HaveCount(2);
            features.First(f => f.FeatureKey == feature1.Key).Value.Should().Be("50");
            features.First(f => f.FeatureKey == feature2.Key).Value.Should().Be("true");
        }

        [Fact]
        public async Task ThrowsErrorWhenSettingValueForNonExistentFeature()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "error-feature-product" },
                { "DisplayName", "Error Feature Product" }
            });

            var plan = await _subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
                ProductKey: product.Key,
                Key: "error-feature-plan",
                DisplayName: "Error Feature Plan"
            ));

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _subscrio.Plans.SetFeatureValueAsync(plan.Key, "non-existent-feature", "50")
            );
        }

        [Fact]
        public async Task ThrowsErrorWhenSettingValueForNonExistentPlan()
        {
            var product = await _fixtures.CreateProductAsync(new Dictionary<string, object>
            {
                { "Key", "error-plan-feature-product" },
                { "DisplayName", "Error Plan Feature Product" }
            });

            var feature = await _fixtures.CreateFeatureAsync(new Dictionary<string, object>
            {
                { "Key", "error-plan-feature" },
                { "DisplayName", "Error Plan Feature" },
                { "ValueType", "numeric" },
                { "DefaultValue", "10" }
            });

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _subscrio.Plans.SetFeatureValueAsync("non-existent-plan", feature.Key, "50")
            );
        }
    }
}
