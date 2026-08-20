using FluentValidation;
using MommyAndSunny.Application.Admin.Articles.DTOs;

namespace MommyAndSunny.Application.Admin.Articles.Validators
{
    public class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequest>
    {
        public UpdateArticleRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống")
                .MaximumLength(255);

            RuleFor(x => x.Excerpt).MaximumLength(500);

            RuleFor(x => x.Content).NotEmpty().WithMessage("Nội dung không được để trống");

            RuleFor(x => x.CoverImageUrl).MaximumLength(500);
            RuleFor(x => x.AuthorName).MaximumLength(150);

            RuleFor(x => x.Category).IsInEnum().WithMessage("Chuyên mục không hợp lệ");
        }
    }
}
