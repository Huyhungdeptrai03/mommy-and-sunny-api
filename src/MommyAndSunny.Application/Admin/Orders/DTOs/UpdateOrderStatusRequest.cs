using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Orders.DTOs
{
    public class UpdateOrderStatusRequest
    {
        public OrderStatus ToStatus { get; set; }
        public string? Note { get; set; }
    }
}
