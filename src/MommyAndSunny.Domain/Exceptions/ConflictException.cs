using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Exceptions
{
    // Dùng khi có xung đột dữ liệu, VD: email đã tồn tại lúc đăng ký.
    // Middleware sẽ map exception này sang HTTP 409 Conflict.
    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
