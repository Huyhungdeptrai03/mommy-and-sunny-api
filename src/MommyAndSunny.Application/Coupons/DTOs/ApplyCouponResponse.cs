using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.DTOs
{
    public class ApplyCouponResponse
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
