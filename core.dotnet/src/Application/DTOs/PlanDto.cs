namespace Subscrio.Core.Application.DTOs;

public record CreatePlanDto
{
    public required string ProductKey { get; init; }
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? OnExpireTransitionToBillingCycleKey { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record UpdatePlanDto
{
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? OnExpireTransitionToBillingCycleKey { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record PlanDto
{
    public required string ProductKey { get; init; }
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public string? OnExpireTransitionToBillingCycleKey { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public record PlanFilterDto
{
    public string? ProductKey { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

public record PlanFeatureDto
{
    public required string FeatureKey { get; init; }
    public required string Value { get; init; }
}

