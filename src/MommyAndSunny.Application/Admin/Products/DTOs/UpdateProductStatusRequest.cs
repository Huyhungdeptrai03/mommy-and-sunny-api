using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class UpdateProductStatusRequest
    {
        public ProductStatus Status { get; set; }
    }
}
