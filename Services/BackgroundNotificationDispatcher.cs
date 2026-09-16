using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;

namespace SportsManagementMVC.Services;

public sealed class BackgroundNotificationDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundNotificationDispatcher> _logger;

    public BackgroundNotificationDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundNotificationDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void QueuePlayerNotification(
        string eventKey,
        string subject,
        string textBody,
        string? htmlBody = null,
        string? context = null)
    {
        Queue(async services =>
        {
            var delivery =
                services.GetRequiredService<NotificationDeliveryService>();

            var results = await delivery.SendToActivePlayersAsync(
                eventKey,
                subject,
                textBody,
                htmlBody);

            foreach (var result in results.Where(result => !result.Success))
            {
                _logger.LogWarning(
                    "Background notification {Context} reported: {Message}",
                    context ?? eventKey,
                    result.Message);
            }
        }, context ?? eventKey);
    }

    public void QueueSubscriberBroadcast(
        string subject,
        string bodyHtml,
        string context)
    {
        Queue(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var emailService = services.GetRequiredService<EmailService>();

            var emails = await db.Subscribers
                .AsNoTracking()
                .Select(subscriber => subscriber.Email)
                .Where(email => email != "")
                .Distinct()
                .ToListAsync();

            foreach (var email in emails)
            {
                var sent = await emailService.SendAsync(
                    email,
                    subject,
                    bodyHtml);

                if (!sent)
                {
                    _logger.LogWarning(
                        "Background subscriber email {Context} was not accepted for {Email}.",
                        context,
                        email);
                }
            }
        }, context);
    }

    public void QueueEmail(
        string toEmail,
        string subject,
        string bodyHtml,
        string context)
    {
        Queue(async services =>
        {
            var emailService = services.GetRequiredService<EmailService>();
            var sent = await emailService.SendAsync(
                toEmail,
                subject,
                bodyHtml);

            if (!sent)
            {
                _logger.LogWarning(
                    "Background email {Context} was not accepted for {Email}.",
                    context,
                    toEmail);
            }
        }, context);
    }

    private void Queue(
        Func<IServiceProvider, Task> work,
        string context)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await work(scope.ServiceProvider);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Background notification task {Context} failed.",
                    context);
            }
        });
    }
}
