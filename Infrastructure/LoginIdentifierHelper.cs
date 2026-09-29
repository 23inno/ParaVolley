using System.Text.RegularExpressions;

namespace SportsManagementMVC.Infrastructure;

public static class LoginIdentifierHelper
{
    private const string PlaceholderDomain =
        "phone.paravolley.local";

    public static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(
            phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("00") &&
            digits.Length > 10)
        {
            digits = digits[2..];
        }

        if (digits.Length == 10 &&
            digits.StartsWith('0'))
        {
            digits = "27" + digits[1..];
        }

        if (digits.Length < 10 ||
            digits.Length > 15)
        {
            return null;
        }

        return digits;
    }

    public static string BuildStorageEmail(
        string? email,
        string accountType,
        string normalizedPhone)
    {
        var normalizedEmail =
            NormalizeEmail(email);

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return normalizedEmail;
        }

        var safeAccountType = Regex.Replace(
            accountType.ToLowerInvariant(),
            "[^a-z0-9]+",
            string.Empty);

        return $"{safeAccountType}-{normalizedPhone}@{PlaceholderDomain}";
    }

    public static bool IsPlaceholderEmail(
        string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            email.EndsWith(
                "@" + PlaceholderDomain,
                StringComparison.OrdinalIgnoreCase);
    }

    public static string DisplayPhone(
        string? phone)
    {
        var normalized = NormalizePhone(phone);

        if (normalized is { Length: 11 } &&
            normalized.StartsWith("27"))
        {
            return $"+27 {normalized.Substring(2, 2)} " +
                   $"{normalized.Substring(4, 3)} " +
                   $"{normalized.Substring(7, 4)}";
        }

        return string.IsNullOrWhiteSpace(phone)
            ? "Not provided"
            : phone.Trim();
    }

    public static string BuildWhatsAppUrl(
        string normalizedPhone,
        string message)
    {
        return "https://wa.me/" +
            Uri.EscapeDataString(normalizedPhone) +
            "?text=" +
            Uri.EscapeDataString(message);
    }

    public static string BuildWhatsAppDesktopUrl(
        string normalizedPhone,
        string message)
    {
        return "whatsapp://send?phone=" +
            Uri.EscapeDataString(normalizedPhone) +
            "&text=" +
            Uri.EscapeDataString(message);
    }

    public static string BuildWhatsAppWebUrl(
        string normalizedPhone,
        string message)
    {
        return "https://web.whatsapp.com/send?phone=" +
            Uri.EscapeDataString(normalizedPhone) +
            "&text=" +
            Uri.EscapeDataString(message);
    }
}
