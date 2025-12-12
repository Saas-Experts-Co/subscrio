using FluentValidation;
using Subscrio.Core.Application.DTOs;

namespace Subscrio.Core.Application.Validators;

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Customer key is required")
            .MinimumLength(1).WithMessage("Customer key is required")
            .MaximumLength(255).WithMessage("Customer key too long");

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

