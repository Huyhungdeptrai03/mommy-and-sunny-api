using FluentValidation;
using MommyAndSunny.Application.Admin.Products.DTOs;

namespace MommyAndSunny.Application.Admin.Products.Validators
{
    public class CreateProductVariantRequestValidator : AbstractValidator<CreateProductVariantRequest>
    {
        public CreateProductVariantRequestValidator()
        {
            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU không được để trống")
                .MaximumLength(100);

            RuleFor(x => x.Size).MaximumLength(50);
            RuleFor(x => x.Color).MaximumLength(50);

            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Giá bán phải lớn hơn 0");
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Tồn kho không được âm");

            // CompareAtPrice là giá gốc gạch ngang khi Flash Sale — phải cao hơn giá bán thực tế
            // thì mới có ý nghĩa hiển thị giảm giá, thấp hơn/bằng là dữ liệu vô lý
            RuleFor(x => x.CompareAtPrice)
                .GreaterThan(x => x.Price).WithMessage("Giá gốc (trước giảm) phải lớn hơn giá bán")
                .When(x => x.CompareAtPrice.HasValue);
        }
    }
}
