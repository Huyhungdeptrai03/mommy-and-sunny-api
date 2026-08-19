using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.PublicStats.DTOs
{
    public class PublicStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalDeliveredOrders { get; set; }
        public int TotalCustomers { get; set; }
    }
}
