using System.Security.Claims;
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
    [Route("api/events")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Player"
    )]
    public class EventRegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EventRegistrationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("{eventId:int:min(1)}/register")]
        public async Task<ActionResult<EventRegistrationDto>> Register(
            int eventId)
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
                .FirstOrDefaultAsync(playerItem =>
                    playerItem.Id == playerId);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            if (player.Status != PlayerStatus.Active)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "Only active players may register for events."
                    });
            }

            var eventItem = await _db.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == eventId);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "The event could not be found."
                });
            }

            if (eventItem.Status != EventStatus.Upcoming)
            {
                return Conflict(new
                {
                    message =
                        "Registration is only available for upcoming events."
                });
            }

            var existingRegistration =
                await _db.EventRegistrations
                    .FirstOrDefaultAsync(registration =>
                        registration.PlayerId == playerId &&
                        registration.EventId == eventId);

            if (existingRegistration?.Status ==
                EventRegistrationStatus.Registered)
            {
                return Conflict(new
                {
                    message =
                        "You are already registered for this event."
                });
            }

            if (existingRegistration != null)
            {
                existingRegistration.Status =
                    EventRegistrationStatus.Registered;

                existingRegistration.RegisteredAtUtc =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(MapToDto(
                    existingRegistration,
                    eventItem));
            }

            var registration = new EventRegistration
            {
                PlayerId = playerId,
                EventId = eventId,
                RegisteredAtUtc = DateTime.UtcNow,
                Status = EventRegistrationStatus.Registered
            };

            _db.EventRegistrations.Add(registration);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        "The event registration already exists or could not be saved."
                });
            }

            var response = MapToDto(
                registration,
                eventItem);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        [HttpGet("/api/player/registrations")]
public async Task<ActionResult<IReadOnlyList<EventRegistrationDto>>>
    GetMyRegistrations()
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

    var registrations = await _db.EventRegistrations
        .AsNoTracking()
        .Include(registration => registration.Event)
        .Where(registration =>
            registration.PlayerId == playerId)
        .OrderBy(registration =>
            registration.Event.Date)
        .ThenBy(registration =>
            registration.Event.Time)
        .ThenBy(registration => registration.Id)
        .ToListAsync();

    var response = registrations
        .Select(registration =>
            MapToDto(
                registration,
                registration.Event))
        .ToList();

    return Ok(response);
}
        private static EventRegistrationDto MapToDto(
            EventRegistration registration,
            Event eventItem)
        {
            return new EventRegistrationDto
            {
                Id = registration.Id,
                EventId = eventItem.Id,
                EventTitle = eventItem.Title,
                EventDate = eventItem.Date,
                EventTime = eventItem.Time,
                EventLocation = eventItem.Location,
                EventType = eventItem.Type.ToString(),
                EventStatus = eventItem.Status.ToString(),
                RegistrationStatus =
                    registration.Status.ToString(),
                RegisteredAtUtc =
                    registration.RegisteredAtUtc
            };
        }
    }
}