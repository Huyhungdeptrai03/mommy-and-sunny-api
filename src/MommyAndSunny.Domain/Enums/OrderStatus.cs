namespace MommyAndSunny.Domain.Enums;

// Vòng đời: Pending -> Confirmed -> Shipping -> Delivered
// Nhánh phụ: Pending/Confirmed -> Cancelled (không cho hủy từ Shipping trở đi)
public enum OrderStatus
{
    Pending,                // Chờ xác nhận
    Confirmed,              // Đã xác nhận

    // 3 trạng thái mới — CHỈ áp dụng cho đơn có sản phẩm thuộc đợt hàng xách tay,
    // đơn hàng thường (không dính đợt hàng) bỏ qua, đi thẳng Confirmed -> Shipping
    Purchasing,             // Đang mua hàng ở nước ngoài
    InternationalShipping,  // Đang vận chuyển quốc tế về Việt Nam
    ArrivedAtWarehouse,     // Đã về kho Việt Nam, chờ thanh toán nốt + giao nội địa

    Shipping,               // Đang giao nội địa (chặng cuối, dùng chung cho cả 2 loại đơn)
    Delivered,              // Đã giao
    Cancelled               // Đã hủy
}

