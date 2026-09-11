using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services;

public sealed record NotificationDeliveryResult(
    bool Success,
    string Message);

public sealed class NotificationDeliveryService
{
    private static readonly HttpClient HttpClient = new();

    private static readonly NotificationPreference[] DefaultPreferences =
    {
        new()
        {
            EventKey = "new_player",
            EventLabel = "New Player Registration",
            EventDescription = "When a new player registration is received",
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = false
        },
        new()
        {
            EventKey = "announcement",
            EventLabel = "Announcements",
            EventDescription = "When a new announcement is published to players",
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = true
        },
        new()
        {
            EventKey = "event_reminders",
            EventLabel = "Event Reminders",
            EventDescription = "Important reminders and updates for scheduled events",
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = true
        },
        new()
        {
            EventKey = "match_results",
            EventLabel = "Match Results",
            EventDescription = "When match results are recorded or published",
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = true
        },
        new()
        {
            EventKey = "low_attendance",
            EventLabel = "Low Attendance Alert",
            EventDescription = "When attendance falls below the configured threshold",
            EmailEnabled = true,
            SmsEnabled = true,
            PushEnabled = false
        }
    };

    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        ApplicationDbContext context,
        EmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationDeliveryService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public bool EmailConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["EmailSettings:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(_configuration["EmailSettings:SenderEmail"]) &&
        !string.IsNullOrWhiteSpace(_configuration["EmailSettings:SenderPassword"]);

    public bool SmsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Twilio:AccountSid"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Twilio:AuthToken"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Twilio:FromNumber"]);

    public bool PushConfigured =>
        !string.IsNullOrWhiteSpace(GetFirebaseProjectId()) &&
        !string.IsNullOrWhiteSpace(GetFirebaseServiceAccountJson());

    public async Task EnsureDefaultPreferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var existingKeys = await _context.NotificationPreferences
            .AsNoTracking()
            .Select(preference => preference.EventKey)
            .ToListAsync(cancellationToken);

        foreach (var template in DefaultPreferences)
        {
            if (existingKeys.Contains(template.EventKey))
            {
                continue;
            }

            _context.NotificationPreferences.Add(
                new NotificationPreference
                {
                    EventKey = template.EventKey,
                    EventLabel = template.EventLabel,
                    EventDescription = template.EventDescription,
                    EmailEnabled = template.EmailEnabled,
                    SmsEnabled = template.SmsEnabled,
                    PushEnabled = template.PushEnabled
                });
        }

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<NotificationDeliveryResult> SendEmailAsync(
        string toEmail,
        string subject,
        string bodyHtml)
    {
        if (!EmailConfigured)
        {
            return new(false, "Email delivery is not configured.");
        }

        var sent = await _emailService.SendAsync(
            toEmail.Trim(),
            subject,
            bodyHtml);

        return sent
            ? new(true, $"Test email sent to {toEmail.Trim()}.")
            : new(false, "The SMTP server did not accept the email. Check the application logs and SMTP settings.");
    }

    public async Task<NotificationDeliveryResult> SendSmsAsync(
        string toPhone,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!SmsConfigured)
        {
            return new(false, "SMS delivery is not configured.");
        }

        var accountSid = _configuration["Twilio:AccountSid"]!;
        var authToken = _configuration["Twilio:AuthToken"]!;
        var fromNumber = _configuration["Twilio:FromNumber"]!;

        var endpoint =
            $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(accountSid)}/Messages.json";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["To"] = toPhone.Trim(),
                ["From"] = fromNumber,
                ["Body"] = body
            });

        try
        {
            using var response = await HttpClient.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new(true, $"Test SMS sent to {toPhone.Trim()}.");
            }

            _logger.LogWarning(
                "Twilio SMS failed with HTTP status {StatusCode}.",
                (int)response.StatusCode);

            return new(
                false,
                $"Twilio rejected the SMS request with HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Twilio SMS delivery failed.");
            return new(false, "Could not connect to the SMS provider.");
        }
    }

    public async Task<NotificationDeliveryResult> SendPushToPlayersAsync(
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!PushConfigured)
        {
            return new(false, "Push delivery is not configured.");
        }

        try
        {
            var projectId = GetFirebaseProjectId()!;
            var serviceAccountJson = GetFirebaseServiceAccountJson()!;

            var serviceAccountCredential =
                CredentialFactory.FromJson<ServiceAccountCredential>(
                    serviceAccountJson);

            var credential = serviceAccountCredential
                .ToGoogleCredential()
                .CreateScoped(
                    "https://www.googleapis.com/auth/firebase.messaging");

            var accessToken = await credential
                .UnderlyingCredential
                .GetAccessTokenForRequestAsync();

            var endpoint =
                $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(projectId)}/messages:send";

            var payload = new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object>
                {
                    ["topic"] = "players",
                    ["notification"] = new Dictionary<string, string>
                    {
                        ["title"] = title,
                        ["body"] = body
                    },
                    ["data"] = new Dictionary<string, string>
                    {
                        ["title"] = title,
                        ["body"] = body,
                        ["source"] = "paravolley"
                    },
                    ["android"] = new Dictionary<string, object>
                    {
                        ["priority"] = "high",
                        ["notification"] = new Dictionary<string, string>
                        {
                            ["channel_id"] = "paravolley_updates"
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                endpoint);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await HttpClient.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new(true, "Test push notification sent to the player topic.");
            }

            _logger.LogWarning(
                "Firebase push failed with HTTP status {StatusCode}.",
                (int)response.StatusCode);

            return new(
                false,
                $"Firebase rejected the push request with HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Firebase push delivery failed.");
            return new(false, "Could not send the Firebase push notification.");
        }
    }

    public async Task<IReadOnlyList<NotificationDeliveryResult>> SendToActivePlayersAsync(
        string eventKey,
        string subject,
        string textBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultPreferencesAsync(cancellationToken);

        var preference = await _context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.EventKey == eventKey,
                cancellationToken);

        if (preference == null)
        {
            return new[]
            {
                new NotificationDeliveryResult(false, $"No notification preference exists for '{eventKey}'.")
            };
        }

        var results = new List<NotificationDeliveryResult>();

        if (preference.EmailEnabled && EmailConfigured)
        {
            var emails = await _context.Players
                .AsNoTracking()
                .Where(player =>
                    player.Status == PlayerStatus.Active &&
                    player.Email != "")
                .Select(player => player.Email)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var email in emails)
            {
                results.Add(
                    await SendEmailAsync(
                        email,
                        subject,
                        htmlBody ?? textBody));
            }
        }

        if (preference.SmsEnabled && SmsConfigured)
        {
            var phones = await _context.Players
                .AsNoTracking()
                .Where(player =>
                    player.Status == PlayerStatus.Active &&
                    player.Phone != "")
                .Select(player => player.Phone)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var phone in phones)
            {
                results.Add(
                    await SendSmsAsync(
                        phone,
                        textBody,
                        cancellationToken));
            }
        }

        if (preference.PushEnabled && PushConfigured)
        {
            results.Add(
                await SendPushToPlayersAsync(
                    subject,
                    textBody,
                    cancellationToken));
        }

        if (results.Count == 0)
        {
            results.Add(
                new NotificationDeliveryResult(
                    false,
                    "No enabled notification channel is currently configured for this event."));
        }

        return results;
    }

    public NotificationProviderStatus GetEmailStatus() =>
        new()
        {
            Name = "Email",
            Configured = EmailConfigured,
            Detail = EmailConfigured
                ? "SMTP host, sender and credentials are configured."
                : "Configure EmailSettings:SmtpHost, SenderEmail and SenderPassword."
        };

    public NotificationProviderStatus GetSmsStatus() =>
        new()
        {
            Name = "SMS",
            Configured = SmsConfigured,
            Detail = SmsConfigured
                ? "Twilio account, token and sending number are configured."
                : "Configure Twilio:AccountSid, AuthToken and FromNumber."
        };

    public NotificationProviderStatus GetPushStatus() =>
        new()
        {
            Name = "Push",
            Configured = PushConfigured,
            Detail = PushConfigured
                ? "Firebase Cloud Messaging service credentials are configured."
                : "Configure Firebase:ProjectId and Firebase:ServiceAccountJsonBase64."
        };

    private string? GetFirebaseProjectId()
    {
        var configured = _configuration["Firebase:ProjectId"]?.Trim();

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var json = GetFirebaseServiceAccountJson();
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement
                .TryGetProperty("project_id", out var projectId)
                    ? projectId.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string? GetFirebaseServiceAccountJson()
    {
        var base64 =
            _configuration["Firebase:ServiceAccountJsonBase64"]?.Trim();

        if (!string.IsNullOrWhiteSpace(base64))
        {
            try
            {
                return Encoding.UTF8.GetString(
                    Convert.FromBase64String(base64));
            }
            catch (FormatException)
            {
                _logger.LogWarning(
                    "Firebase:ServiceAccountJsonBase64 is not valid Base64.");
                return null;
            }
        }

        var rawJson =
            _configuration["Firebase:ServiceAccountJson"]?.Trim();

        return string.IsNullOrWhiteSpace(rawJson)
            ? null
            : rawJson;
    }
}
