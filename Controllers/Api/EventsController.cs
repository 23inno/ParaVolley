using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Models;
using EventEntity = SportsManagementMVC.Models.Event;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/events")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Coach,Player"
    )]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EventsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents()
        {
            var events = await _db.Events
                .AsNoTracking()
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Time)
                .ThenBy(e => e.Id)
                .ToListAsync();

            var response = events
                .Select(MapToDto)
                .ToList();

            return Ok(response);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<ActionResult<EventDto>> GetEvent(int id)
        {
            var eventItem = await _db.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "The event could not be found."
                });
            }

            return Ok(MapToDto(eventItem));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<ActionResult<EventDto>> CreateEvent(
            EventDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Event title is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Time))
            {
                return BadRequest(new
                {
                    message = "Event time is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new
                {
                    message = "Event location is required."
                });
            }

            if (!Enum.TryParse<EventType>(
                request.Type,
                true,
                out var eventType))
            {
                return BadRequest(new
                {
                    message = "Invalid event type."
                });
            }

            if (!Enum.TryParse<EventStatus>(
                request.Status,
                true,
                out var eventStatus))
            {
                return BadRequest(new
                {
                    message = "Invalid event status."
                });
            }

            if (request.Participants < 0)
            {
                return BadRequest(new
                {
                    message =
                        "Participants cannot be less than zero."
                });
            }

            var eventItem = new EventEntity
            {
                Title = request.Title.Trim(),
                Date = request.Date,
                Time = request.Time.Trim(),
                Location = request.Location.Trim(),
                Type = eventType,
                Participants = request.Participants,
                Status = eventStatus,
                Description = request.Description?.Trim()
                    ?? string.Empty
            };

            _db.Events.Add(eventItem);

            await _db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new
                {
                    id = eventItem.Id
                },
                MapToDto(eventItem));
        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<ActionResult<EventDto>> UpdateEvent(
            int id,
            EventDto request)
        {
            var eventItem = await _db.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "The event could not be found."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Event title is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Time))
            {
                return BadRequest(new
                {
                    message = "Event time is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest(new
                {
                    message = "Event location is required."
                });
            }

            if (!Enum.TryParse<EventType>(
                request.Type,
                true,
                out var eventType))
            {
                return BadRequest(new
                {
                    message = "Invalid event type."
                });
            }

            if (!Enum.TryParse<EventStatus>(
                request.Status,
                true,
                out var eventStatus))
            {
                return BadRequest(new
                {
                    message = "Invalid event status."
                });
            }

            if (request.Participants < 0)
            {
                return BadRequest(new
                {
                    message =
                        "Participants cannot be less than zero."
                });
            }

            eventItem.Title = request.Title.Trim();
            eventItem.Date = request.Date;
            eventItem.Time = request.Time.Trim();
            eventItem.Location = request.Location.Trim();
            eventItem.Type = eventType;
            eventItem.Participants = request.Participants;
            eventItem.Status = eventStatus;
            eventItem.Description =
                request.Description?.Trim() ?? string.Empty;

            await _db.SaveChangesAsync();

            return Ok(MapToDto(eventItem));
        }

        [HttpDelete("{id:int:min(1)}")]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _db.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "The event could not be found."
                });
            }

            var hasRegistrations =
                await _db.EventRegistrations
                    .AnyAsync(registration =>
                        registration.EventId == id);

            var hasAttendance =
                await _db.Attendances
                    .AnyAsync(attendance =>
                        attendance.EventId == id);

            if (hasRegistrations || hasAttendance)
            {
                return Conflict(new
                {
                    message =
                        "This event cannot be deleted because registrations or attendance records already exist."
                });
            }

            _db.Events.Remove(eventItem);

            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static EventDto MapToDto(
            EventEntity eventItem)
        {
            return new EventDto
            {
                Id = eventItem.Id,
                Title = eventItem.Title,
                Date = eventItem.Date,
                Time = eventItem.Time,
                Location = eventItem.Location,
                Type = eventItem.Type.ToString(),
                Participants = eventItem.Participants,
                Status = eventItem.Status.ToString(),
                Description = eventItem.Description
            };
        }
    }
}