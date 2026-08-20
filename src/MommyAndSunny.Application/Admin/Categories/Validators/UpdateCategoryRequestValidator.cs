using FluentValidation;
using MommyAndSunny.Application.Admin.Categories.DTOs;

namespace MommyAndSunny.Application.Admin.Categories.Validators
{
    public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
    {
        public UpdateCategoryRequestValidator()
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
