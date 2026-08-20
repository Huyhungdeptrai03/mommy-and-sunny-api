using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs;
using MommyAndSunny.Application.Admin.PersonalImportRequests.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/personal-import-requests")]
    [Authorize(Roles = "Admin")]
    public class AdminPersonalImportRequestsController : ControllerBase
    {
        private readonly IAdminPersonalImportRequestService _service;

        public AdminPersonalImportRequestsController(IAdminPersonalImportRequestService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] AdminPersonalImportRequestFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _service.GetRequestsAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdatePersonalImportRequestStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateStatusAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result, message = "Cập nhật trạng thái thành công" });
        }
    }
}
