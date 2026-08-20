using FluentValidation;
using MommyAndSunny.Application.PersonalImportRequests.DTOs;

namespace MommyAndSunny.Application.PersonalImportRequests.Validators
{
    public class CreatePersonalImportRequestDtoValidator : AbstractValidator<CreatePersonalImportRequestDto>
    {
        public CreatePersonalImportRequestDtoValidator()
        {
            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Vui lòng nhập tên sản phẩm muốn đặt")
                .MaximumLength(255);

            RuleFor(x => x.ProductLink)
                .MaximumLength(1000)
                .Must(link => Uri.TryCreate(link, UriKind.Absolute, out _))
                .WithMessage("Link sản phẩm không hợp lệ")
                .When(x => !string.IsNullOrWhiteSpace(x.ProductLink));

            // Chặn cả 2 chiều: âm lẫn số lượng phi thực tế (đơn hàng cá nhân, không phải sỉ)
            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 999).WithMessage("Số lượng phải từ 1 đến 999");

            RuleFor(x => x.Notes).MaximumLength(2000);
            RuleFor(x => x.ImageUrl).MaximumLength(500);

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Vui lòng nhập họ tên liên hệ")
                .MaximumLength(150);

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage("Vui lòng nhập số điện thoại liên hệ")
                .Matches(@"^0\d{9,10}$").WithMessage("Số điện thoại không hợp lệ");

            RuleFor(x => x.CustomerEmail)
                .EmailAddress().WithMessage("Email không hợp lệ")
                .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
        }
    }
}
