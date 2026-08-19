using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Entities
{
    public class Coupon : BaseEntity
    {
        // Luôn lưu dạng CHỮ HOA, không dấu cách — chuẩn hóa lúc tạo để tránh trùng lặp
        // kiểu "SALE10" và "sale10" là 2 mã khác nhau trong database nhưng cùng ý nghĩa
        public string Code { get; set; } = string.Empty;

        public CouponDiscountType DiscountType { get; set; }

        // Percentage: 0-100 (%). FixedAmount: số tiền VNĐ trực tiếp
        public decimal DiscountValue { get; set; }

        // Chỉ có ý nghĩa khi DiscountType = Percentage — chặn giảm giá % vô tội vạ trên
        // đơn hàng giá trị quá lớn (VD: giảm 20% nhưng tối đa 100.000đ)
        public decimal? MaxDiscountAmount { get; set; }

        // Giá trị đơn hàng tối thiểu (tính trên phần ĐỦ ĐIỀU KIỆN áp mã — xem CategoryId)
        // mới được dùng mã này
        public decimal? MinOrderValue { get; set; }

        // NULL = áp dụng cho toàn bộ đơn hàng. Có giá trị = chỉ áp dụng giảm giá cho
        // các sản phẩm thuộc đúng danh mục này trong đơn (phần còn lại tính giá đầy đủ)
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // NULL = không giới hạn tổng số lần dùng
        public int? TotalUsageLimit { get; set; }
        public int UsageCount { get; set; } = 0;

        // Admin có thể tắt mã ngay lập tức mà không cần xóa hẳn (giữ lịch sử đơn đã dùng)
        public bool IsActive { get; set; } = true;
    }
}
