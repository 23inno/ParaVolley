using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
[Route("Attendance/Live")]
public sealed class LiveAttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceNotificationService _attendanceNotifications;

    public LiveAttendanceController(
        ApplicationDbContext context,
        AttendanceNotificationService attendanceNotifications)
    {
        _context = context;
        _attendanceNotifications = attendanceNotifications;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? eventId,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .AsNoTracking()
            .Where(eventItem => eventItem.Status != EventStatus.Cancelled)
            .OrderByDescending(eventItem => eventItem.Date)
            .ThenBy(eventItem => eventItem.Title)
            .Take(150)
            .ToListAsync(cancellationToken);

        Event? selectedEvent = null;

        if (eventId.HasValue)
        {
            selectedEvent = events.FirstOrDefault(item => item.Id == eventId.Value);
        }

        if (selectedEvent == null)
        {
            var today = DateTime.Today;
            selectedEvent = events
                .Where(item => item.Date >= today)
                .OrderBy(item => item.Date)
                .ThenBy(item => item.Time)
                .FirstOrDefault()
                ?? events.FirstOrDefault();
        }

        return View(
            "~/Views/Attendance/Live.cshtml",
            new LiveAttendanceViewModel
            {
                Events = events,
                SelectedEvent = selectedEvent
            });
    }

    [HttpGet("Status")]
    public async Task<IActionResult> Status(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken);

        if (eventItem == null)
        {
            return NotFound(new { message = "The event could not be found." });
        }

        var registrations = await _context.EventRegistrations
            .AsNoTracking()
            .Include(item => item.Player)
            .Where(item =>
                item.EventId == eventId &&
                item.Status == EventRegistrationStatus.Registered)
            .OrderBy(item => item.Player.Name)
            .ToListAsync(cancellationToken);

        var registeredIds = registrations
            .Select(item => item.PlayerId)
            .ToHashSet();

        var attendanceRows = await _context.Attendances
            .AsNoTracking()
            .Include(item => item.Player)
            .Where(item => item.EventId == eventId)
            .OrderByDescending(item => item.CheckedInAtUtc.HasValue)
            .ThenByDescending(item => item.CheckedInAtUtc)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var attendanceByPlayer = attendanceRows
            .GroupBy(item => item.PlayerId)
            .ToDictionary(group => group.Key, group => group.First());

        var checkedInRows = attendanceRows
            .Where(item =>
                item.Status == AttendanceStatus.Present &&
                registeredIds.Contains(item.PlayerId))
            .ToList();

        var checkedInIds = checkedInRows
            .Select(item => item.PlayerId)
            .ToHashSet();

        var waiting = registrations
            .Where(item => !checkedInIds.Contains(item.PlayerId))
            .Select(item =>
            {
                attendanceByPlayer.TryGetValue(item.PlayerId, out var existing);

                return new
                {
                    playerId = item.PlayerId,
                    playerName = item.Player.Name,
                    team = item.Player.Team,
                    recordStatus = existing?.Status.ToString(),
                    isMarkedAbsent = existing?.Status == AttendanceStatus.Absent
                };
            })
            .ToList();

        var latestSession = await _context.QrAttendanceSessions
            .AsNoTracking()
            .Where(item => item.EventId == eventId)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var sessionIsLive = latestSession != null &&
            !latestSession.IsRevoked &&
            latestSession.ExpiresAtUtc > nowUtc;

        var recentFailureCutoff = nowUtc.AddMinutes(-10);
        var recentFailures = await _context.QrAttendanceAttempts
            .AsNoTracking()
            .Where(item =>
                item.EventId == eventId &&
                item.AttemptedAtUtc >= recentFailureCutoff)
            .OrderByDescending(item => item.AttemptedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var registeredCount = registrations.Count;
        var checkedInCount = checkedInRows.Count;
        var remainingCount = Math.Max(0, registeredCount - checkedInCount);
        var attendanceRate = registeredCount == 0
            ? 0
            : Math.Round(checkedInCount * 100.0 / registeredCount, 1);

        return Json(new
        {
            eventItem = new
            {
                id = eventItem.Id,
                title = eventItem.Title,
                date = eventItem.Date.ToString("yyyy-MM-dd"),
                time = eventItem.Time,
                location = eventItem.Location,
                status = eventItem.Status.ToString()
            },
            summary = new
            {
                registered = registeredCount,
                checkedIn = checkedInCount,
                remaining = remainingCount,
                attendanceRate
            },
            session = latestSession == null
                ? null
                : new
                {
                    id = latestSession.Id,
                    isLive = sessionIsLive,
                    isRevoked = latestSession.IsRevoked,
                    createdAtUtc = latestSession.CreatedAtUtc,
                    expiresAtUtc = latestSession.ExpiresAtUtc
                },
            checkedIn = checkedInRows.Select(item => new
            {
                id = item.Id,
                playerId = item.PlayerId,
                playerName = item.Player?.Name ?? "Player",
                team = item.Player?.Team ?? string.Empty,
                checkedInAtUtc = item.CheckedInAtUtc,
                checkedInTime = item.CheckedInAtUtc.HasValue
                    ? item.CheckedInAtUtc.Value.AddHours(2).ToString("HH:mm:ss")
                    : "Recorded",
                method = GetMethodLabel(item.EntryMethod)
            }),
            waiting,
            recentFailures = recentFailures.Select(item => new
            {
                id = item.Id,
                playerId = item.PlayerId,
                message = item.Message,
                outcome = item.Outcome,
                attemptedAt = item.AttemptedAtUtc.AddHours(2).ToString("HH:mm:ss")
            })
        });
    }

    [HttpPost("Session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var appUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(appUserIdValue, out var appUserId))
        {
            return Unauthorized(new { message = "Your signed-in account could not be identified." });
        }

        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken);

        if (eventItem == null)
        {
            return NotFound(new { message = "The event could not be found." });
        }

        if (eventItem.Status == EventStatus.Cancelled)
        {
            return Conflict(new { message = "Attendance cannot be started for a cancelled event." });
        }

        var existingSessions = await _context.QrAttendanceSessions
            .Where(item => item.EventId == eventId && !item.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var existingSession in existingSessions)
        {
            existingSession.IsRevoked = true;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var nowUtc = DateTime.UtcNow;

        var session = new QrAttendanceSession
        {
            EventId = eventId,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddMinutes(15),
            IsRevoked = false,
            CreatedByAppUserId = appUserId
        };

        _context.QrAttendanceSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            sessionId = session.Id,
            token = rawToken,
            expiresAtUtc = session.ExpiresAtUtc,
            message = "A new 15-minute QR attendance session is live."
        });
    }

    [HttpPost("Manual")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualCheckIn(
        int eventId,
        int playerId,
        CancellationToken cancellationToken = default)
    {
        var eventItem = await _context.Events
            .FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken);

        if (eventItem == null)
        {
            return NotFound(new { message = "The event could not be found." });
        }

        if (eventItem.Status == EventStatus.Cancelled)
        {
            return Conflict(new { message = "Attendance cannot be recorded for a cancelled event." });
        }

        var registration = await _context.EventRegistrations
            .AsNoTracking()
            .Include(item => item.Player)
            .FirstOrDefaultAsync(item =>
                item.EventId == eventId &&
                item.PlayerId == playerId &&
                item.Status == EventRegistrationStatus.Registered,
                cancellationToken);

        if (registration == null)
        {
            return Conflict(new { message = "This player is not registered for the selected event." });
        }

        var existing = await _context.Attendances
            .FirstOrDefaultAsync(item =>
                item.EventId == eventId &&
                item.PlayerId == playerId,
                cancellationToken);

        if (existing?.Status == AttendanceStatus.Present)
        {
            return Conflict(new { message = "This player is already checked in." });
        }

        if (existing == null)
        {
            existing = new Attendance
            {
                EventId = eventId,
                PlayerId = playerId,
                Date = eventItem.Date.Date
            };
            _context.Attendances.Add(existing);
        }

        existing.Status = AttendanceStatus.Present;
        existing.CheckedInAtUtc = DateTime.UtcNow;
        existing.EntryMethod = AttendanceEntryMethod.Manual;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (DatabaseConflictClassifier.IsUniqueViolation(
                exception,
                _context.Database))
        {
            return Conflict(new { message = "This player is already checked in." });
        }

        return Json(new
        {
            message = $"{registration.Player.Name} was checked in manually."
        });
    }

    [HttpPost("Session/{sessionId:int}/End")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndSession(
        int sessionId,
        bool markRemainingAbsent,
        CancellationToken cancellationToken = default)
    {
        var appUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(appUserIdValue, out var appUserId))
        {
            return Unauthorized(new { message = "Your signed-in account could not be identified." });
        }

        var session = await _context.QrAttendanceSessions
            .Include(item => item.Event)
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);

        if (session == null)
        {
            return NotFound(new { message = "The attendance session could not be found." });
        }

        if (User.IsInRole(nameof(AppUserRole.Coach)) &&
            session.CreatedByAppUserId != appUserId)
        {
            return Forbid();
        }

        session.IsRevoked = true;
        var markedAbsent = 0;
        var snapshots = new Dictionary<int, AttendanceThresholdSnapshot>();

        if (markRemainingAbsent)
        {
            var registeredPlayerIds = await _context.EventRegistrations
                .AsNoTracking()
                .Where(item =>
                    item.EventId == session.EventId &&
                    item.Status == EventRegistrationStatus.Registered)
                .Select(item => item.PlayerId)
                .ToListAsync(cancellationToken);

            var existingPlayerIds = await _context.Attendances
                .AsNoTracking()
                .Where(item => item.EventId == session.EventId)
                .Select(item => item.PlayerId)
                .ToListAsync(cancellationToken);

            var existingSet = existingPlayerIds.ToHashSet();
            var missingPlayerIds = registeredPlayerIds
                .Where(playerId => !existingSet.Contains(playerId))
                .Distinct()
                .ToList();

            foreach (var playerId in missingPlayerIds)
            {
                snapshots[playerId] =
                    await _attendanceNotifications.GetSnapshotAsync(
                        playerId,
                        cancellationToken);

                _context.Attendances.Add(new Attendance
                {
                    EventId = session.EventId,
                    PlayerId = playerId,
                    Date = session.Event.Date.Date,
                    Status = AttendanceStatus.Absent,
                    EntryMethod = AttendanceEntryMethod.SessionClose,
                    CheckedInAtUtc = null
                });
            }

            markedAbsent = missingPlayerIds.Count;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var snapshot in snapshots)
        {
            await _attendanceNotifications.NotifyIfCrossedBelowAsync(
                snapshot.Key,
                snapshot.Value,
                cancellationToken);
        }

        return Json(new
        {
            markedAbsent,
            message = markedAbsent > 0
                ? $"Attendance session ended. {markedAbsent} remaining registered player(s) were marked absent."
                : "Attendance session ended."
        });
    }

    private static string HashToken(string rawToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    private static string GetMethodLabel(AttendanceEntryMethod method)
    {
        return method switch
        {
            AttendanceEntryMethod.Qr => "QR",
            AttendanceEntryMethod.Manual => "Manual",
            AttendanceEntryMethod.SessionClose => "Session close",
            _ => "Recorded"
        };
    }
}
