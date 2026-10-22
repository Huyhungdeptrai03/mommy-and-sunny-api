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

        // SePay QR image URL: display as an image and poll order payment status.
        public string? PaymentUrl { get; set; }
    }

}
