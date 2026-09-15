using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services;

public sealed class AutomaticAbsenceSchedulerService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomaticAbsenceSchedulerService> _logger;

    public AutomaticAbsenceSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomaticAbsenceSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FinalizeMissedAttendanceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Automatic attendance finalization failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task FinalizeMissedAttendanceAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var attendanceNotifications = scope.ServiceProvider
            .GetRequiredService<AttendanceNotificationService>();

        // South Africa is UTC+2 throughout the year and does not observe DST.
        // Once the SAST calendar day has rolled over, the previous event day
        // is considered complete and missing registered-player records can be
        // finalized as Absent.
        var todaySast = DateTime.UtcNow.AddHours(2).Date;
        var yesterdaySast = todaySast.AddDays(-1);

        var candidates = await context.EventRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.Status == EventRegistrationStatus.Registered &&
                registration.Event.Status != EventStatus.Cancelled &&
                registration.Event.Date < todaySast &&
                !context.Attendances.Any(attendance =>
                    attendance.PlayerId == registration.PlayerId &&
                    attendance.EventId == registration.EventId))
            .OrderByDescending(registration => registration.Event.Date)
            .ThenBy(registration => registration.EventId)
            .ThenBy(registration => registration.PlayerId)
            .Select(registration => new
            {
                registration.PlayerId,
                registration.EventId,
                EventDate = registration.Event.Date
            })
            .Take(500)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return;
        }

        var created = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AttendanceThresholdSnapshot? before = null;

            // Only newly completed yesterday records participate in the live
            // low-attendance alert workflow. Older historical backfill is kept
            // silent so enabling this scheduler cannot generate an alert flood.
            if (candidate.EventDate.Date == yesterdaySast)
            {
                before = await attendanceNotifications.GetSnapshotAsync(
                    candidate.PlayerId,
                    cancellationToken);
            }

            var attendance = new Attendance
            {
                PlayerId = candidate.PlayerId,
                EventId = candidate.EventId,
                Date = candidate.EventDate.Date,
                Status = AttendanceStatus.Absent
            };

            context.Attendances.Add(attendance);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                created++;
            }
            catch (DbUpdateException exception)
                when (DatabaseConflictClassifier.IsUniqueViolation(
                    exception,
                    context.Database))
            {
                // Another app instance or a late manual/QR submission may have
                // created the Player/Event attendance row after the candidate
                // query. The unique constraint makes the operation idempotent.
                context.Entry(attendance).State = EntityState.Detached;
                continue;
            }

            if (before != null)
            {
                await attendanceNotifications.NotifyIfCrossedBelowAsync(
                    candidate.PlayerId,
                    before,
                    cancellationToken);
            }
        }

        if (created > 0)
        {
            _logger.LogInformation(
                "Automatically marked {Count} registered player attendance record(s) Absent after the SAST event day ended.",
                created);
        }
    }
}
