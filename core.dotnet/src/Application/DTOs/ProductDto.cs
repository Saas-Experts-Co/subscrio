namespace Subscrio.Core.Application.DTOs;

public record CreateProductDto
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record UpdateProductDto
{
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record ProductDto
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public record ProductFilterDto
{
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
    public string? SortBy { get; init; }
    public string SortOrder { get; init; } = "asc";
}

