using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services;

public sealed record AttendanceThresholdSnapshot(
    int Season,
    int Threshold,
    int Sessions,
    int Present,
    double Rate);

public sealed class AttendanceNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly StaffNotificationService _staffNotifications;
    private readonly ILogger<AttendanceNotificationService> _logger;

    public AttendanceNotificationService(
        ApplicationDbContext context,
        StaffNotificationService staffNotifications,
        ILogger<AttendanceNotificationService> logger)
    {
        _context = context;
        _staffNotifications = staffNotifications;
        _logger = logger;
    }

    public async Task<AttendanceThresholdSnapshot> GetSnapshotAsync(
        int playerId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.OrganisationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var season =
            int.TryParse(settings?.ActiveSeason, out var configuredSeason)
                ? configuredSeason
                : DateTime.Today.Year;

        var threshold = Math.Clamp(
            settings?.MinAttendancePercent ?? 75,
            0,
            100);

        var seasonStart = new DateTime(season, 1, 1);
        var seasonEnd = seasonStart.AddYears(1);

        var totals = await _context.Attendances
            .AsNoTracking()
            .Where(record =>
                record.PlayerId == playerId &&
                record.Date >= seasonStart &&
                record.Date < seasonEnd)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Sessions = group.Count(),
                Present = group.Count(record =>
                    record.Status == AttendanceStatus.Present)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var sessions = totals?.Sessions ?? 0;
        var present = totals?.Present ?? 0;
        var rate = sessions == 0
            ? 100.0
            : Math.Round(present * 100.0 / sessions, 1);

        return new AttendanceThresholdSnapshot(
            season,
            threshold,
            sessions,
            present,
            rate);
    }

    public async Task NotifyIfCrossedBelowAsync(
        int playerId,
        AttendanceThresholdSnapshot before,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var after = await GetSnapshotAsync(
                playerId,
                cancellationToken);

            if (after.Sessions == 0 ||
                after.Rate >= after.Threshold)
            {
                return;
            }

            var wasAlreadyBelow =
                before.Sessions > 0 &&
                before.Rate < after.Threshold;

            if (wasAlreadyBelow)
            {
                return;
            }

            var player = await _context.Players
                .AsNoTracking()
                .Where(item => item.Id == playerId)
                .Select(item => new
                {
                    item.Name,
                    item.Team
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (player == null)
            {
                return;
            }

            var subject = $"Low attendance alert: {player.Name}";
            var teamText = string.IsNullOrWhiteSpace(player.Team)
                ? string.Empty
                : $" ({player.Team})";

            var textBody =
                $"{player.Name}{teamText} has an attendance rate of " +
                $"{after.Rate:0.#}% ({after.Present}/{after.Sessions}) for {after.Season}, " +
                $"which is below the configured minimum of {after.Threshold}%.";

            var safeName =
                System.Net.WebUtility.HtmlEncode(player.Name);
            var safeTeam =
                System.Net.WebUtility.HtmlEncode(player.Team ?? string.Empty);

            var htmlBody = $"""
                <p>ParaVolley Mpumalanga attendance has fallen below the configured minimum.</p>
                <p><strong>Player:</strong> {safeName}</p>
                <p><strong>Team:</strong> {(string.IsNullOrWhiteSpace(safeTeam) ? "Not assigned" : safeTeam)}</p>
                <p><strong>Season:</strong> {after.Season}</p>
                <p><strong>Attendance:</strong> {after.Rate:0.#}% ({after.Present}/{after.Sessions})</p>
                <p><strong>Minimum:</strong> {after.Threshold}%</p>
                """;

            var results = await _staffNotifications.SendAsync(
                "low_attendance",
                subject,
                textBody,
                htmlBody,
                includeCoaches: true,
                cancellationToken);

            foreach (var result in results.Where(item => !item.Success))
            {
                _logger.LogWarning(
                    "Low attendance alert for player {PlayerId} reported: {Message}",
                    playerId,
                    result.Message);
            }
        }
        catch (Exception exception)
        {
            // Attendance changes must stay committed even if notification
            // delivery or a follow-up query fails.
            _logger.LogWarning(
                exception,
                "Attendance changed for player {PlayerId}, but the low-attendance alert check failed.",
                playerId);
        }
    }
}
