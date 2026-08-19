using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.PurchaseBatches.DTOs
{
    public class AdminPurchaseBatchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CountryOfOrigin { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime? EstimatedArrivalDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
