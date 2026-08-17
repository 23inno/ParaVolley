using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/announcements")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Coach,Player"
    )]
    public class AnnouncementsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AnnouncementsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            var announcements = await _db.Announcements
                .AsNoTracking()
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.Date)
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    excerpt = a.Excerpt,
                    content = a.Content,
                    author = a.Author,
                    date = a.Date,
                    category = a.Category.ToString(),
                    isPinned = a.IsPinned,
                    views = a.Views
                })
                .ToListAsync();

            return Ok(announcements);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetAnnouncement(int id)
        {
            var announcement = await _db.Announcements
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound(new
                {
                    message = "The announcement could not be found."
                });
            }

            return Ok(new
            {
                id = announcement.Id,
                title = announcement.Title,
                excerpt = announcement.Excerpt,
                content = announcement.Content,
                author = announcement.Author,
                date = announcement.Date,
                category = announcement.Category.ToString(),
                isPinned = announcement.IsPinned,
                views = announcement.Views
            });
        }
    }
}