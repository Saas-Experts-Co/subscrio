namespace Subscrio.Core.Application.DTOs;

public record CreateStripeSubscriptionParams
{
    public required string CustomerKey { get; init; }
    public required string PlanKey { get; init; }
    public required string BillingCycleKey { get; init; }
    public required string StripePriceId { get; init; }
}

public record CreateCheckoutSessionParams
{
    public required string CustomerKey { get; init; }
    public required string BillingCycleKey { get; init; }
    public string? SubscriptionKey { get; init; }
    public string? StripeSecretKey { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
    public int? Quantity { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public bool? AllowPromotionCodes { get; init; }
    public string? BillingAddressCollection { get; init; }
    public List<string>? PaymentMethodTypes { get; init; }
    public int? TrialPeriodDays { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public Dictionary<string, object?>? StripeOptions { get; init; }
}

