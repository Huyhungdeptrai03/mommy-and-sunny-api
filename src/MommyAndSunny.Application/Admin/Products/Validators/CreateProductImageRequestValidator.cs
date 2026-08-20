using FluentValidation;
using MommyAndSunny.Application.Admin.Products.DTOs;

namespace MommyAndSunny.Application.Admin.Products.Validators
{
    public class CreateProductImageRequestValidator : AbstractValidator<CreateProductImageRequest>
    {
        public CreateProductImageRequestValidator()
        {
            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("URL ảnh không được để trống")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("URL ảnh không hợp lệ");

            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        }
    }
}
