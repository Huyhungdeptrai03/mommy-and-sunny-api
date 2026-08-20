using FluentValidation;
using MommyAndSunny.Application.Admin.Categories.DTOs;

namespace MommyAndSunny.Application.Admin.Categories.Validators
{
    public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
    {
        public CreateCategoryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống")
                .MaximumLength(150);

            RuleFor(x => x.ImageUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("URL ảnh không hợp lệ")
                .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
        }
    }
}
