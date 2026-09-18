using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Security
{
    public sealed class PasswordResetTokenService
    {
        private readonly byte[] _signingKey;

        public PasswordResetTokenService(IConfiguration configuration)
        {
            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT signing key was not found.");

            _signingKey = SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    $"ParaVolleyPasswordReset|{jwtKey}"));
        }

        public string CreateToken(
            AppUser user,
            TimeSpan lifetime)
        {
            var expiresAt = DateTimeOffset.UtcNow
                .Add(lifetime)
                .ToUnixTimeSeconds();

            var payload = $"{user.Id}:{expiresAt}";
            var signature = Sign(
                payload,
                user.PasswordHash);

            var encodedPayload = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(payload));

            var encodedSignature =
                WebEncoders.Base64UrlEncode(signature);

            return $"{encodedPayload}.{encodedSignature}";
        }

        public bool TryGetUserId(
            string? token,
            out int userId)
        {
            userId = 0;

            return TryReadToken(
                token,
                out _,
                out _,
                out userId,
                out _);
        }

        public bool IsValid(
            string? token,
            AppUser user)
        {
            if (!TryReadToken(
                    token,
                    out var payload,
                    out var suppliedSignature,
                    out var userId,
                    out _))
            {
                return false;
            }

            if (userId != user.Id)
            {
                return false;
            }

            var expectedSignature = Sign(
                payload,
                user.PasswordHash);

            return CryptographicOperations.FixedTimeEquals(
                suppliedSignature,
                expectedSignature);
        }

        private bool TryReadToken(
            string? token,
            out string payload,
            out byte[] signature,
            out int userId,
            out long expiresAt)
        {
            payload = string.Empty;
            signature = Array.Empty<byte>();
            userId = 0;
            expiresAt = 0;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var parts = token.Split('.', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
                payload = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(parts[0]));

                signature =
                    WebEncoders.Base64UrlDecode(parts[1]);
            }
            catch
            {
                return false;
            }

            var payloadParts = payload.Split(':', 2);

            if (payloadParts.Length != 2 ||
                !int.TryParse(payloadParts[0], out userId) ||
                !long.TryParse(payloadParts[1], out expiresAt))
            {
                return false;
            }

            return expiresAt >
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private byte[] Sign(
            string payload,
            string passwordHash)
        {
            var passwordFingerprint = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(passwordHash)));

            using var hmac = new HMACSHA256(_signingKey);

            return hmac.ComputeHash(
                Encoding.UTF8.GetBytes(
                    $"{payload}|{passwordFingerprint}"));
        }
    }
}
