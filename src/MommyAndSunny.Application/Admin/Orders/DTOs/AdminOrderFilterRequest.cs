using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Orders.DTOs
{
    public class AdminOrderFilterRequest
    {
        public OrderStatus? Status { get; set; }
        public string? Search { get; set; } // Tìm theo OrderCode, email, hoặc tên người nhận
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
