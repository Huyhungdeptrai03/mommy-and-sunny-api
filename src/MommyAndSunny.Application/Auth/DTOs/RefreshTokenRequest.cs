using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.DTOs
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
