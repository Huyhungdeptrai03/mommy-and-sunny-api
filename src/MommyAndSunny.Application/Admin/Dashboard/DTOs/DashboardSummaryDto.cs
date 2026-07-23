using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Dashboard.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int PendingOrdersCount { get; set; }
    }
}
