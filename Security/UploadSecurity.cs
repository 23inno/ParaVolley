using System.Text;

namespace SportsManagementMVC.Security;

public static class UploadSecurity
{
    public const long ImageMaxBytes = 2 * 1024 * 1024;
    public const long ReportMaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

    private static readonly HashSet<string> ReportExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".xlsx", ".csv",
            ".jpg", ".jpeg", ".png", ".webp"
        };

    public static async Task<bool> IsSafeImageAsync(
        IFormFile file,
        long maxBytes = ImageMaxBytes)
    {
        var extension = Path.GetExtension(file.FileName);
        if (file.Length <= 0 || file.Length > maxBytes ||
            !ImageExtensions.Contains(extension))
        {
            return false;
        }

        var header = await ReadHeaderAsync(file, 12);
        return MatchesImageSignature(extension, header);
    }

    public static async Task<bool> IsSafeReportAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (file.Length <= 0 || file.Length > ReportMaxBytes ||
            !ReportExtensions.Contains(extension))
        {
            return false;
        }

        var header = await ReadHeaderAsync(file, 12);
        if (ImageExtensions.Contains(extension))
        {
            return MatchesImageSignature(extension, header);
        }

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return header.AsSpan().StartsWith("%PDF-"u8);
        }

        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return header.Length >= 4 &&
                   header[0] == (byte)'P' && header[1] == (byte)'K' &&
                   header[2] is 3 or 5 or 7 &&
                   header[3] is 4 or 6 or 8;
        }

        return !header.Contains((byte)0) &&
               Encoding.UTF8.GetString(header).Length > 0;
    }

    public static string SafeOriginalFileName(string fileName)
    {
        var value = Path.GetFileName(fileName).Trim();
        return value.Length <= 200 ? value : value[..200];
    }

    public static string GeneratedFileName(IFormFile file) =>
        $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";

    private static bool MatchesImageSignature(
        string extension,
        byte[] header)
    {
        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return header.Length >= 3 &&
                   header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        }

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            return header.AsSpan().StartsWith(signature);
        }

        return header.Length >= 12 &&
               header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
               header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
    }

    private static async Task<byte[]> ReadHeaderAsync(
        IFormFile file,
        int bytesToRead)
    {
        var buffer = new byte[Math.Min(bytesToRead, (int)file.Length)];
        await using var stream = file.OpenReadStream();
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(totalRead, buffer.Length - totalRead));
            if (read == 0) break;
            totalRead += read;
        }

        return totalRead == buffer.Length ? buffer : buffer[..totalRead];
    }
}
