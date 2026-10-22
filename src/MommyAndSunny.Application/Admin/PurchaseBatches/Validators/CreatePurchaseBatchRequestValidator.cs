using FluentValidation;
using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;

namespace MommyAndSunny.Application.Admin.PurchaseBatches.Validators
{
    public class CreatePurchaseBatchRequestValidator : AbstractValidator<CreatePurchaseBatchRequest>
    {
        public CreatePurchaseBatchRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên đợt hàng không được để trống")
                .MaximumLength(255);

            RuleFor(x => x.CountryOfOrigin).MaximumLength(100);

            RuleFor(x => x.CloseDate).GreaterThan(x => x.OpenDate)
                .WithMessage("Ngày đóng phải sau ngày mở");
            RuleFor(x => x.EstimatedArrivalDate).GreaterThan(x => x.CloseDate)
                .When(x => x.EstimatedArrivalDate.HasValue)
                .WithMessage("Ngày dự kiến về hàng phải sau ngày đóng");

            // CloseDate > OpenDate đã được AdminPurchaseBatchService kiểm tra riêng — validator
            // này chỉ đảm bảo dữ liệu đầu vào cơ bản, tránh lặp thông báo lỗi
        }
    }
}
