using FluentValidation;
using MommyAndSunny.Application.Articles.DTOs;

namespace MommyAndSunny.Application.Articles.Validators
{
    public class CreateArticleCommentRequestValidator : AbstractValidator<CreateArticleCommentRequest>
    {
        public CreateArticleCommentRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung bình luận không được để trống")
                // Khớp HasMaxLength(2000) ở ArticleCommentConfiguration — chặn sớm ở validator
                // thay vì để query ghi xuống DB mới báo lỗi cột quá dài
                .MaximumLength(2000).WithMessage("Bình luận tối đa 2000 ký tự");
        }
    }
}
