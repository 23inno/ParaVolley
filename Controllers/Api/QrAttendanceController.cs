using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using AttendanceEntity = SportsManagementMVC.Models.Attendance;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/qr-attendance")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
    )]
    public class QrAttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public QrAttendanceController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("events/{eventId:int}/sessions")]
        [Authorize(Roles = "Admin,Coach")]
        [EnableRateLimiting("sensitive")]
        public async Task<ActionResult<QrAttendanceSessionDto>>
            CreateSession(int eventId)
        {
            var appUserIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(appUserIdValue, out var appUserId))
            {
                return Unauthorized(new
                {
                    message =
                        "The access token does not contain a valid user account."
                });
            }

            var eventItem = await _db.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "The event could not be found."
                });
            }

            if (eventItem.Status == EventStatus.Cancelled)
            {
                return Conflict(new
                {
                    message =
                        "A QR attendance session cannot be created for a cancelled event."
                });
            }

            string rawToken;
            string tokenHash;

            do
            {
                rawToken = GenerateAttendanceCode();
                tokenHash = HashToken(rawToken);
            }
            while (await _db.QrAttendanceSessions
                .AsNoTracking()
                .AnyAsync(item => item.TokenHash == tokenHash));

            var createdAtUtc = DateTime.UtcNow;

            // ParaVolley operates in South Africa (SAST, UTC+2).
            // A generated QR session remains valid until midnight at the
            // end of the South African calendar day on which it was created.
            var createdAtSast = createdAtUtc.AddHours(2);
            var startOfNextSastDay = createdAtSast.Date.AddDays(1);
            var expiresAtUtc = DateTime.SpecifyKind(
                startOfNextSastDay.AddHours(-2),
                DateTimeKind.Utc);

            var session = new QrAttendanceSession
            {
                EventId = eventId,
                TokenHash = tokenHash,
                CreatedAtUtc = createdAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                IsRevoked = false,
                CreatedByAppUserId = appUserId
            };

            _db.QrAttendanceSessions.Add(session);
            await _db.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new QrAttendanceSessionDto
                {
                    SessionId = session.Id,
                    EventId = eventItem.Id,
                    EventTitle = eventItem.Title,
                    Token = rawToken,
                    ExpiresAtUtc = expiresAtUtc
                });
        }

        [HttpPost("sessions/{sessionId:int:min(1)}/revoke")]
        [Authorize(Roles = "Admin,Coach")]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> RevokeSession(int sessionId)
        {
            var appUserIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(appUserIdValue, out var appUserId))
            {
                return Unauthorized(new
                {
                    message =
                        "The access token does not contain a valid user account."
                });
            }

            var session = await _db.QrAttendanceSessions
                .FirstOrDefaultAsync(item => item.Id == sessionId);

            if (session == null)
            {
                return NotFound(new
                {
                    message =
                        "The QR attendance session could not be found."
                });
            }

            if (User.IsInRole("Coach") &&
                session.CreatedByAppUserId != appUserId)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            if (session.IsRevoked)
            {
                return Ok(new
                {
                    message =
                        "The QR attendance session is already revoked."
                });
            }

            session.IsRevoked = true;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "The QR attendance session was revoked successfully."
            });
        }

        [HttpPost("check-in")]
        [Authorize(Roles = "Player")]
        [EnableRateLimiting("qr-check-in")]
        public async Task<ActionResult<AttendanceDto>>
            CheckIn(QrCheckInRequest request)
        {
            var playerIdValue = User.FindFirstValue("playerId");

            if (!int.TryParse(playerIdValue, out var playerId))
            {
                return Unauthorized(new
                {
                    message =
                        "The access token does not contain a valid player account."
                });
            }

            var player = await _db.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == playerId &&
                    p.Status == PlayerStatus.Active);

            if (player == null)
            {
                return NotFound(new
                {
                    message =
                        "The active player profile could not be found."
                });
            }

            var rawToken = request.Token.Trim().ToUpperInvariant();
            var tokenHash = HashToken(rawToken);

            var session = await _db.QrAttendanceSessions
                .AsNoTracking()
                .Include(qrSession => qrSession.Event)
                .FirstOrDefaultAsync(qrSession =>
                    qrSession.TokenHash == tokenHash);

            if (session == null)
            {
                return BadRequest(new
                {
                    message =
                        "The QR attendance code is invalid."
                });
            }

            if (session.IsRevoked)
            {
                await RecordAttemptAsync(
                    session,
                    playerId,
                    "SessionEnded",
                    "Scan rejected — attendance session has ended.");

                return Conflict(new
                {
                    message =
                        "This QR attendance session has been revoked."
                });
            }

            if (session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                await RecordAttemptAsync(
                    session,
                    playerId,
                    "Expired",
                    "Scan rejected — QR session expired.");

                return Conflict(new
                {
                    message =
                        "This QR attendance code has expired."
                });
            }

            if (session.Event.Status == EventStatus.Cancelled)
            {
                return Conflict(new
                {
                    message =
                        "Attendance cannot be recorded for a cancelled event."
                });
            }

            var isRegistered = await _db.EventRegistrations
                .AsNoTracking()
                .AnyAsync(registration =>
                    registration.PlayerId == playerId &&
                    registration.EventId == session.EventId &&
                    registration.Status ==
                        EventRegistrationStatus.Registered);

            if (!isRegistered)
            {
                await RecordAttemptAsync(
                    session,
                    playerId,
                    "NotRegistered",
                    $"{player.Name} is not registered for this event.");

                return Conflict(new
                {
                    message =
                        "You are not registered for this event."
                });
            }

            var attendanceExists = await _db.Attendances
                .AsNoTracking()
                .AnyAsync(attendance =>
                    attendance.PlayerId == playerId &&
                    attendance.EventId == session.EventId);

            if (attendanceExists)
            {
                await RecordAttemptAsync(
                    session,
                    playerId,
                    "Duplicate",
                    $"Duplicate scan — {player.Name} is already checked in.");

                return DuplicateAttendanceConflict();
            }

            var attendanceRecord = new AttendanceEntity
            {
                PlayerId = playerId,
                EventId = session.EventId,
                Date = session.Event.Date.Date,
                Status = AttendanceStatus.Present,
                CheckedInAtUtc = DateTime.UtcNow,
                EntryMethod = AttendanceEntryMethod.Qr
            };

            _db.Attendances.Add(attendanceRecord);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
                when (DatabaseConflictClassifier.IsUniqueViolation(
                    exception,
                    _db.Database))
            {
                _db.Entry(attendanceRecord).State = EntityState.Detached;

                await RecordAttemptAsync(
                    session,
                    playerId,
                    "Duplicate",
                    $"Duplicate scan — {player.Name} is already checked in.");

                return DuplicateAttendanceConflict();
            }

            return StatusCode(
                StatusCodes.Status201Created,
                new AttendanceDto
                {
                    Id = attendanceRecord.Id,
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    EventId = session.Event.Id,
                    EventTitle = session.Event.Title,
                    EventDate = session.Event.Date,
                    EventTime = session.Event.Time,
                    EventLocation = session.Event.Location,
                    AttendanceDate = attendanceRecord.Date,
                    CheckedInAtUtc = attendanceRecord.CheckedInAtUtc,
                    EntryMethod = attendanceRecord.EntryMethod.ToString(),
                    Status = attendanceRecord.Status.ToString()
                });
        }

        private async Task RecordAttemptAsync(
            QrAttendanceSession session,
            int playerId,
            string outcome,
            string message)
        {
            _db.QrAttendanceAttempts.Add(new QrAttendanceAttempt
            {
                EventId = session.EventId,
                QrAttendanceSessionId = session.Id,
                PlayerId = playerId,
                AttemptedAtUtc = DateTime.UtcNow,
                Outcome = outcome,
                Message = message
            });

            await _db.SaveChangesAsync();
        }

        private static string GenerateAttendanceCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> code = stackalloc char[6];

            for (var index = 0; index < code.Length; index++)
            {
                code[index] = alphabet[
                    RandomNumberGenerator.GetInt32(alphabet.Length)];
            }

            return new string(code);
        }

        private static string HashToken(string rawToken)
        {
            var tokenHashBytes =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(rawToken));

            return Convert.ToHexString(tokenHashBytes);
        }

        private ConflictObjectResult DuplicateAttendanceConflict()
        {
            return Conflict(new
            {
                message =
                    "Attendance has already been recorded for this player and event."
            });
        }
    }
}
