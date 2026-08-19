using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class UpdateProductRequest
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public int? AgeRangeMinMonths { get; set; }
        public int? AgeRangeMaxMonths { get; set; }

        public Guid? PurchaseBatchId { get; set; }
    }
}
