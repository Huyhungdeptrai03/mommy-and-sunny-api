using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Enums
{
    public enum PurchaseBatchStatus
    {
        Open,           // Đang mở nhận đặt hàng
        Closed,         // Đã chốt đơn, chưa đi mua
        Purchasing,     // Đang mua hàng ở nước ngoài
        Shipping,       // Đang vận chuyển quốc tế về Việt Nam
        Arrived,        // Đã về kho Việt Nam
        Completed,      // Toàn bộ đơn trong đợt đã giao xong
        Cancelled       // Hủy đợt (hoàn cọc toàn bộ)
    }

}
