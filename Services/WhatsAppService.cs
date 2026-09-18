using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SportsManagementMVC.Services;

public sealed class WhatsAppService
{
    private static readonly HttpClient HttpClient = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IConfiguration configuration,
        ILogger<WhatsAppService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool Configured =>
        !string.IsNullOrWhiteSpace(
            _configuration["WhatsApp:AccessToken"]) &&
        !string.IsNullOrWhiteSpace(
            _configuration["WhatsApp:PhoneNumberId"]) &&
        !string.IsNullOrWhiteSpace(
            _configuration["WhatsApp:ApprovalTemplateName"]);

    public async Task<NotificationDeliveryResult> SendPlayerApprovalAsync(
        string phoneNumber,
        string playerName,
        string setupUrl,
        CancellationToken cancellationToken = default)
    {
        if (!Configured)
        {
            return new(
                false,
                "WhatsApp approval delivery is not configured.");
        }

        var to = NormalizePhoneNumber(phoneNumber);

        if (string.IsNullOrWhiteSpace(to))
        {
            return new(
                false,
                "The player's phone number could not be formatted for WhatsApp.");
        }

        var accessToken =
            _configuration["WhatsApp:AccessToken"]!.Trim();

        var phoneNumberId =
            _configuration["WhatsApp:PhoneNumberId"]!.Trim();

        var templateName =
            _configuration["WhatsApp:ApprovalTemplateName"]!.Trim();

        var languageCode =
            _configuration["WhatsApp:LanguageCode"]?.Trim();

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            languageCode = "en_US";
        }

        var graphApiVersion =
            _configuration["WhatsApp:GraphApiVersion"]?.Trim();

        if (string.IsNullOrWhiteSpace(graphApiVersion))
        {
            graphApiVersion = "v23.0";
        }

        var endpoint =
            $"https://graph.facebook.com/{graphApiVersion}/" +
            $"{Uri.EscapeDataString(phoneNumberId)}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new
                {
                    code = languageCode
                },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = playerName
                            },
                            new
                            {
                                type = "text",
                                text = setupUrl
                            }
                        }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await HttpClient.SendAsync(
                request,
                cancellationToken);

            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new(
                    true,
                    "WhatsApp approval message sent.");
            }

            var providerMessage =
                GetProviderMessage(responseBody);

            _logger.LogWarning(
                "WhatsApp Cloud API rejected approval message with HTTP {StatusCode}: {ProviderMessage}",
                (int)response.StatusCode,
                providerMessage);

            return new(
                false,
                string.IsNullOrWhiteSpace(providerMessage)
                    ? $"WhatsApp rejected the message with HTTP {(int)response.StatusCode}."
                    : $"WhatsApp rejected the message: {providerMessage}");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "WhatsApp approval delivery failed.");

            return new(
                false,
                "Could not connect to WhatsApp.");
        }
    }

    private static string? NormalizePhoneNumber(
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var digits = new string(
            phoneNumber
                .Where(char.IsDigit)
                .ToArray());

        if (digits.Length == 10 &&
            digits.StartsWith("0"))
        {
            return "27" + digits[1..];
        }

        return digits.Length >= 10
            ? digits
            : null;
    }

    private static string? GetProviderMessage(
        string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document =
                JsonDocument.Parse(responseBody);

            var root = document.RootElement;

            if (root.TryGetProperty(
                    "error",
                    out var error) &&
                error.ValueKind ==
                    JsonValueKind.Object &&
                error.TryGetProperty(
                    "message",
                    out var message) &&
                message.ValueKind ==
                    JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Return a short plain-text response below.
        }

        var compact = responseBody.Trim();

        return compact.Length <= 240
            ? compact
            : compact[..240] + "...";
    }
}
