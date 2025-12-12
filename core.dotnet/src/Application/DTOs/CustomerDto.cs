namespace Subscrio.Core.Application.DTOs;

public record CreateCustomerDto
{
    public required string Key { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? ExternalBillingId { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record UpdateCustomerDto
{
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? ExternalBillingId { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record CustomerDto
{
    public required string Key { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? ExternalBillingId { get; init; }
    public required string Status { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public record CustomerFilterDto
{
    public string? Status { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

