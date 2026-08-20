using FluentValidation;
using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;

namespace MommyAndSunny.Application.Admin.PurchaseBatches.Validators
{
    public class UpdatePurchaseBatchRequestValidator : AbstractValidator<UpdatePurchaseBatchRequest>
    {
        public UpdatePurchaseBatchRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên đợt hàng không được để trống")
                .MaximumLength(255);

            RuleFor(x => x.CountryOfOrigin).MaximumLength(100);
        }
    }
}
