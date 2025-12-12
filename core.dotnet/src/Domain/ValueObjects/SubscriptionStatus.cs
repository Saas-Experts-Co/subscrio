namespace Subscrio.Core.Domain.ValueObjects;

public enum SubscriptionStatus
{
    Pending = 0,
    Active = 1,
    Trial = 2,
    Cancelled = 3,
    CancellationPending = 4,
    Expired = 5
}

