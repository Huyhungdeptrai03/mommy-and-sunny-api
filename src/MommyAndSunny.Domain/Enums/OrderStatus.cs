namespace MommyAndSunny.Domain.Enums;

// Vòng đời: Pending -> Confirmed -> Shipping -> Delivered
// Nhánh phụ: Pending/Confirmed -> Cancelled (không cho hủy từ Shipping trở đi)
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipping,
    Delivered,
    Cancelled
}
