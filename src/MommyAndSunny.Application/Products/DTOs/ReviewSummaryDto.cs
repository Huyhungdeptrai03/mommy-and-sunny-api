using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ReviewSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalCount { get; set; }

        // Key: số sao (1-5), Value: số lượng đánh giá có số sao đó
        public Dictionary<int, int> RatingBreakdown { get; set; } = new();
    }
}
