using System.Security.Cryptography;
using System.Text;

namespace SportsManagementMVC.Data
{
    // Simplified SHA-256 password hashing for this demo/assignment app.
    // A production app should use ASP.NET Core Identity's PasswordHasher<T>
    // (which salts and uses PBKDF2) instead of this.
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        public static bool Verify(string password, string storedHash)
        {
            return Hash(password).Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
