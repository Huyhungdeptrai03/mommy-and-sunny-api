using FluentValidation;
using MommyAndSunny.Application.Admin.Products.DTOs;

namespace MommyAndSunny.Application.Admin.Products.Validators
{
    public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
                .MaximumLength(255);

            RuleFor(x => x.Brand).MaximumLength(150);

            RuleFor(x => x.AgeRangeMinMonths).GreaterThanOrEqualTo(0)
                .WithMessage("Độ tuổi tối thiểu không được âm")
                .When(x => x.AgeRangeMinMonths.HasValue);

            RuleFor(x => x.AgeRangeMaxMonths).GreaterThanOrEqualTo(0)
                .WithMessage("Độ tuổi tối đa không được âm")
                .When(x => x.AgeRangeMaxMonths.HasValue);
        }
    }
}
