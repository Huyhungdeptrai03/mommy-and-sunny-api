using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.DTOs
{
    public class OrderFilterRequest
    {
        public OrderStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
