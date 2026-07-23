using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ProductFilterRequest
    {
        public string? CategorySlug { get; set; }
        public int? AgeMonths { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Search { get; set; }
        public ProductSortBy SortBy { get; set; } = ProductSortBy.Newest;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
