using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Models;
using AttendanceEntity = SportsManagementMVC.Models.Attendance;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
    )]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AttendanceController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("/api/player/attendance")]
        [Authorize(Roles = "Player")]
        public async Task<ActionResult<IReadOnlyList<AttendanceDto>>>
            GetMyAttendance()
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

            var playerExists = await _db.Players
                .AsNoTracking()
                .AnyAsync(player => player.Id == playerId);

            if (!playerExists)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            var attendanceRecords = await _db.Attendances
                .AsNoTracking()
                .Include(attendance => attendance.Player)
                .Include(attendance => attendance.Event)
                .Where(attendance =>
                    attendance.PlayerId == playerId)
                .OrderByDescending(attendance =>
                    attendance.Date)
                .ThenByDescending(attendance =>
                    attendance.Id)
                .ToListAsync();

            var response = attendanceRecords
                .Select(MapToDto)
                .ToList();

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<ActionResult<AttendanceDto>> RecordAttendance(
            RecordAttendanceRequest request)
        {
            if (!Enum.TryParse<AttendanceStatus>(
                    request.Status,
                    ignoreCase: true,
                    out var attendanceStatus) ||
                !Enum.IsDefined(
                    typeof(AttendanceStatus),
                    attendanceStatus))
            {
                return BadRequest(new
                {
                    message =
                        "Status must be either Present or Absent."
                });
            }

            var player = await _db.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(playerItem =>
                    playerItem.Id == request.PlayerId);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player could not be found."
                });
            }

            var eventItem = await _db.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == request.EventId);

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
                        "Attendance cannot be recorded for a cancelled event."
                });
            }

            var attendanceExists = await _db.Attendances
                .AnyAsync(attendance =>
                    attendance.PlayerId == request.PlayerId &&
                    attendance.EventId == request.EventId);

            if (attendanceExists)
            {
                return Conflict(new
                {
                    message =
                        "Attendance has already been recorded for this player and event."
                });
            }

            var attendanceRecord = new AttendanceEntity
            {
                PlayerId = request.PlayerId,
                EventId = request.EventId,
                Date = eventItem.Date.Date,
                Status = attendanceStatus
            };

            _db.Attendances.Add(attendanceRecord);
            await _db.SaveChangesAsync();

            attendanceRecord.Player = player;
            attendanceRecord.Event = eventItem;

            return StatusCode(
                StatusCodes.Status201Created,
                MapToDto(attendanceRecord));
        }

        private static AttendanceDto MapToDto(
            AttendanceEntity attendance)
        {
            return new AttendanceDto
            {
                Id = attendance.Id,
                PlayerId = attendance.PlayerId,
                PlayerName =
                    attendance.Player?.Name ?? string.Empty,
                EventId = attendance.EventId,
                EventTitle =
                    attendance.Event?.Title ?? string.Empty,
                EventDate =
                    attendance.Event?.Date ?? attendance.Date,
                EventTime =
                    attendance.Event?.Time ?? string.Empty,
                EventLocation =
                    attendance.Event?.Location ?? string.Empty,
                AttendanceDate = attendance.Date,
                Status = attendance.Status.ToString()
            };
        }
    }
}