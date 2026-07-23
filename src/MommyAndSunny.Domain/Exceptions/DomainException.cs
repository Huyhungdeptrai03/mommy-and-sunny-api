namespace MommyAndSunny.Domain.Exceptions;

// Exception gốc cho mọi lỗi nghiệp vụ (không phải lỗi hệ thống).
// Middleware ở API layer sẽ bắt exception này và trả về HTTP 400/409 phù hợp,
// thay vì để lộ stack trace kiểu lỗi 500.
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
