using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Users.DTOs;
using MommyAndSunny.Application.Admin.Users.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _service;
        private readonly ICurrentUserService _currentUserService;

        public AdminUsersController(IAdminUserService service, ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _service.GetUsersAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
        {
            var currentAdminId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
            var result = await _service.UpdateRoleAsync(id, currentAdminId, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
