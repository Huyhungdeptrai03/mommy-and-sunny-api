using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class User : BaseEntity
{
    public string NormalizedEmail { get; set; } = string.Empty;
    public Guid SecurityVersion { get; set; } = Guid.NewGuid();
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;

    // Null nếu tài khoản đăng ký qua Google (AuthProvider = Google)
    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool EmailVerified { get; set; } = false;

    // Dùng cho luồng xác thực email — lưu HASH của token (không lưu token thô),
    // cùng nguyên tắc bảo mật đã áp dụng với RefreshToken
    public string? EmailVerificationTokenHash { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    // Dùng cho luồng quên mật khẩu — hiệu lực ngắn hơn (1 giờ) vì nhạy cảm hơn
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Navigation properties
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
