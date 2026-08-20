using FluentValidation;
using MommyAndSunny.Application.Admin.Products.DTOs;

namespace MommyAndSunny.Application.Admin.Products.Validators
{
    public class CreateProductSpecificationRequestValidator : AbstractValidator<CreateProductSpecificationRequest>
    {
        public CreateProductSpecificationRequestValidator()
        {
            RuleFor(x => x.Label)
                .NotEmpty().WithMessage("Tên thông số không được để trống")
                .MaximumLength(150);

            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Giá trị thông số không được để trống")
                .MaximumLength(500);

            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        }
    }
}
