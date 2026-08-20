using FluentValidation;
using MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs;

namespace MommyAndSunny.Application.Admin.PersonalImportRequests.Validators
{
    public class UpdatePersonalImportRequestStatusRequestValidator : AbstractValidator<UpdatePersonalImportRequestStatusRequest>
    {
        public UpdatePersonalImportRequestStatusRequestValidator()
        {
            RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái không hợp lệ");
            RuleFor(x => x.AdminNote).MaximumLength(2000);
        }
    }
}
