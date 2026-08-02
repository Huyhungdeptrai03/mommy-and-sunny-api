using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.DTOs
{
    public class CreateOrderResponse
    {
        public string OrderCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;

        // Chỉ có giá trị khi PaymentMethod = Momo — FE cần redirect người dùng tới URL này
        public string? PaymentUrl { get; set; }
    }

}
