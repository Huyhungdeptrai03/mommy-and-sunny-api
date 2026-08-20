using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.PersonalImportRequests.DTOs;
using MommyAndSunny.Application.PersonalImportRequests.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/personal-import-requests")]
    public class PersonalImportRequestsController : ControllerBase
    {
        private readonly IPersonalImportRequestService _service;
        private readonly ICurrentUserService _currentUserService;

        public PersonalImportRequestsController(
            IPersonalImportRequestService service, ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        // Public — không yêu cầu đăng nhập, nhưng nếu request có kèm JWT hợp lệ
        // (khách đã đăng nhập gửi yêu cầu) thì vẫn gắn UserId để admin tra cứu lịch sử
        [HttpPost]
        public async Task<IActionResult> Create(CreatePersonalImportRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(_currentUserService.UserId, request, cancellationToken);
            return StatusCode(201, new
            {
                success = true,
                data = result,
                message = "Đã gửi yêu cầu, chúng tôi sẽ liên hệ với bạn sớm nhất"
            });
        }
    }
}
