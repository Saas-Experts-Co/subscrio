namespace Subscrio.Core.Application.DTOs;

public record CreateSubscriptionDto
{
    public required string Key { get; init; }
    public required string CustomerKey { get; init; }
    public required string BillingCycleKey { get; init; }
    public DateTime? ActivationDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public DateTime? CancellationDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public DateTime? CurrentPeriodStart { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record UpdateSubscriptionDto
{
    public string? BillingCycleKey { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public DateTime? CancellationDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public DateTime? CurrentPeriodStart { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public record SubscriptionDto
{
    public required string Key { get; init; }
    public required string CustomerKey { get; init; }
    public required string ProductKey { get; init; }
    public required string PlanKey { get; init; }
    public required string BillingCycleKey { get; init; }
    public required string Status { get; init; }
    public required bool IsArchived { get; init; }
    public string? ActivationDate { get; init; }
    public string? ExpirationDate { get; init; }
    public string? CancellationDate { get; init; }
    public string? TrialEndDate { get; init; }
    public string? CurrentPeriodStart { get; init; }
    public string? CurrentPeriodEnd { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public CustomerDto? Customer { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public record SubscriptionFilterDto
{
    public string? CustomerKey { get; init; }
    public string? ProductKey { get; init; }
    public string? PlanKey { get; init; }
    public string? Status { get; init; }
    public bool? IsArchived { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

public record DetailedSubscriptionFilterDto
{
    public string? CustomerKey { get; init; }
    public string? ProductKey { get; init; }
    public string? PlanKey { get; init; }
    public string? BillingCycleKey { get; init; }
    public string? Status { get; init; }
    public bool? IsArchived { get; init; }
    public DateTime? ActivationDateFrom { get; init; }
    public DateTime? ActivationDateTo { get; init; }
    public DateTime? ExpirationDateFrom { get; init; }
    public DateTime? ExpirationDateTo { get; init; }
    public DateTime? TrialEndDateFrom { get; init; }
    public DateTime? TrialEndDateTo { get; init; }
    public DateTime? CurrentPeriodStartFrom { get; init; }
    public DateTime? CurrentPeriodStartTo { get; init; }
    public DateTime? CurrentPeriodEndFrom { get; init; }
    public DateTime? CurrentPeriodEndTo { get; init; }
    public bool? HasStripeId { get; init; }
    public bool? HasTrial { get; init; }
    public bool? HasFeatureOverrides { get; init; }
    public string? FeatureKey { get; init; }
    public string? MetadataKey { get; init; }
    public object? MetadataValue { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

public record TransitionExpiredSubscriptionsReport
{
    public required int Processed { get; init; }
    public required int Transitioned { get; init; }
    public required int Archived { get; init; }
    public required List<TransitionError> Errors { get; init; }
}

public record TransitionError
{
    public required string SubscriptionKey { get; init; }
    public required string Error { get; init; }
}

