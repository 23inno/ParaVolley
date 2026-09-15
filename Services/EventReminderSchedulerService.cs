using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services;

public sealed class EventReminderSchedulerService : BackgroundService
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StaleClaimAge = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EventReminderSchedulerService> _logger;

    public EventReminderSchedulerService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<EventReminderSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Integration tests should not run background notification delivery.
        if (_environment.IsEnvironment("Testing"))
        {
            return;
        }

        await Task.Delay(
            TimeSpan.FromSeconds(20),
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Scheduled event reminder check failed.");
            }

            await Task.Delay(
                CheckInterval,
                stoppingToken);
        }
    }

    private async Task SendDueRemindersAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var delivery = scope.ServiceProvider
            .GetRequiredService<NotificationDeliveryService>();

        await delivery.EnsureDefaultPreferencesAsync(cancellationToken);

        var preference = await context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.EventKey == "event_reminders",
                cancellationToken);

        if (preference == null)
        {
            return;
        }

        var anyConfiguredChannelEnabled =
            (preference.EmailEnabled && delivery.EmailConfigured) ||
            (preference.SmsEnabled && delivery.SmsConfigured) ||
            (preference.PushEnabled && delivery.PushConfigured);

        if (!anyConfiguredChannelEnabled)
        {
            return;
        }

        var now = BackupService.GetSastNow();
        var cutoff = now.Add(ReminderWindow);
        var staleClaimCutoff = now.Subtract(StaleClaimAge);

        // A process may stop after claiming an event but before delivery finishes.
        // Clear only stale, unfinished claims so they can be retried safely.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "EventReminderDispatches"
            WHERE "SentAtSast" IS NULL
              AND "ClaimedAtSast" < {staleClaimCutoff};
            """,
            cancellationToken);

        var events = await context.Events
            .AsNoTracking()
            .Where(item =>
                item.Status == EventStatus.Upcoming &&
                item.Date >= now.Date &&
                item.Date <= cutoff.Date)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        foreach (var ev in events)
        {
            if (!TryGetScheduledForSast(ev, out var scheduledFor))
            {
                _logger.LogWarning(
                    "Event {EventId} has an invalid time value '{EventTime}' and cannot be reminded.",
                    ev.Id,
                    ev.Time);
                continue;
            }

            if (scheduledFor <= now || scheduledFor > cutoff)
            {
                continue;
            }

            var claimed = await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "EventReminderDispatches"
                    ("EventId", "ScheduledForSast", "ClaimedAtSast", "SentAtSast")
                VALUES
                    ({ev.Id}, {scheduledFor}, {now}, NULL)
                ON CONFLICT ("EventId", "ScheduledForSast") DO NOTHING;
                """,
                cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            try
            {
                var scheduledText = scheduledFor.ToString(
                    "dddd, d MMMM yyyy 'at' HH:mm",
                    CultureInfo.InvariantCulture);

                var subject = $"Event reminder: {ev.Title}";
                var textBody =
                    $"Reminder: {ev.Title} is scheduled for {scheduledText} " +
                    $"at {ev.Location}.";

                var encodedTitle =
                    System.Net.WebUtility.HtmlEncode(ev.Title);
                var encodedLocation =
                    System.Net.WebUtility.HtmlEncode(ev.Location);
                var encodedScheduledText =
                    System.Net.WebUtility.HtmlEncode(scheduledText);

                var htmlBody = $"""
                    <p>This is a reminder for an upcoming ParaVolley Mpumalanga event.</p>
                    <h3>{encodedTitle}</h3>
                    <p><strong>When:</strong> {encodedScheduledText}</p>
                    <p><strong>Where:</strong> {encodedLocation}</p>
                    """;

                var results = await delivery.SendToActivePlayersAsync(
                    "event_reminders",
                    subject,
                    textBody,
                    htmlBody,
                    cancellationToken);

                if (results.Any(result => result.Success))
                {
                    var sentAt = BackupService.GetSastNow();

                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                        UPDATE "EventReminderDispatches"
                        SET "SentAtSast" = {sentAt}
                        WHERE "EventId" = {ev.Id}
                          AND "ScheduledForSast" = {scheduledFor};
                        """,
                        cancellationToken);

                    _logger.LogInformation(
                        "Event reminder sent for event {EventId} scheduled at {ScheduledForSast}.",
                        ev.Id,
                        scheduledFor);
                }
                else
                {
                    await ReleaseClaimAsync(
                        context,
                        ev.Id,
                        scheduledFor,
                        cancellationToken);

                    foreach (var result in results)
                    {
                        _logger.LogWarning(
                            "Event {EventId} reminder delivery reported: {Message}",
                            ev.Id,
                            result.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                await ReleaseClaimAsync(
                    context,
                    ev.Id,
                    scheduledFor,
                    CancellationToken.None);

                _logger.LogWarning(
                    exception,
                    "Event {EventId} reminder delivery failed.",
                    ev.Id);
            }
        }
    }

    private static async Task ReleaseClaimAsync(
        ApplicationDbContext context,
        int eventId,
        DateTime scheduledFor,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "EventReminderDispatches"
            WHERE "EventId" = {eventId}
              AND "ScheduledForSast" = {scheduledFor}
              AND "SentAtSast" IS NULL;
            """,
            cancellationToken);
    }

    private static bool TryGetScheduledForSast(
        Event ev,
        out DateTime scheduledFor)
    {
        scheduledFor = default;

        if (TimeSpan.TryParse(
                ev.Time,
                CultureInfo.InvariantCulture,
                out var timeOfDay) &&
            timeOfDay >= TimeSpan.Zero &&
            timeOfDay < TimeSpan.FromDays(1))
        {
            scheduledFor = DateTime.SpecifyKind(
                ev.Date.Date.Add(timeOfDay),
                DateTimeKind.Unspecified);
            return true;
        }

        if (DateTime.TryParse(
                ev.Time,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed) ||
            DateTime.TryParse(
                ev.Time,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out parsed))
        {
            scheduledFor = DateTime.SpecifyKind(
                ev.Date.Date.Add(parsed.TimeOfDay),
                DateTimeKind.Unspecified);
            return true;
        }

        return false;
    }
}
