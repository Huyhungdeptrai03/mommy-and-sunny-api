using FluentValidation;
using MommyAndSunny.Application.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống")
                .MaximumLength(150);

           // RuleFor(x => x.PhoneNumber)
            //    .Matches(@"^0\d{9,10}$").WithMessage("Số điện thoại không hợp lệ")
            //    .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }
}
