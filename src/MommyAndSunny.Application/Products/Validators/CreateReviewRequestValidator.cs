using FluentValidation;
using MommyAndSunny.Application.Products.DTOs;

namespace MommyAndSunny.Application.Products.Validators
{
    public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
    {
        public CreateReviewRequestValidator()
        {
            // Rating 1-5 đã được ReviewService kiểm tra riêng (kèm thông báo nghiệp vụ cụ thể) —
            // chỉ bổ sung phần còn thiếu: giới hạn độ dài Comment
            RuleFor(x => x.Comment).MaximumLength(2000).WithMessage("Đánh giá tối đa 2000 ký tự");
        }
    }
}
