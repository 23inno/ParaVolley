using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SportsManagementMVC.Data
{
    public sealed record EmailSendResult(
        bool Success,
        string? Diagnostic = null);

    // Prefers the Resend HTTPS API when configured. This works on hosts
    // that block outbound SMTP ports (including free Render web services).
    // SMTP remains available as a fallback for local/Railway deployments.
    public class EmailService
    {
        private static readonly HttpClient HttpClient = new();

        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration config,
            ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendAsync(
            string toEmail,
            string subject,
            string bodyHtml)
        {
            var result = await SendDetailedAsync(
                toEmail,
                subject,
                bodyHtml);

            return result.Success;
        }

        public async Task<EmailSendResult> SendDetailedAsync(
            string toEmail,
            string subject,
            string bodyHtml)
        {
            var resendApiKey =
                _config["Resend:ApiKey"];

            if (!string.IsNullOrWhiteSpace(resendApiKey))
            {
                return await SendWithResendAsync(
                    toEmail,
                    subject,
                    bodyHtml,
                    resendApiKey);
            }

            return await SendWithSmtpAsync(
                toEmail,
                subject,
                bodyHtml);
        }

        private async Task<EmailSendResult> SendWithResendAsync(
            string toEmail,
            string subject,
            string bodyHtml,
            string apiKey)
        {
            var senderEmail =
                _config["Resend:SenderEmail"]
                ?? _config["EmailSettings:SenderEmail"];

            var senderName =
                _config["Resend:SenderName"]
                ?? _config["EmailSettings:SenderName"]
                ?? "ParaVolley Mpumalanga";

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                return new EmailSendResult(
                    false,
                    "Resend sender email is missing.");
            }

            var payload = new
            {
                from = $"{senderName} <{senderEmail}>",
                to = new[] { toEmail },
                subject,
                html = bodyHtml
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.resend.com/emails");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey.Trim());

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                using var response =
                    await HttpClient.SendAsync(request);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new EmailSendResult(true);
                }

                _logger.LogWarning(
                    "Resend rejected email to {To} with HTTP {StatusCode}: {Body}",
                    toEmail,
                    (int)response.StatusCode,
                    BuildSafeDiagnostic(responseBody));

                return new EmailSendResult(
                    false,
                    BuildSafeDiagnostic(
                        $"Resend HTTP {(int)response.StatusCode}: {responseBody}"));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to send email to {To} through Resend",
                    toEmail);

                return new EmailSendResult(
                    false,
                    BuildSafeDiagnostic(
                        $"{exception.GetType().Name}: {exception.Message}"));
            }
        }

        private async Task<EmailSendResult> SendWithSmtpAsync(
            string toEmail,
            string subject,
            string bodyHtml)
        {
            var host = _config["EmailSettings:SmtpHost"];
            var portStr = _config["EmailSettings:SmtpPort"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:SenderPassword"];
            var senderName =
                _config["EmailSettings:SenderName"]
                ?? "ParaVolley Mpumalanga";

            var enableSsl =
                bool.TryParse(
                    _config["EmailSettings:EnableSsl"],
                    out var ssl)
                    ? ssl
                    : true;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogInformation(
                    "EMAIL NOT SENT (no email provider configured). To: {To} | Subject: {Subject}",
                    toEmail,
                    subject);

                return new EmailSendResult(
                    false,
                    "No Resend API key or SMTP configuration is available.");
            }

            try
            {
                var port =
                    int.TryParse(portStr, out var parsedPort)
                        ? parsedPort
                        : 587;

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(
                        senderEmail,
                        senderPassword),
                    EnableSsl = enableSsl,
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(
                        senderEmail,
                        senderName),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true,
                };

                message.To.Add(toEmail);

                await client.SendMailAsync(message);

                return new EmailSendResult(true);
            }
            catch (SmtpException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to send email to {To}",
                    toEmail);

                return new EmailSendResult(
                    false,
                    BuildSafeDiagnostic(
                        $"SMTP {exception.StatusCode}: {exception.Message}"));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to send email to {To}",
                    toEmail);

                return new EmailSendResult(
                    false,
                    BuildSafeDiagnostic(
                        $"{exception.GetType().Name}: {exception.Message}"));
            }
        }

        private static string BuildSafeDiagnostic(
            string value)
        {
            var singleLine = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            return singleLine.Length <= 300
                ? singleLine
                : singleLine[..300];
        }
    }
}
