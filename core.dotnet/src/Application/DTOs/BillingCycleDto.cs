namespace Subscrio.Core.Application.DTOs;

public record CreateBillingCycleDto
{
    public required string PlanKey { get; init; }
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public int? DurationValue { get; init; }
    public required string DurationUnit { get; init; }
    public string? ExternalProductId { get; init; }
}

public record UpdateBillingCycleDto
{
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public int? DurationValue { get; init; }
    public string? DurationUnit { get; init; }
    public string? ExternalProductId { get; init; }
}

public record BillingCycleDto
{
    public string? ProductKey { get; init; }
    public string? PlanKey { get; init; }
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int? DurationValue { get; init; }
    public required string DurationUnit { get; init; }
    public string? ExternalProductId { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public record BillingCycleFilterDto
{
    public string? PlanKey { get; init; }
    public string? Status { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
    public string? DurationUnit { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

