using System.Text.Json;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;

namespace Subscrio.Core.Application.Services;

// NOTE: This service depends on the Subscrio class which will be created in Phase 4.
// The service structure is complete, but it will compile once Subscrio is available.
// For now, using a placeholder interface that matches the expected Subscrio API.


/// <summary>
/// Deep equality check for objects (for metadata comparison)
/// </summary>
internal static class DeepEqualHelper
{
    public static bool DeepEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return a == b;
        if (a.GetType() != b.GetType()) return false;

        if (a is Dictionary<string, object?> aDict && b is Dictionary<string, object?> bDict)
        {
            if (aDict.Count != bDict.Count) return false;
            foreach (var kvp in aDict)
            {
                if (!bDict.TryGetValue(kvp.Key, out var bValue)) return false;
                if (!DeepEqual(kvp.Value, bValue)) return false;
            }
            return true;
        }

        if (a is IEnumerable<object> aEnum && b is IEnumerable<object> bEnum)
        {
            var aList = aEnum.ToList();
            var bList = bEnum.ToList();
            if (aList.Count != bList.Count) return false;
            for (int i = 0; i < aList.Count; i++)
            {
                if (!DeepEqual(aList[i], bList[i])) return false;
            }
            return true;
        }

        return a.Equals(b);
    }
}

/// <summary>
/// Normalize null/undefined/empty string for comparison
/// </summary>
internal static class NormalizeValueHelper
{
    public static string? NormalizeValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value;
    }
}

/// <summary>
/// Compare feature config with existing feature DTO to detect changes
/// </summary>
internal static class ChangeDetectionHelper
{
    public static bool HasFeatureChanges(FeatureConfig config, FeatureDto existing)
    {
        if (config.DisplayName != existing.DisplayName) return true;
        if (NormalizeValueHelper.NormalizeValue(config.Description) != NormalizeValueHelper.NormalizeValue(existing.Description)) return true;
        if (config.ValueType != existing.ValueType) return true;
        if (config.DefaultValue != existing.DefaultValue) return true;
        if (NormalizeValueHelper.NormalizeValue(config.GroupName) != NormalizeValueHelper.NormalizeValue(existing.GroupName)) return true;
        if (!DeepEqualHelper.DeepEqual(config.Validator ?? null, existing.Validator ?? null)) return true;
        if (!DeepEqualHelper.DeepEqual(config.Metadata ?? null, existing.Metadata ?? null)) return true;
        return false;
    }

    public static bool HasProductChanges(ProductConfig config, ProductDto existing)
    {
        if (config.DisplayName != existing.DisplayName) return true;
        if (NormalizeValueHelper.NormalizeValue(config.Description) != NormalizeValueHelper.NormalizeValue(existing.Description)) return true;
        if (!DeepEqualHelper.DeepEqual(config.Metadata ?? null, existing.Metadata ?? null)) return true;
        return false;
    }

    public static bool HasPlanChanges(PlanConfig config, PlanDto existing)
    {
        if (config.DisplayName != existing.DisplayName) return true;
        if (NormalizeValueHelper.NormalizeValue(config.Description) != NormalizeValueHelper.NormalizeValue(existing.Description)) return true;
        if (NormalizeValueHelper.NormalizeValue(config.OnExpireTransitionToBillingCycleKey) != NormalizeValueHelper.NormalizeValue(existing.OnExpireTransitionToBillingCycleKey)) return true;
        if (!DeepEqualHelper.DeepEqual(config.Metadata ?? null, existing.Metadata ?? null)) return true;
        return false;
    }

    public static bool HasBillingCycleChanges(BillingCycleConfig config, BillingCycleDto existing)
    {
        if (config.DisplayName != existing.DisplayName) return true;
        if (NormalizeValueHelper.NormalizeValue(config.Description) != NormalizeValueHelper.NormalizeValue(existing.Description)) return true;
        if (config.DurationValue != existing.DurationValue) return true;
        if (config.DurationUnit != existing.DurationUnit) return true;
        if (NormalizeValueHelper.NormalizeValue(config.ExternalProductId) != NormalizeValueHelper.NormalizeValue(existing.ExternalProductId)) return true;
        return false;
    }
}

