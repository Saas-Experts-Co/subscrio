using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Application.Utils;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Application.Services;

public class FeatureManagementService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IProductRepository _productRepository;
    private readonly IValidator<CreateFeatureDto> _createFeatureValidator;
    private readonly IValidator<UpdateFeatureDto> _updateFeatureValidator;
    private readonly IValidator<FeatureFilterDto> _featureFilterValidator;

    public FeatureManagementService(
        IFeatureRepository featureRepository,
        IProductRepository productRepository,
        IValidator<CreateFeatureDto> createFeatureValidator,
        IValidator<UpdateFeatureDto> updateFeatureValidator,
        IValidator<FeatureFilterDto> featureFilterValidator)
    {
        _featureRepository = featureRepository;
        _productRepository = productRepository;
        _createFeatureValidator = createFeatureValidator;
        _updateFeatureValidator = updateFeatureValidator;
        _featureFilterValidator = featureFilterValidator;
    }

    public async Task<FeatureDto> CreateFeatureAsync(CreateFeatureDto dto)
    {
        // Validate input
        var validationResult = await _createFeatureValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid feature data for key '{dto.Key}': {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Check if key already exists
        var existing = await _featureRepository.FindByKeyAsync(dto.Key);
        if (existing != null)
        {
            throw new ConflictException($"Feature with key '{dto.Key}' already exists");
        }

        // Validate default value based on type
        FeatureValueValidator.Validate(dto.DefaultValue, dto.ValueType);

        // Create domain entity (no ID - database will generate)
        var feature = new Feature(new FeatureProps(
            dto.Key,
            dto.DisplayName,
            dto.Description,
            dto.ValueType,
            dto.DefaultValue,
            dto.GroupName,
            FeatureStatus.Active,
            dto.Validator,
            dto.Metadata,
            DateHelper.Now(),
            DateHelper.Now()
        ));

        // Save and get entity with generated ID
        var savedFeature = await _featureRepository.SaveAsync(feature);
        return FeatureMapper.ToDto(savedFeature);
    }

    public async Task<FeatureDto> UpdateFeatureAsync(string key, UpdateFeatureDto dto)
    {
        // Validate input
        var validationResult = await _updateFeatureValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid update data", validationResult.Errors);
        }

        var feature = await _featureRepository.FindByKeyAsync(key);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{key}' not found");
        }

        // Key is immutable - no validation needed

        // Update properties
        if (dto.DisplayName != null)
        {
            feature.UpdateDisplayName(dto.DisplayName);
        }
        if (dto.Description != null)
        {
            feature.Props = feature.Props with { Description = dto.Description };
        }
        if (dto.DefaultValue != null)
        {
            FeatureValueValidator.Validate(dto.DefaultValue, feature.Props.ValueType);
            feature.Props = feature.Props with { DefaultValue = dto.DefaultValue };
        }
        if (dto.GroupName != null)
        {
            feature.Props = feature.Props with { GroupName = dto.GroupName };
        }
        if (dto.Validator != null)
        {
            feature.Props = feature.Props with { Validator = dto.Validator };
        }
        if (dto.Metadata != null)
        {
            feature.Props = feature.Props with { Metadata = dto.Metadata };
        }

        feature.Props = feature.Props with { UpdatedAt = DateHelper.Now() };
        var savedFeature = await _featureRepository.SaveAsync(feature);
        return FeatureMapper.ToDto(savedFeature);
    }

    public async Task<FeatureDto?> GetFeatureAsync(string key)
    {
        var feature = await _featureRepository.FindByKeyAsync(key);
        return feature != null ? FeatureMapper.ToDto(feature) : null;
    }

    public async Task<IReadOnlyList<FeatureDto>> ListFeaturesAsync(FeatureFilterDto? filters = null)
    {
        filters ??= new FeatureFilterDto();
        var validationResult = await _featureFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid filter parameters", validationResult.Errors);
        }

        var features = await _featureRepository.FindAllAsync(validationResult.Data);
        return features.Select(FeatureMapper.ToDto).ToList();
    }

    public async Task ArchiveFeatureAsync(string key)
    {
        var feature = await _featureRepository.FindByKeyAsync(key);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{key}' not found");
        }

        feature.Archive();
        await _featureRepository.SaveAsync(feature);
    }

    public async Task UnarchiveFeatureAsync(string key)
    {
        var feature = await _featureRepository.FindByKeyAsync(key);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{key}' not found");
        }

        feature.Unarchive();
        await _featureRepository.SaveAsync(feature);
    }

    public async Task DeleteFeatureAsync(string key)
    {
        var feature = await _featureRepository.FindByKeyAsync(key);
        if (feature == null)
        {
            throw new NotFoundException($"Feature with key '{key}' not found");
        }

        if (!feature.CanDelete())
        {
            throw new DomainException(
                $"Cannot delete feature with status '{feature.Status}'. " +
                "Feature must be archived before deletion."
            );
        }

        // Feature from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!feature.Id.HasValue)
        {
            throw new DomainException($"Feature '{feature.Key}' does not have an ID and cannot be deleted.");
        }

        // Check for product associations
        var hasProductAssociations = await _featureRepository.HasProductAssociationsAsync(feature.Id.Value);
        if (hasProductAssociations)
        {
            throw new DomainException(
                $"Cannot delete feature '{feature.Key}'. Feature is associated with products. Please dissociate from all products first."
            );
        }

        // Check for plan feature values
        var hasPlanFeatureValues = await _featureRepository.HasPlanFeatureValuesAsync(feature.Id.Value);
        if (hasPlanFeatureValues)
        {
            throw new DomainException(
                $"Cannot delete feature '{feature.Key}'. Feature is used in plan feature values. Please remove from all plans first."
            );
        }

        // Check for subscription overrides
        var hasSubscriptionOverrides = await _featureRepository.HasSubscriptionOverridesAsync(feature.Id.Value);
        if (hasSubscriptionOverrides)
        {
            throw new DomainException(
                $"Cannot delete feature '{feature.Key}'. Feature has subscription overrides. Please remove all subscription overrides first."
            );
        }

        await _featureRepository.DeleteAsync(feature.Id.Value);
    }

    public async Task<IReadOnlyList<FeatureDto>> GetFeaturesByProductAsync(string productKey)
    {
        // Verify product exists
        var product = await _productRepository.FindByKeyAsync(productKey);
        if (product == null)
        {
            throw new NotFoundException($"Product with key '{productKey}' not found");
        }

        // Product from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!product.Id.HasValue)
        {
            throw new DomainException($"Product '{product.Key}' does not have an ID.");
        }

        var features = await _featureRepository.FindByProductAsync(product.Id.Value);
        return features.Select(FeatureMapper.ToDto).ToList();
    }
}

