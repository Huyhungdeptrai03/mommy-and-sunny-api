using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class PersonalImportRequest : BaseEntity
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLink { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    // Null nếu khách gửi yêu cầu khi chưa đăng nhập — form này cố ý KHÔNG bắt buộc
    // đăng nhập vì mục đích chính là thu thập lead, không phải tạo đơn hàng ngay
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public PersonalImportRequestStatus Status { get; set; } = PersonalImportRequestStatus.New;

    // Admin ghi chú nội bộ trong quá trình xử lý (báo giá, trao đổi...) — khác với
    // Notes của khách, không hiển thị lại cho khách
    public string? AdminNote { get; set; }
}
