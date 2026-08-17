using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/player/dashboard")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Player"
    )]
    public class PlayerDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PlayerDashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
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
                .FirstOrDefaultAsync(item => item.Id == playerId);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            var upcomingEvents = await _db.Events
                .AsNoTracking()
                .Where(item =>
                    item.Status == EventStatus.Upcoming)
                .OrderBy(item => item.Date)
                .ThenBy(item => item.Time)
                .Take(5)
                .Select(item => new
                {
                    id = item.Id,
                    title = item.Title,
                    date = item.Date,
                    time = item.Time,
                    location = item.Location,
                    type = item.Type.ToString(),
                    status = item.Status.ToString()
                })
                .ToListAsync();

            var registeredEvents = await _db.EventRegistrations
                .AsNoTracking()
                .Include(item => item.Event)
                .Where(item =>
                    item.PlayerId == playerId &&
                    item.Status == EventRegistrationStatus.Registered)
                .OrderBy(item => item.Event.Date)
                .ThenBy(item => item.Event.Time)
                .Take(5)
                .Select(item => new
                {
                    registrationId = item.Id,
                    eventId = item.EventId,
                    title = item.Event.Title,
                    date = item.Event.Date,
                    time = item.Event.Time,
                    location = item.Event.Location,
                    status = item.Status.ToString()
                })
                .ToListAsync();

            var recentAnnouncements = await _db.Announcements
                .AsNoTracking()
                .OrderByDescending(item => item.IsPinned)
                .ThenByDescending(item => item.Date)
                .Take(5)
                .Select(item => new
                {
                    id = item.Id,
                    title = item.Title,
                    excerpt = item.Excerpt,
                    category = item.Category.ToString(),
                    date = item.Date,
                    isPinned = item.IsPinned
                })
                .ToListAsync();

            var recentMatches = await _db.Matches
                .AsNoTracking()
                .OrderByDescending(item => item.Date)
                .ThenByDescending(item => item.Id)
                .Take(5)
                .Select(item => new
                {
                    id = item.Id,
                    teamA = item.TeamA,
                    teamB = item.TeamB,
                    date = item.Date,
                    time = item.Time,
                    venue = item.Venue,
                    tournament = item.Tournament,
                    status = item.Status.ToString(),
                    scoreA = item.ScoreA,
                    scoreB = item.ScoreB
                })
                .ToListAsync();

            var attendanceRecords = await _db.Attendances
                .AsNoTracking()
                .Where(item =>
                    item.PlayerId == playerId)
                .ToListAsync();

            var totalAttendance = attendanceRecords.Count;

            var presentAttendance = attendanceRecords.Count(item =>
                item.Status == AttendanceStatus.Present);

            var absentAttendance = attendanceRecords.Count(item =>
                item.Status == AttendanceStatus.Absent);

            var attendanceRate =
                totalAttendance == 0
                    ? 0
                    : Math.Round(
                        (double)presentAttendance /
                        totalAttendance * 100,
                        1);

            return Ok(new
            {
                player = new
                {
                    id = player.Id,
                    name = player.Name,
                    email = player.Email,
                    position = player.Position,
                    team = player.Team,
                    status = player.Status.ToString()
                },

                summary = new
                {
                    upcomingEvents =
                        upcomingEvents.Count,

                    registeredEvents =
                        registeredEvents.Count,

                    totalAttendance,

                    presentAttendance,

                    absentAttendance,

                    attendanceRate
                },

                upcomingEvents,
                registeredEvents,
                recentAnnouncements,
                recentMatches
            });
        }
    }
}