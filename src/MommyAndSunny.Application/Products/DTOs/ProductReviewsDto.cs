using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ProductReviewsDto
    {
        public ReviewSummaryDto Summary { get; set; } = new();
        public List<ReviewDto> Items { get; set; } = new();
    }
}
