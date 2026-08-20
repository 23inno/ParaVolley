using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
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

        // Admin/Coach creates a temporary QR attendance session.
        [HttpPost("events/{eventId:int}/sessions")]
        [Authorize(Roles = "Admin,Coach")]
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

            var userExists = await _db.AppUsers
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id == appUserId &&
                    user.IsActive);

            if (!userExists)
            {
                return Unauthorized(new
                {
                    message =
                        "The authenticated user account could not be found."
                });
            }

            var rawTokenBytes =
                RandomNumberGenerator.GetBytes(32);

            var rawToken =
                Convert.ToHexString(rawTokenBytes);

            var tokenHash =
                HashToken(rawToken);

            var createdAtUtc = DateTime.UtcNow;
            var expiresAtUtc =
                createdAtUtc.AddMinutes(15);

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

            var userIsActive = await _db.AppUsers
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id == appUserId &&
                    user.IsActive);

            if (!userIsActive)
            {
                return Unauthorized(new
                {
                    message =
                        "The authenticated user account could not be found."
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

        // Player submits the token obtained from scanning the QR code.
        [HttpPost("check-in")]
        [Authorize(Roles = "Player")]
        public async Task<ActionResult<AttendanceDto>>
            CheckIn(QrCheckInRequest request)
        {
            var playerIdValue =
                User.FindFirstValue("playerId");

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

            var rawToken = request.Token.Trim();
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
                return Conflict(new
                {
                    message =
                        "This QR attendance session has been revoked."
                });
            }

            if (session.ExpiresAtUtc <= DateTime.UtcNow)
            {
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
                return DuplicateAttendanceConflict();
            }

            var attendanceRecord = new AttendanceEntity
            {
                PlayerId = playerId,
                EventId = session.EventId,
                Date = session.Event.Date.Date,
                Status = AttendanceStatus.Present
            };

            _db.Attendances.Add(attendanceRecord);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                })
            {
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
                    Status = attendanceRecord.Status.ToString()
                });
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
