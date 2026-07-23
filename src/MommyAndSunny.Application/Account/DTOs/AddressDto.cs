using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Account.DTOs
{
    public class AddressDto
    {
        public Guid Id { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string DetailAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}

