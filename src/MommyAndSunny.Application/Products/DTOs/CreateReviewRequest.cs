using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class CreateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
