using MommyAndSunny.Application.Policies.DTOs;

namespace MommyAndSunny.Application.Policies.Services
{
    public interface IPolicyService
    {
        Task<PolicyDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    }
}
