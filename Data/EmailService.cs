using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SportsManagementMVC.Data
{
    // Sends real emails via SMTP once EmailSettings is configured in appsettings.json
    // (or appsettings.Development.json / user secrets / environment variables).
    // Until a SmtpHost is provided, SendAsync logs the email instead of sending it,
    // so the rest of the app (subscribe flow, notifications) still works end-to-end
    // in this demo without crashing on a missing mail server.
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string bodyHtml)
        {
            var host = _config["EmailSettings:SmtpHost"];
            var portStr = _config["EmailSettings:SmtpPort"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:SenderPassword"];
            var senderName = _config["EmailSettings:SenderName"] ?? "ParaVolley Mpumalanga";
            var enableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out var ssl) ? ssl : true;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(senderEmail))
            {
                // No SMTP server configured yet - log instead of sending so the
                // rest of the feature (subscribing, DB records) still works.
                _logger.LogInformation(
                    "EMAIL NOT SENT (no SMTP configured). To: {To} | Subject: {Subject}\n{Body}",
                    toEmail, subject, bodyHtml);
                return false;
            }

            try
            {
                var port = int.TryParse(portStr, out var p) ? p : 587;

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true,
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {To}", toEmail);
                return false;
            }
        }
    }
}
