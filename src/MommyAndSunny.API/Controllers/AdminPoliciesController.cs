using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Policies.DTOs;
using MommyAndSunny.Application.Admin.Policies.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/policies")]
    [Authorize(Roles = "Admin")]
    public class AdminPoliciesController : ControllerBase
    {
        private readonly IAdminPolicyService _adminPolicyService;

        public AdminPoliciesController(IAdminPolicyService adminPolicyService)
        {
            _adminPolicyService = adminPolicyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicies(CancellationToken cancellationToken)
        {
            var result = await _adminPolicyService.GetPoliciesAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPolicyById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminPolicyService.GetPolicyByIdAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePolicy(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminPolicyService.UpdatePolicyAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result, message = "Cập nhật chính sách thành công" });
        }
    }
}
