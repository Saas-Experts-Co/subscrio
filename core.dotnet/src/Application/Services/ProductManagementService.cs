using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Application.Validators;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Application.Services;

public class ProductManagementService
{
    private readonly IProductRepository _productRepository;
    private readonly IFeatureRepository _featureRepository;

    public ProductManagementService(
        IProductRepository productRepository,
        IFeatureRepository featureRepository
    )
    {
        _productRepository = productRepository;
        _featureRepository = featureRepository;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        // Validate input
        var validator = new CreateProductDtoValidator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid product data for key '{dto.Key}': {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Check for duplicate key
        var existing = await _productRepository.FindByKeyAsync(dto.Key);
        if (existing != null)
        {
            throw new ConflictException(
                $"Product with key '{dto.Key}' already exists"
            );
        }

        // Create domain entity (no ID - database will generate)
        var product = new Product(
            new ProductProps
            {
                Key = dto.Key,
                DisplayName = dto.DisplayName,
                Description = dto.Description,
                Status = ProductStatus.Active,
                Metadata = dto.Metadata,
                CreatedAt = DateHelper.Now(),
                UpdatedAt = DateHelper.Now()
            },
            null
        );

        // Save and get entity with generated ID
        var savedProduct = await _productRepository.SaveAsync(product);

        return ProductMapper.ToDto(savedProduct);
    }

    public async Task<ProductDto> UpdateProductAsync(string key, UpdateProductDto dto)
    {
        // Validate input
        var validator = new UpdateProductDtoValidator();
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid update data", validationResult.Errors);
        }

        // Find existing by key
        var product = await _productRepository.FindByKeyAsync(key);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{key}' not found. Please check the product key and try again.");
        }

        // Key is immutable - no validation needed

        // Update properties
        if (dto.DisplayName != null)
        {
            product.UpdateDisplayName(dto.DisplayName);
        }
        if (dto.Description != null)
        {
            product.Props.Description = dto.Description;
        }
        if (dto.Metadata != null)
        {
            product.Props.Metadata = dto.Metadata;
        }

        product.Props.UpdatedAt = DateHelper.Now();

        // Save
        var savedProduct = await _productRepository.SaveAsync(product);

        return ProductMapper.ToDto(savedProduct);
    }

    public async Task<ProductDto?> GetProductAsync(string key)
    {
        var product = await _productRepository.FindByKeyAsync(key);
        return product != null ? ProductMapper.ToDto(product) : null;
    }

    public async Task<List<ProductDto>> ListProductsAsync(ProductFilterDto? filters = null)
    {
        var validator = new ProductFilterDtoValidator();
        var validationResult = await validator.ValidateAsync(filters ?? new ProductFilterDto());
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid filter parameters", validationResult.Errors);
        }

        var products = await _productRepository.FindAllAsync(filters ?? new ProductFilterDto());
        return products.Select(ProductMapper.ToDto).ToList();
    }

    public async Task DeleteProductAsync(string key)
    {
        var product = await _productRepository.FindByKeyAsync(key);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{key}' not found");
        }

        if (!product.CanDelete())
        {
            throw new DomainException(
                $"Cannot delete product with status '{product.Status}'. Product must be archived before deletion."
            );
        }

        // Check for plans before deletion
        var hasPlans = await _productRepository.HasPlansAsync(product.Key);
        if (hasPlans)
        {
            throw new DomainException(
                $"Cannot delete product '{product.Key}'. Product has associated plans. Please delete or archive all plans first."
            );
        }

        // Product from repository always has ID (BIGSERIAL PRIMARY KEY)
        await _productRepository.DeleteAsync(product.Id!.Value);
    }

    public async Task<ProductDto> ArchiveProductAsync(string key)
    {
        var product = await _productRepository.FindByKeyAsync(key);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{key}' not found");
        }

        product.Archive();
        var savedProduct = await _productRepository.SaveAsync(product);

        return ProductMapper.ToDto(savedProduct);
    }

    public async Task<ProductDto> UnarchiveProductAsync(string key)
    {
        var product = await _productRepository.FindByKeyAsync(key);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{key}' not found");
        }

        product.Unarchive();
        var savedProduct = await _productRepository.SaveAsync(product);

        return ProductMapper.ToDto(savedProduct);
    }

    public async Task AssociateFeatureAsync(string productKey, string featureKey)
    {
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{productKey}' not found");
        }

        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{featureKey}' not found");
        }

        // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
        await _productRepository.AssociateFeatureAsync(product.Id!.Value, feature.Id!.Value);
    }

    public async Task DissociateFeatureAsync(string productKey, string featureKey)
    {
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{productKey}' not found");
        }

        var feature = await _featureRepository.FindByKeyAsync(featureKey);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{featureKey}' not found");
        }

        // Entities from repository always have IDs (BIGSERIAL PRIMARY KEY)
        await _productRepository.DissociateFeatureAsync(product.Id!.Value, feature.Id!.Value);
    }
}

