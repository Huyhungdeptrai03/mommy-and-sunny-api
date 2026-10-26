using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool EmailVerified { get; set; }
    }
}
