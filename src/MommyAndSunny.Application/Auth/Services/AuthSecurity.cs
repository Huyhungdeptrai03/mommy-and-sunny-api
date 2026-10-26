using System.Security.Cryptography;
using System.Text;
namespace MommyAndSunny.Application.Auth.Services;

public static class AuthSecurity
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    public static string RandomToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static bool ValidPassword(string password) => password.Length >= 8 && Encoding.UTF8.GetByteCount(password) <= 72;
}
