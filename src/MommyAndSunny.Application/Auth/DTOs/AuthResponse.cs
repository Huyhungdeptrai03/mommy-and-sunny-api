using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.DTOs
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }
}
