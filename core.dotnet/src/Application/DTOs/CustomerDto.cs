namespace Subscrio.Core.Application.DTOs;

public record CreateCustomerDto(
    string Key,
    string? DisplayName = null,
    string? Email = null,
    string? ExternalBillingId = null,
    Dictionary<string, object?>? Metadata = null
);

public record UpdateCustomerDto(
    string? DisplayName = null,
    string? Email = null,
    string? ExternalBillingId = null,
    Dictionary<string, object?>? Metadata = null
);

public record CustomerDto(
    string Key,
    string? DisplayName,
    string? Email,
    string? ExternalBillingId,
    string Status,
    Dictionary<string, object?>? Metadata,
    string CreatedAt,
    string UpdatedAt
);

public record CustomerFilterDto(
    string? Status = null,
    string? Search = null,
    string? SortBy = null,
    string? SortOrder = null,
    int Limit = 50,
    int Offset = 0
);


