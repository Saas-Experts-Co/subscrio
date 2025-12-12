using FluentValidation;
using Subscrio.Core.Application.Constants;
using Subscrio.Core.Application.DTOs;

namespace Subscrio.Core.Application.Validators;

public class SubscriptionFilterDtoValidator : AbstractValidator<SubscriptionFilterDto>
{
    public SubscriptionFilterDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(x => x == null || x == "pending" || x == "active" || x == "trial" || x == "cancelled" || x == "cancellation_pending" || x == "expired")
            .WithMessage("Status must be 'pending', 'active', 'trial', 'cancelled', 'cancellation_pending', or 'expired'")
            .When(x => x.Status != null);

        RuleFor(x => x.SortBy)
            .Must(x => x == null || x == "activationDate" || x == "expirationDate" || x == "createdAt" || x == "updatedAt" || x == "currentPeriodStart" || x == "currentPeriodEnd")
            .WithMessage("SortBy must be 'activationDate', 'expirationDate', 'createdAt', 'updatedAt', 'currentPeriodStart', or 'currentPeriodEnd'")
            .When(x => x.SortBy != null);

        RuleFor(x => x.SortOrder)
            .Must(x => x == null || x == "asc" || x == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'")
            .When(x => x.SortOrder != null);

        RuleFor(x => x.Limit)
            .InclusiveBetween(ApplicationConstants.MinPageSize, ApplicationConstants.MaxPageSize)
            .When(x => x.Limit != default);

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Offset != default);
    }
}

