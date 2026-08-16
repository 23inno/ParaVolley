using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
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

        private static EventDto MapToDto(EventEntity eventItem)
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