namespace MommyAndSunny.Domain.Exceptions;

// Ném ra khi InventoryService phát hiện AvailableQuantity không đủ
// cho số lượng khách yêu cầu (dùng ở cả CartService và OrderService).
public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productName, int available, int requested)
        : base($"Sản phẩm '{productName}' chỉ còn {available} trong kho, không đủ để đặt {requested} sản phẩm")
    {
    }
}
