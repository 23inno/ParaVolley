using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;

namespace SportsManagementMVC.Services;

public static class BackgroundNotificationDispatcher
{
    public static void QueuePlayerNotification(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string eventKey,
        string subject,
        string textBody,
        string? htmlBody = null,
        string? context = null)
    {
        Queue(
            scopeFactory,
            logger,
            async services =>
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
                    logger.LogWarning(
                        "Background notification {Context} reported: {Message}",
                        context ?? eventKey,
                        result.Message);
                }
            },
            context ?? eventKey);
    }

    public static void QueueSubscriberBroadcast(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string subject,
        string bodyHtml,
        string context)
    {
        Queue(
            scopeFactory,
            logger,
            async services =>
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
                        logger.LogWarning(
                            "Background subscriber email {Context} was not accepted for {Email}.",
                            context,
                            email);
                    }
                }
            },
            context);
    }

    public static void QueueEmail(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string toEmail,
        string subject,
        string bodyHtml,
        string context)
    {
        Queue(
            scopeFactory,
            logger,
            async services =>
            {
                var emailService = services.GetRequiredService<EmailService>();
                var sent = await emailService.SendAsync(
                    toEmail,
                    subject,
                    bodyHtml);

                if (!sent)
                {
                    logger.LogWarning(
                        "Background email {Context} was not accepted for {Email}.",
                        context,
                        toEmail);
                }
            },
            context);
    }

    private static void Queue(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        Func<IServiceProvider, Task> work,
        string context)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await work(scope.ServiceProvider);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Background notification task {Context} failed.",
                    context);
            }
        });
    }
}
