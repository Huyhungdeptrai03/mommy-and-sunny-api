using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.PurchaseBatches.DTOs
{
    public class CreatePurchaseBatchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? CountryOfOrigin { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime? EstimatedArrivalDate { get; set; }
    }
}
