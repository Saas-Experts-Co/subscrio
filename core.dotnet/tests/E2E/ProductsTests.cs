using FluentAssertions;
using Subscrio.Core;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Tests.Setup;
using Xunit;

namespace Subscrio.Core.Tests.E2E;

[Collection("Database")]
public class ProductsTests : IClassFixture<TestDatabaseFixture>
{
    private readonly Subscrio _subscrio;
    private readonly TestFixtures _fixtures;

    public ProductsTests(TestDatabaseFixture fixture)
    {
        _subscrio = fixture.Subscrio;
        _fixtures = new TestFixtures(_subscrio);
    }

    public class ProductCreation : ProductsTests
    {
        public ProductCreation(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task CreatesProductWithValidData()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "test-product",
                DisplayName: "Test Product",
                Description: "A test product"
            ));

            product.Should().NotBeNull();
            product.Key.Should().Be("test-product");
            product.DisplayName.Should().Be("Test Product");
            product.Status.Should().Be("active");
            product.CreatedAt.Should().NotBeNullOrEmpty();
            product.UpdatedAt.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ThrowsErrorForDuplicateProductKey()
        {
            await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "duplicate-key",
                DisplayName: "Product 1"
            ));

            await Assert.ThrowsAsync<ConflictException>(async () =>
            {
                await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                    Key: "duplicate-key",
                    DisplayName: "Product 2"
                ));
            });
        }

        [Fact]
        public async Task ValidatesProductKeyFormat()
        {
            await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                    Key: "INVALID KEY!",
                    DisplayName: "Invalid Product"
                ));
            });
        }
    }

    public class ProductRetrieval : ProductsTests
    {
        public ProductRetrieval(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task RetrievesProductByKey()
        {
            var created = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "retrieve-test",
                DisplayName: "Retrieve Test"
            ));

            var retrieved = await _subscrio.Products.GetProductAsync(created.Key);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be(created.Key);
            retrieved.Key.Should().Be("retrieve-test");
        }

        [Fact]
        public async Task ReturnsNullForNonExistentProduct()
        {
            var result = await _subscrio.Products.GetProductAsync("non-existent-key");
            result.Should().BeNull();
        }
    }

    public class ProductUpdate : ProductsTests
    {
        public ProductUpdate(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task UpdatesProductDisplayName()
        {
            var created = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "update-test",
                DisplayName: "Original Name"
            ));

            var updated = await _subscrio.Products.UpdateProductAsync(created.Key, new UpdateProductDto(
                DisplayName: "Updated Name"
            ));

            updated.DisplayName.Should().Be("Updated Name");
            updated.Key.Should().Be("update-test"); // Key unchanged
        }

        [Fact]
        public async Task ThrowsErrorWhenUpdatingNonExistentProduct()
        {
            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _subscrio.Products.UpdateProductAsync("non-existent-key", new UpdateProductDto(
                    DisplayName: "New Name"
                ));
            });
        }
    }

    public class ProductLifecycle : ProductsTests
    {
        public ProductLifecycle(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ArchivesAndActivatesProduct()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "lifecycle-test",
                DisplayName: "Lifecycle Test"
            ));

            await _subscrio.Products.ArchiveProductAsync(product.Key);
            var archived = await _subscrio.Products.GetProductAsync(product.Key);
            archived!.Status.Should().Be("archived");

            await _subscrio.Products.UnarchiveProductAsync(product.Key);
            var unarchived = await _subscrio.Products.GetProductAsync(product.Key);
            unarchived!.Status.Should().Be("active");
        }

        [Fact]
        public async Task CompleteProductLifecycle()
        {
            // Create
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "lifecycle-complete",
                DisplayName: "Lifecycle Complete"
            ));
            product.Status.Should().Be("active");

            // Update
            var updated = await _subscrio.Products.UpdateProductAsync(product.Key, new UpdateProductDto(
                DisplayName: "Updated Name"
            ));
            updated.DisplayName.Should().Be("Updated Name");

            // Note: DeactivateProductAsync doesn't exist - products are archived/unarchived

            // Archive
            await _subscrio.Products.ArchiveProductAsync(product.Key);
            var archived = await _subscrio.Products.GetProductAsync(product.Key);
            archived!.Status.Should().Be("archived");

            // Delete (only allowed when archived)
            await _subscrio.Products.DeleteProductAsync(product.Key);
            var deleted = await _subscrio.Products.GetProductAsync(product.Key);
            deleted.Should().BeNull();
        }
    }

    public class FeatureAssociation : ProductsTests
    {
        public FeatureAssociation(TestDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task AssociatesAndDissociatesFeatures()
        {
            var product = await _subscrio.Products.CreateProductAsync(new CreateProductDto(
                Key: "feature-test",
                DisplayName: "Feature Test"
            ));

            var feature = await _subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "test-feature",
                DisplayName: "Test Feature",
                ValueType: "toggle",
                DefaultValue: "false"
            ));

            // Associate
            await _subscrio.Products.AssociateFeatureAsync(product.Key, feature.Key);
            var features = await _subscrio.Features.GetFeaturesByProductAsync(product.Key);
            features.Should().HaveCount(1);
            features[0].Key.Should().Be(feature.Key);

            // Dissociate
            await _subscrio.Products.DissociateFeatureAsync(product.Key, feature.Key);
            var afterDissociate = await _subscrio.Features.GetFeaturesByProductAsync(product.Key);
            afterDissociate.Should().BeEmpty();
        }
    }
}

