using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.PurchaseBatches.DTOs
{
    public class PublicPurchaseBatchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CountryOfOrigin { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime? EstimatedArrivalDate { get; set; }
        public int ProductCount { get; set; }
    }
}
