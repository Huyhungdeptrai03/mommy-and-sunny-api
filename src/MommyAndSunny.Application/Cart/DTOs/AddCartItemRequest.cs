using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Cart.DTOs
{
    public class AddCartItemRequest
    {
        public Guid ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
