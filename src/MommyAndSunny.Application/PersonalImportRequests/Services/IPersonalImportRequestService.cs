using MommyAndSunny.Application.PersonalImportRequests.DTOs;

namespace MommyAndSunny.Application.PersonalImportRequests.Services
{
    public interface IPersonalImportRequestService
    {
        Task<PersonalImportRequestDto> CreateAsync(Guid? userId, CreatePersonalImportRequestDto request, CancellationToken cancellationToken = default);
    }
}
