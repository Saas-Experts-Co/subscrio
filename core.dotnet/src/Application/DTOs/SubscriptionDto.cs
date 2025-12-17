namespace Subscrio.Core.Application.DTOs;

public record CreateSubscriptionDto(
    string Key,
    string CustomerKey,
    string BillingCycleKey,
    DateTime? ActivationDate = null,
    DateTime? ExpirationDate = null,
    DateTime? CancellationDate = null,
    DateTime? TrialEndDate = null,
    DateTime? CurrentPeriodStart = null,
    DateTime? CurrentPeriodEnd = null,
    string? StripeSubscriptionId = null,
    Dictionary<string, object>? Metadata = null
);

public record UpdateSubscriptionDto(
    string? BillingCycleKey = null,
    DateTime? ExpirationDate = null,
    DateTime? CancellationDate = null,
    DateTime? TrialEndDate = null,
    DateTime? CurrentPeriodStart = null,
    DateTime? CurrentPeriodEnd = null,
    string? StripeSubscriptionId = null,
    Dictionary<string, object>? Metadata = null
);

public record SubscriptionDto(
    string Key,
    string CustomerKey,
    string ProductKey,
    string PlanKey,
    string BillingCycleKey,
    string Status,
    bool IsArchived,
    string? ActivationDate,
    string? ExpirationDate,
    string? CancellationDate,
    string? TrialEndDate,
    string? CurrentPeriodStart,
    string? CurrentPeriodEnd,
    string? StripeSubscriptionId,
    Dictionary<string, object>? Metadata,
    CustomerDto? Customer,
    string CreatedAt,
    string UpdatedAt
);

public record SubscriptionFilterDto(
    string? CustomerKey = null,
    string? ProductKey = null,
    string? PlanKey = null,
    string? Status = null,
    bool? IsArchived = null,
    string? SortBy = null,
    string? SortOrder = null,
    int? Limit = null,
    int? Offset = null
);

public record DetailedSubscriptionFilterDto(
    string? CustomerKey = null,
    string? ProductKey = null,
    string? PlanKey = null,
    string? BillingCycleKey = null,
    string? Status = null,
    bool? IsArchived = null,
    DateTime? ActivationDateFrom = null,
    DateTime? ActivationDateTo = null,
    DateTime? ExpirationDateFrom = null,
    DateTime? ExpirationDateTo = null,
    DateTime? TrialEndDateFrom = null,
    DateTime? TrialEndDateTo = null,
    DateTime? CurrentPeriodStartFrom = null,
    DateTime? CurrentPeriodStartTo = null,
    DateTime? CurrentPeriodEndFrom = null,
    DateTime? CurrentPeriodEndTo = null,
    bool? HasStripeId = null,
    bool? HasTrial = null,
    bool? HasFeatureOverrides = null,
    string? FeatureKey = null,
    string? MetadataKey = null,
    object? MetadataValue = null,
    string? SortBy = null,
    string? SortOrder = null,
    int? Limit = null,
    int? Offset = null
);

public record FeatureOverrideDto(
    long FeatureId,
    string Value,
    string Type,
    string CreatedAt
);

public record TransitionExpiredSubscriptionsReport(
    int Processed,
    int Transitioned,
    int Archived,
    List<TransitionError> Errors
);

public record TransitionError(
    string SubscriptionKey,
    string Error
);


