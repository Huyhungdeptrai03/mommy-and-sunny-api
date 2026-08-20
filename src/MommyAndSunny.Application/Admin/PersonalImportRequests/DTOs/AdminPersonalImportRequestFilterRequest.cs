using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs
{
    public class AdminPersonalImportRequestFilterRequest
    {
        public PersonalImportRequestStatus? Status { get; set; }
        public string? Search { get; set; } // Tìm theo tên sản phẩm, tên khách hoặc SĐT
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
