using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    // Null nếu tài khoản đăng ký qua Google (AuthProvider = Google)
    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool EmailVerified { get; set; } = false;

    // Navigation properties
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
