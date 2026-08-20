namespace MommyAndSunny.Domain.Enums
{
    public enum PersonalImportRequestStatus
    {
        New,        // Khách vừa gửi, chưa xử lý
        Contacted,  // Đã liên hệ khách để trao đổi chi tiết
        Quoted,     // Đã báo giá, chờ khách xác nhận
        Completed,  // Đã chốt và xử lý xong (thường tạo đơn hàng thủ công ngoài luồng)
        Cancelled   // Khách hủy hoặc không phản hồi
    }
}
