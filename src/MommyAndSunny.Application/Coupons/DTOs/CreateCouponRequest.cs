using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.DTOs
{
    public class CreateCouponRequest
    {
        public string Code { get; set; } = string.Empty;

        // "Percentage" hoặc "FixedAmount"
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? TotalUsageLimit { get; set; }
    }
}
