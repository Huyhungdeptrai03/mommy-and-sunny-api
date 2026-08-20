using MommyAndSunny.Application.Admin.Policies.DTOs;

namespace MommyAndSunny.Application.Admin.Policies.Services
{
    public interface IAdminPolicyService
    {
        Task<List<AdminPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken = default);
        Task<AdminPolicyDto> GetPolicyByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<AdminPolicyDto> UpdatePolicyAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken = default);
    }
}
