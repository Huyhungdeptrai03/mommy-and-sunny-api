using MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs;
using MommyAndSunny.Application.Common.Models;

namespace MommyAndSunny.Application.Admin.PersonalImportRequests.Services
{
    public interface IAdminPersonalImportRequestService
    {
        Task<PagedResult<AdminPersonalImportRequestDto>> GetRequestsAsync(AdminPersonalImportRequestFilterRequest filter, CancellationToken cancellationToken = default);
        Task<AdminPersonalImportRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<AdminPersonalImportRequestDto> UpdateStatusAsync(Guid id, UpdatePersonalImportRequestStatusRequest request, CancellationToken cancellationToken = default);
    }
}
