namespace Subscrio.Core.Application.DTOs;

public record BillingCycleConfig
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public int? DurationValue { get; init; }
    public required string DurationUnit { get; init; }
    public string? ExternalProductId { get; init; }
    public bool? Archived { get; init; }
}

public record PlanConfig
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? OnExpireTransitionToBillingCycleKey { get; init; }
    public Dictionary<string, string>? FeatureValues { get; init; }
    public List<BillingCycleConfig>? BillingCycles { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public bool? Archived { get; init; }
}

public record FeatureConfig
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string ValueType { get; init; }
    public required string DefaultValue { get; init; }
    public string? GroupName { get; init; }
    public Dictionary<string, object?>? Validator { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public bool? Archived { get; init; }
}

public record ProductConfig
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public bool? Archived { get; init; }
    public List<string>? Features { get; init; }
    public List<PlanConfig>? Plans { get; init; }
}

public record ConfigSyncDto
{
    public required string Version { get; init; }
    public required List<FeatureConfig> Features { get; init; }
    public required List<ProductConfig> Products { get; init; }
}

public record ConfigSyncReport
{
    public required ConfigSyncCounts Created { get; init; }
    public required ConfigSyncCounts Updated { get; init; }
    public required ConfigSyncCounts Archived { get; init; }
    public required ConfigSyncCounts Unarchived { get; init; }
    public required ConfigSyncCounts Ignored { get; init; }
    public required List<ConfigSyncError> Errors { get; init; }
    public required List<ConfigSyncWarning> Warnings { get; init; }
}

public record ConfigSyncCounts
{
    public required int Features { get; init; }
    public required int Products { get; init; }
    public required int Plans { get; init; }
    public required int BillingCycles { get; init; }
}

public record ConfigSyncError
{
    public required string EntityType { get; init; }
    public required string Key { get; init; }
    public required string Message { get; init; }
}

public record ConfigSyncWarning
{
    public required string EntityType { get; init; }
    public required string Key { get; init; }
    public required string Message { get; init; }
}

public record FeatureUsageSummaryDto
{
    public required int ActiveSubscriptions { get; init; }
    public required List<string> EnabledFeatures { get; init; }
    public required List<string> DisabledFeatures { get; init; }
    public required Dictionary<string, int> NumericFeatures { get; init; }
    public required Dictionary<string, string> TextFeatures { get; init; }
}

