using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Users.DTOs
{
    public class AdminUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public int OrderCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
