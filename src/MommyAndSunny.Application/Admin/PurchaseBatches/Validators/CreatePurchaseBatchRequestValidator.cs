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

            // CloseDate > OpenDate đã được AdminPurchaseBatchService kiểm tra riêng — validator
            // này chỉ đảm bảo dữ liệu đầu vào cơ bản, tránh lặp thông báo lỗi
        }
    }
}
