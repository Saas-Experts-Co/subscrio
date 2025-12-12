using FluentValidation;
using Subscrio.Core.Application.DTOs;

namespace Subscrio.Core.Application.Validators;

public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(255)
            .When(x => x.DisplayName != null);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.ExternalBillingId)
            .MaximumLength(255)
            .When(x => x.ExternalBillingId != null);
    }
}

