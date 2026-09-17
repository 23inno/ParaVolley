using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;

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
        public async Task<IActionResult> GetAnnouncements(
            int page = 1,
            int pageSize = Paging.DefaultApiPageSize,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetAppUserId(out var appUserId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid user account."
                });
            }

            page = Paging.Page(page);
            pageSize = Paging.PageSize(pageSize, Paging.MaximumApiPageSize);

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
                    views = a.Views,
                    isRead = _db.AnnouncementReadReceipts.Any(receipt =>
                        receipt.AppUserId == appUserId &&
                        receipt.AnnouncementId == a.Id)
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(announcements);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetAnnouncement(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetAppUserId(out var appUserId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid user account."
                });
            }

            var announcement = await _db.Announcements
                .AsNoTracking()
                .Where(a => a.Id == id)
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
                    views = a.Views,
                    isRead = _db.AnnouncementReadReceipts.Any(receipt =>
                        receipt.AppUserId == appUserId &&
                        receipt.AnnouncementId == a.Id)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (announcement == null)
            {
                return NotFound(new
                {
                    message = "The announcement could not be found."
                });
            }

            return Ok(announcement);
        }

        [HttpPost("{id:int:min(1)}/read")]
        public async Task<IActionResult> MarkAsRead(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetAppUserId(out var appUserId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid user account."
                });
            }

            var announcementExists = await _db.Announcements
                .AsNoTracking()
                .AnyAsync(a => a.Id == id, cancellationToken);

            if (!announcementExists)
            {
                return NotFound(new
                {
                    message = "The announcement could not be found."
                });
            }

            var alreadyRead = await _db.AnnouncementReadReceipts
                .AsNoTracking()
                .AnyAsync(
                    receipt =>
                        receipt.AppUserId == appUserId &&
                        receipt.AnnouncementId == id,
                    cancellationToken);

            if (!alreadyRead)
            {
                _db.AnnouncementReadReceipts.Add(new AnnouncementReadReceipt
                {
                    AppUserId = appUserId,
                    AnnouncementId = id,
                    ReadAtUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(cancellationToken);
            }

            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            CancellationToken cancellationToken = default)
        {
            if (!TryGetAppUserId(out var appUserId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid user account."
                });
            }

            var unreadAnnouncementIds = await _db.Announcements
                .AsNoTracking()
                .Where(announcement => !_db.AnnouncementReadReceipts.Any(receipt =>
                    receipt.AppUserId == appUserId &&
                    receipt.AnnouncementId == announcement.Id))
                .Select(announcement => announcement.Id)
                .ToListAsync(cancellationToken);

            if (unreadAnnouncementIds.Count > 0)
            {
                var readAtUtc = DateTime.UtcNow;
                _db.AnnouncementReadReceipts.AddRange(
                    unreadAnnouncementIds.Select(announcementId =>
                        new AnnouncementReadReceipt
                        {
                            AppUserId = appUserId,
                            AnnouncementId = announcementId,
                            ReadAtUtc = readAtUtc
                        }));

                await _db.SaveChangesAsync(cancellationToken);
            }

            return NoContent();
        }

        private bool TryGetAppUserId(out int appUserId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out appUserId);
        }
    }
}
