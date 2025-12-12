using FluentValidation;
using Subscrio.Core.Application.Constants;
using Subscrio.Core.Application.DTOs;

namespace Subscrio.Core.Application.Validators;

public class BillingCycleFilterDtoValidator : AbstractValidator<BillingCycleFilterDto>
{
    public BillingCycleFilterDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(x => x == null || x == "active" || x == "archived")
            .WithMessage("Status must be 'active' or 'archived'")
            .When(x => x.Status != null);

        RuleFor(x => x.Limit)
            .InclusiveBetween(ApplicationConstants.MinPageSize, ApplicationConstants.MaxPageSize)
            .When(x => x.Limit != default);

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Offset != default);

        RuleFor(x => x.DurationUnit)
            .Must(x => x == null || x == "days" || x == "weeks" || x == "months" || x == "years" || x == "forever")
            .WithMessage("DurationUnit must be 'days', 'weeks', 'months', 'years', or 'forever'")
            .When(x => x.DurationUnit != null);

        RuleFor(x => x.SortBy)
            .Must(x => x == null || x == "displayName" || x == "createdAt")
            .WithMessage("SortBy must be 'displayName' or 'createdAt'")
            .When(x => x.SortBy != null);

        RuleFor(x => x.SortOrder)
            .Must(x => x == null || x == "asc" || x == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'")
            .When(x => x.SortOrder != null);
    }
}

