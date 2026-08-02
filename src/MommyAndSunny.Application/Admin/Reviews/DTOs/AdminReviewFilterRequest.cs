using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Reviews.DTOs
{
    public class AdminReviewFilterRequest
    {
        // Lọc theo số sao cụ thể (1-5), null nghĩa là xem tất cả
        public int? Rating { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
