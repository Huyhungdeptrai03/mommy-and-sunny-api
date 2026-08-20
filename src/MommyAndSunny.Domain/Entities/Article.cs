using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class Article : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? AuthorName { get; set; }
    public ArticleCategory Category { get; set; }

    // Draft -> Published (không xóa cứng, giữ nguyên bản nháp phòng khi cần sửa lại)
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    // Chỉ có giá trị khi lần đầu chuyển sang Published — dùng để sắp xếp trang công khai
    // và hiển thị ngày đăng, không đổi lại mỗi lần Published->Draft->Published nữa
    public DateTime? PublishedAt { get; set; }
}
