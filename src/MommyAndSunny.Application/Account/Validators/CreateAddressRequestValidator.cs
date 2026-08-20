using FluentValidation;
using MommyAndSunny.Application.Account.DTOs;

namespace MommyAndSunny.Application.Account.Validators
{
    public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
    {
        public CreateAddressRequestValidator()
        {
            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Tên người nhận không được để trống")
                .MaximumLength(150);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^0\d{9,10}$").WithMessage("Số điện thoại không hợp lệ");

            RuleFor(x => x.Province).NotEmpty().WithMessage("Vui lòng chọn Tỉnh/Thành phố");
            RuleFor(x => x.Ward).NotEmpty().WithMessage("Vui lòng chọn Phường/Xã");
            // KHÔNG bắt buộc District — hệ hành chính Việt Nam đã chuyển sang 2 cấp
            // (Tỉnh/Thành + Xã/Phường) từ 1/7/2025, không còn Quận/Huyện. FE (AddressSelector.tsx)
            // không còn thu thập trường này nữa, luôn gửi chuỗi rỗng — giữ field trên DTO/Entity
            // để không phá dữ liệu địa chỉ cũ, nhưng không validate NotEmpty ở đây.

            RuleFor(x => x.DetailAddress)
                .NotEmpty().WithMessage("Địa chỉ chi tiết không được để trống")
                .MaximumLength(500);
        }
    }
}
