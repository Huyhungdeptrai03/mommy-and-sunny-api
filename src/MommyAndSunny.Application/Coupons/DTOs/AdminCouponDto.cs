using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.DTOs
{
    public class AdminCouponDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? TotalUsageLimit { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
    }
    
}
