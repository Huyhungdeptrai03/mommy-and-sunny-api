using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.DTOs
{
    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
