using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class FeaturesTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public FeaturesTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    public class CrudOperations : FeaturesTests
    {
        public CrudOperations(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task CreatesFeatureWithValidDataToggleType()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "toggle-feature",
                DisplayName: "Toggle Feature",
                Description: "A toggle feature",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            feature.Should().NotBeNull();
            feature.Key.Should().Be("toggle-feature");
            feature.DisplayName.Should().Be("Toggle Feature");
            feature.ValueType.Should().Be("toggle");
            feature.DefaultValue.Should().Be("false");
            feature.Status.Should().Be("active");
        }

        [Fact]
        public async Task CreatesFeatureWithValidDataNumericType()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "numeric-feature",
                DisplayName: "Numeric Feature",
                ValueType: "numeric",
                DefaultValue: "100"
            ));

            feature.ValueType.Should().Be("numeric");
            feature.DefaultValue.Should().Be("100");
        }

        [Fact]
        public async Task CreatesFeatureWithValidDataTextType()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "text-feature",
                DisplayName: "Text Feature",
                ValueType: "text",
                DefaultValue: "default-text"
            ));

            feature.ValueType.Should().Be("text");
            feature.DefaultValue.Should().Be("default-text");
        }

        [Fact]
        public async Task RetrievesFeatureByKeyAfterCreation()
        {
            var created = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "retrieve-feature",
                DisplayName: "Retrieve Feature",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            var retrieved = await _subscrio.Features.GetFeatureAsync(created.Key);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be(created.Key);
            retrieved.DisplayName.Should().Be(created.DisplayName);
        }

        [Fact]
        public async Task UpdatesFeatureDisplayName()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "update-name-feature",
                DisplayName: "Original Name",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            var updated = await _subscrio.Features.UpdateFeatureAsync(feature.Key, new UpdateFeatureDto(
                DisplayName: "Updated Name"
            ));

            updated.DisplayName.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdatesFeatureDefaultValue()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: $"update-value-feature-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                DisplayName: "Update Value",
                ValueType: "numeric",
                DefaultValue: "10"
            ));

            var updated = await _subscrio.Features.UpdateFeatureAsync(feature.Key, new UpdateFeatureDto(
                DefaultValue: "20"
            ));

            updated.DefaultValue.Should().Be("20");
        }

        [Fact]
        public async Task UpdatesFeatureDescription()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "update-desc-feature",
                DisplayName: "Update Desc",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            var updated = await _subscrio.Features.UpdateFeatureAsync(feature.Key, new UpdateFeatureDto(
                Description: "Updated description"
            ));

            updated.Description.Should().Be("Updated description");
        }

        [Fact]
        public async Task ReturnsNullForNonExistentFeature()
        {
            var result = await _subscrio.Features.GetFeatureAsync("non-existent-feature");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ThrowsErrorWhenUpdatingNonExistentFeature()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _subscrio.Features.UpdateFeatureAsync("non-existent-feature", new UpdateFeatureDto(
                    DisplayName: "New Name"
                ));
            });
        }
    }

    public class FeatureValidation : FeaturesTests
    {
        public FeatureValidation(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ThrowsErrorForDuplicateFeatureKey()
        {
            await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "duplicate-feature",
                DisplayName: "Feature 1",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            await Assert.ThrowsAsync<ConflictException>(async () =>
            {
                await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                    Key: "duplicate-feature",
                    DisplayName: "Feature 2",
                    ValueType: "toggle",
                    DefaultValue: "false"
                ));
            });
        }

        [Fact]
        public async Task ValidatesFeatureKeyFormat()
        {
            await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                    Key: "INVALID KEY!",
                    DisplayName: "Invalid Feature",
                    ValueType: "toggle",
                    DefaultValue: "false"
                ));
            });
        }

        [Fact]
        public async Task ValidatesValueType()
        {
            await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                    Key: "invalid-type-feature",
                    DisplayName: "Invalid Type",
                    ValueType: "invalid-type",
                    DefaultValue: "false"
                ));
            });
        }

        [Fact]
        public async Task ValidatesDefaultValueForToggleType()
        {
            await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                    Key: "invalid-toggle-feature",
                    DisplayName: "Invalid Toggle",
                    ValueType: "toggle",
                    DefaultValue: "invalid"
                ));
            });
        }

        [Fact]
        public async Task ValidatesDefaultValueForNumericType()
        {
            await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                    Key: "invalid-numeric-feature",
                    DisplayName: "Invalid Numeric",
                    ValueType: "numeric",
                    DefaultValue: "not-a-number"
                ));
            });
        }
    }

    public class FeatureLifecycle : FeaturesTests
    {
        public FeatureLifecycle(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ArchivesAndUnarchivesFeature()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "lifecycle-feature",
                DisplayName: "Lifecycle Feature",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            await _subscrio.Features.ArchiveFeatureAsync(feature.Key);
            var archived = await _subscrio.Features.GetFeatureAsync(feature.Key);
            archived!.Status.Should().Be("archived");

            await _subscrio.Features.UnarchiveFeatureAsync(feature.Key);
            var unarchived = await _subscrio.Features.GetFeatureAsync(feature.Key);
            unarchived!.Status.Should().Be("active");
        }

        [Fact]
        public async Task DeletesArchivedFeature()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "delete-feature",
                DisplayName: "Delete Feature",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            await _subscrio.Features.ArchiveFeatureAsync(feature.Key);
            await _subscrio.Features.DeleteFeatureAsync(feature.Key);

            var result = await _subscrio.Features.GetFeatureAsync(feature.Key);
            result.Should().BeNull();
        }

        [Fact]
        public async Task PreventsDeletionOfActiveFeature()
        {
            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "no-delete-feature",
                DisplayName: "No Delete",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            await Assert.ThrowsAsync<DomainException>(async () =>
            {
                await _subscrio.Features.DeleteFeatureAsync(feature.Key);
            });
        }
    }

    public class FeatureListing : FeaturesTests
    {
        public FeatureListing(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ListsAllFeatures()
        {
            await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "list-1",
                DisplayName: "List 1",
                ValueType: "toggle",
                DefaultValue: "false"
            ));
            await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "list-2",
                DisplayName: "List 2",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            var features = await _subscrio.Features.ListFeaturesAsync();
            features.Should().HaveCountGreaterOrEqualTo(2);
        }

        [Fact]
        public async Task FiltersFeaturesByStatus()
        {
            var active = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "filter-active",
                DisplayName: "Filter Active",
                ValueType: "toggle",
                DefaultValue: "false"
            ));
            var archived = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "filter-archived",
                DisplayName: "Filter Archived",
                ValueType: "toggle",
                DefaultValue: "false"
            ));
            await _subscrio.Features.ArchiveFeatureAsync(archived.Key);

            var activeFeatures = await _subscrio.Features.ListFeaturesAsync(new FeatureFilterDto(Status: "active"));
            activeFeatures.Should().AllSatisfy(f => f.Status.Should().Be("active"));

            var archivedFeatures = await _subscrio.Features.ListFeaturesAsync(new FeatureFilterDto(Status: "archived"));
            archivedFeatures.Should().Contain(f => f.Key == archived.Key);
        }

        [Fact]
        public async Task FiltersFeaturesByValueType()
        {
            await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "filter-toggle",
                DisplayName: "Filter Toggle",
                ValueType: "toggle",
                DefaultValue: "false"
            ));
            await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "filter-numeric",
                DisplayName: "Filter Numeric",
                ValueType: "numeric",
                DefaultValue: "100"
            ));

            var toggleFeatures = await _subscrio.Features.ListFeaturesAsync(new FeatureFilterDto(ValueType: "toggle"));
            toggleFeatures.Should().AllSatisfy(f => f.ValueType.Should().Be("toggle"));
        }
    }

    public class FeatureProductAssociation : FeaturesTests
    {
        public FeatureProductAssociation(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task GetsFeaturesByProduct()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "product-for-features",
                DisplayName: "Product For Features"
            ));

            var feature1 = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "product-feature-1",
                DisplayName: "Product Feature 1",
                ValueType: "toggle",
                DefaultValue: "false"
            ));
            var feature2 = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "product-feature-2",
                DisplayName: "Product Feature 2",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature1.Key);
            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature2.Key);

            var features = await _subscrio.Features.GetFeaturesByProductAsync(product.Key);
            features.Should().HaveCount(2);
            features.Should().Contain(f => f.Key == feature1.Key);
            features.Should().Contain(f => f.Key == feature2.Key);
        }

        [Fact]
        public async Task ReturnsEmptyListForProductWithNoFeatures()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "product-no-features",
                DisplayName: "Product No Features"
            ));

            var features = await _subscrio.Features.GetFeaturesByProductAsync(product.Key);
            features.Should().BeEmpty();
        }
    }
}

