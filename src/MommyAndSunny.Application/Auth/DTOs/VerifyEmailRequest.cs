using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.DTOs
{
    public class VerifyEmailRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
