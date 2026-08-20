using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs
{
    public class UpdatePersonalImportRequestStatusRequest
    {
        public PersonalImportRequestStatus Status { get; set; }
        public string? AdminNote { get; set; }
    }
}
