using FluentValidation;
using MommyAndSunny.Application.Admin.Products.DTOs;

namespace MommyAndSunny.Application.Admin.Products.Validators
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Vui lòng chọn danh mục");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
                .MaximumLength(255);

            RuleFor(x => x.Brand).MaximumLength(150);

            // Min<=Max đã được AdminProductService kiểm tra riêng — ở đây chỉ chặn giá trị âm
            RuleFor(x => x.AgeRangeMinMonths).GreaterThanOrEqualTo(0)
                .WithMessage("Độ tuổi tối thiểu không được âm")
                .When(x => x.AgeRangeMinMonths.HasValue);

            RuleFor(x => x.AgeRangeMaxMonths).GreaterThanOrEqualTo(0)
                .WithMessage("Độ tuổi tối đa không được âm")
                .When(x => x.AgeRangeMaxMonths.HasValue);

            RuleFor(x => x.AgeRangeMaxMonths).GreaterThanOrEqualTo(x => x.AgeRangeMinMonths)
                .When(x => x.AgeRangeMinMonths.HasValue && x.AgeRangeMaxMonths.HasValue)
                .WithMessage("Độ tuổi tối đa phải lớn hơn hoặc bằng độ tuổi tối thiểu");
        }
    }
}
