using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class Policy : BaseEntity
{
    // Cố định 1 trong 4 giá trị: doi-tra, van-chuyen, bao-mat, dieu-khoan — không cho tạo/xoá
    // thêm policy mới, admin chỉ sửa Title/Content của 4 trang đã seed sẵn
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
