using FluentValidation;
using MommyAndSunny.Application.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ")
                .MaximumLength(254);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MaximumLength(128);
        }
    }
}
