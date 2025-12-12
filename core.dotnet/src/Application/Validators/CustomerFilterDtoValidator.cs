using FluentValidation;
using Subscrio.Core.Application.Constants;
using Subscrio.Core.Application.DTOs;

namespace Subscrio.Core.Application.Validators;

public class CustomerFilterDtoValidator : AbstractValidator<CustomerFilterDto>
{
    public CustomerFilterDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(x => x == null || x == "active" || x == "suspended" || x == "archived" || x == "deleted")
            .WithMessage("Status must be 'active', 'suspended', 'archived', or 'deleted'")
            .When(x => x.Status != null);

        RuleFor(x => x.SortBy)
            .Must(x => x == null || x == "displayName" || x == "key" || x == "createdAt")
            .WithMessage("SortBy must be 'displayName', 'key', or 'createdAt'")
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

