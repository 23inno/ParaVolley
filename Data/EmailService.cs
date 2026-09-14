using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SportsManagementMVC.Data
{
    public sealed record EmailSendResult(
        bool Success,
        string? Diagnostic = null);

    // Sends real emails via SMTP once EmailSettings is configured in
    // appsettings.Development.json, user secrets, or environment variables.
    public class EmailService
    {
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
                    "EMAIL NOT SENT (no SMTP configured). To: {To} | Subject: {Subject}",
                    toEmail,
                    subject);

                return new EmailSendResult(
                    false,
                    "SMTP host or sender email is missing.");
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

        private static string BuildSafeDiagnostic(string value)
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
