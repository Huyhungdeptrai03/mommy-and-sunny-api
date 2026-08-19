using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Entities
{
    public class PurchaseBatch : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? CountryOfOrigin { get; set; }

        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime? EstimatedArrivalDate { get; set; }

        public PurchaseBatchStatus Status { get; set; } = PurchaseBatchStatus.Open;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

}
