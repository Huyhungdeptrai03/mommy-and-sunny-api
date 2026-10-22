using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Common.Payments.DTOs
{
    public class SePayCreatePaymentResult
    {
        public bool Success { get; set; }
        public string? PayUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
