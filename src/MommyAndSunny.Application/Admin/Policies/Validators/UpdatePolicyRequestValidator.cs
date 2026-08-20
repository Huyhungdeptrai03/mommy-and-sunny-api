using FluentValidation;
using MommyAndSunny.Application.Admin.Policies.DTOs;

namespace MommyAndSunny.Application.Admin.Policies.Validators
{
    public class UpdatePolicyRequestValidator : AbstractValidator<UpdatePolicyRequest>
    {
        public UpdatePolicyRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống")
                .MaximumLength(255);

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung không được để trống");
        }
    }
}
