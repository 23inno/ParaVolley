using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Models;

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

            var tokenHashBytes =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(rawToken));

            var tokenHash =
                Convert.ToHexString(tokenHashBytes);

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
    }
}
