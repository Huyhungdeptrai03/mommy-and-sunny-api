using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Auth.DTOs
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }
}
