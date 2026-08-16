using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;

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

        private static AttendanceDto MapToDto(
            Models.Attendance attendance)
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