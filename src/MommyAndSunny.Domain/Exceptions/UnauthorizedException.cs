using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Exceptions
{
    // Dùng khi sai thông tin đăng nhập. Middleware map sang HTTP 401 Unauthorized.
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
