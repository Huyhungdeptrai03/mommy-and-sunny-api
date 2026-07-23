using MommyAndSunny.Application.Common.Interfaces;
using System.Security.Claims;

namespace MommyAndSunny.API.Services
{
    // Đặt ở API layer (không phải Infrastructure) vì phụ thuộc trực tiếp vào HttpContext —
    // đây là mối quan tâm của tầng web, không phải hạ tầng lưu trữ dữ liệu
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                // JwtBearer mặc định map claim "sub" sang ClaimTypes.NameIdentifier,
                // nên ưu tiên đọc theo NameIdentifier trước
                var userIdValue = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(userIdValue, out var userId) ? userId : null;
            }
        }
    }
}