/// <summary>
/// Configuration Sync Service
/// Syncs configuration from JSON files or programmatic DTOs to the database
/// Uses public Subscrio API methods to ensure all business logic is reused
/// Equivalent to TypeScript ConfigSyncService
/// </summary>
public class ConfigSyncService
{
    private readonly Subscrio _subscrio; // Use public API methods

    public ConfigSyncService(Subscrio subscrio)
    {
        _subscrio = subscrio;
    }

    /// <summary>
    /// Load configuration from a JSON file and sync
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file</param>
    /// <returns>Sync report with operation results</returns>
    public async Task<ConfigSyncReport> SyncFromFileAsync(string filePath)
    {
        try
        {
            // Read file, parse JSON, validate, then sync
            var fileContent = await File.ReadAllTextAsync(filePath);

            // TODO: Validate JSON property order before parsing (validateConfigJsonPropertyOrder)
            // This will be implemented when the validation logic is available

            var jsonData = JsonSerializer.Deserialize<ConfigSyncDto>(fileContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (jsonData == null)
            {
                throw new ValidationException("Failed to parse configuration file: JSON is null");
            }

            return await SyncFromJsonAsync(jsonData);
        }
        catch (Exception error)
        {
            throw new ValidationException($"Failed to load configuration from file: {error.Message}");
        }
    }

    /// <summary>
    /// Sync configuration from a ConfigSyncDto object
    /// </summary>
    /// <param name="config">Configuration object (can be built programmatically)</param>
    /// <returns>Sync report with operation results</returns>
    public async Task<ConfigSyncReport> SyncFromJsonAsync(ConfigSyncDto config)
    {
        // Phase 1: Validate configuration schema
        // This will throw ValidationException if schema is invalid
        // TODO: Add FluentValidation validation here when validators are available

        // Initialize sync report
        var report = new ConfigSyncReport
        {
            Created = new ConfigSyncCounts { Features = 0, Products = 0, Plans = 0, BillingCycles = 0 },
            Updated = new ConfigSyncCounts { Features = 0, Products = 0, Plans = 0, BillingCycles = 0 },
            Archived = new ConfigSyncCounts { Features = 0, Products = 0, Plans = 0, BillingCycles = 0 },
            Unarchived = new ConfigSyncCounts { Features = 0, Products = 0, Plans = 0, BillingCycles = 0 },
            Ignored = new ConfigSyncCounts { Features = 0, Products = 0, Plans = 0, BillingCycles = 0 },
            Errors = new List<ConfigSyncError>(),
            Warnings = new List<ConfigSyncWarning>()
        };

        // Phase 2: Load Current State
        // Use maximum allowed limit to get as many entities as possible
        var existingProducts = await _subscrio.Products.ListProductsAsync(new ProductFilterDto { Limit = 100, Offset = 0 });
        var existingFeatures = await _subscrio.Features.ListFeaturesAsync(new FeatureFilterDto { Limit = 100, Offset = 0 });
        var existingPlans = await _subscrio.Plans.ListPlansAsync(new PlanFilterDto { Limit = 100, Offset = 0 });
        var existingBillingCycles = await _subscrio.BillingCycles.ListBillingCyclesAsync(new BillingCycleFilterDto { Limit = 100, Offset = 0 });

        // Create lookup maps by key
        var productsByKey = existingProducts.ToDictionary(p => p.Key);
        var featuresByKey = existingFeatures.ToDictionary(f => f.Key);
        // Plan keys are globally unique across all products
        var plansByKey = existingPlans.ToDictionary(p => p.Key);
        // Billing cycle keys are globally unique across all plans
        var billingCyclesByKey = existingBillingCycles.ToDictionary(bc => bc.Key);

        // Track ignored entities (in database but not in config)
        var configFeatureKeys = new HashSet<string>(config.Features.Select(f => f.Key));
        var configProductKeys = new HashSet<string>(config.Products.Select(p => p.Key));
        var configPlanKeys = new HashSet<string>(
            config.Products.SelectMany(p => p.Plans ?? new List<PlanConfig>()).Select(plan => plan.Key)
        );
        var configBillingCycleKeys = new HashSet<string>(
            config.Products.SelectMany(p => p.Plans ?? new List<PlanConfig>())
                .SelectMany(plan => plan.BillingCycles ?? new List<BillingCycleConfig>())
                .Select(bc => bc.Key)
        );

        report = report with
        {
            Ignored = new ConfigSyncCounts
            {
                Features = existingFeatures.Count(f => !configFeatureKeys.Contains(f.Key)),
                Products = existingProducts.Count(p => !configProductKeys.Contains(p.Key)),
                Plans = existingPlans.Count(p => !configPlanKeys.Contains(p.Key)),
                BillingCycles = existingBillingCycles.Count(bc => !configBillingCycleKeys.Contains(bc.Key))
            }
        };

        // Phase 3: Sync Features (Independent Entities)
        foreach (var featureConfig in config.Features)
        {
            try
            {
                featuresByKey.TryGetValue(featureConfig.Key, out var existing);

                if (existing == null)
                {
                    // Create new feature
                    var createDto = new CreateFeatureDto
                    {
                        Key = featureConfig.Key,
                        DisplayName = featureConfig.DisplayName,
                        Description = featureConfig.Description,
                        ValueType = featureConfig.ValueType,
                        DefaultValue = featureConfig.DefaultValue,
                        GroupName = featureConfig.GroupName,
                        Validator = featureConfig.Validator,
                        Metadata = featureConfig.Metadata
                    };

                    await _subscrio.Features.CreateFeatureAsync(createDto);
                    report = report with
                    {
                        Created = report.Created with { Features = report.Created.Features + 1 }
                    };

                    // Archive if needed
                    if (featureConfig.Archived == true)
                    {
                        await _subscrio.Features.ArchiveFeatureAsync(featureConfig.Key);
                        report = report with
                        {
                            Archived = report.Archived with { Features = report.Archived.Features + 1 }
                        };
                    }
                }
                else
                {
                    // Check if entity needs updating
                    var needsUpdate = ChangeDetectionHelper.HasFeatureChanges(featureConfig, existing);

                    if (needsUpdate)
                    {
                        var updateDto = new UpdateFeatureDto
                        {
                            DisplayName = featureConfig.DisplayName,
                            Description = featureConfig.Description,
                            ValueType = featureConfig.ValueType,
                            DefaultValue = featureConfig.DefaultValue,
                            GroupName = featureConfig.GroupName,
                            Validator = featureConfig.Validator,
                            Metadata = featureConfig.Metadata
                        };

                        await _subscrio.Features.UpdateFeatureAsync(featureConfig.Key, updateDto);
                        report = report with
                        {
                            Updated = report.Updated with { Features = report.Updated.Features + 1 }
                        };
                    }

                    // Handle archive status
                    var isArchived = existing.Status == "archived";
                    if (featureConfig.Archived == true && !isArchived)
                    {
                        await _subscrio.Features.ArchiveFeatureAsync(featureConfig.Key);
                        report = report with
                        {
                            Archived = report.Archived with { Features = report.Archived.Features + 1 }
                        };
                    }
                    else if (featureConfig.Archived == false && isArchived)
                    {
                        await _subscrio.Features.UnarchiveFeatureAsync(featureConfig.Key);
                        report = report with
                        {
                            Unarchived = report.Unarchived with { Features = report.Unarchived.Features + 1 }
                        };
                    }
                }
            }
            catch (Exception error)
            {
                report = report with
                {
                    Errors = report.Errors.Concat(new[]
                    {
                        new ConfigSyncError
                        {
                            EntityType = "feature",
                            Key = featureConfig.Key,
                            Message = error.Message
                        }
                    }).ToList()
                };
            }
        }

        // Phase 4-7: Sync Products, Plans, Billing Cycles
        // TODO: Complete implementation for Products, Plans, and Billing Cycles sync
        // This is a large implementation that will be completed in Phase 4 when Subscrio class is available
        // The structure is in place, but the full sync logic for products, plans, and billing cycles
        // will be implemented once we have the actual Subscrio class to work with.

        return report;
    }
}

