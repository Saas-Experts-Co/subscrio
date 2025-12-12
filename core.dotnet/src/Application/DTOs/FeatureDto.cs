namespace Subscrio.Core.Application.DTOs;

public record CreateFeatureDto
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string ValueType { get; init; }
    public required string DefaultValue { get; init; }
    public string? GroupName { get; init; }
    public Dictionary<string, object?>? Validator { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record UpdateFeatureDto
{
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? ValueType { get; init; }
    public string? DefaultValue { get; init; }
    public string? GroupName { get; init; }
    public Dictionary<string, object?>? Validator { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record FeatureDto
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string ValueType { get; init; }
    public required string DefaultValue { get; init; }
    public string? GroupName { get; init; }
    public required string Status { get; init; }
    public Dictionary<string, object?>? Validator { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public record FeatureFilterDto
{
    public string? Status { get; init; }
    public string? ValueType { get; init; }
    public string? GroupName { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

