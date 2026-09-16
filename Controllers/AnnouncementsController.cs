using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly NotificationDeliveryService _notificationDelivery;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnnouncementsController> _logger;

        public AnnouncementsController(
            ApplicationDbContext context,
            EmailService emailService,
            IConfiguration configuration,
            ILogger<NotificationDeliveryService> notificationLogger,
            IServiceScopeFactory scopeFactory,
            ILogger<AnnouncementsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _notificationDelivery = new NotificationDeliveryService(
                context,
                emailService,
                configuration,
                notificationLogger);
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // GET: Announcements
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            string? category,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);
            var query = _context.Announcements.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Title.Contains(search) || a.Excerpt.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<AnnouncementCategory>(category, out var parsedCategory))
            {
                query = query.Where(a => a.Category == parsedCategory);
            }

            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;

            var filteredCount = await query.CountAsync(cancellationToken);
            var pageCount = Paging.PageCount(filteredCount, Paging.DefaultPageSize);
            page = Math.Min(page, pageCount);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;

            ViewBag.Sponsors = await _context.Sponsors
                .OrderBy(s => s.Tier)
                .ThenBy(s => s.Name)
                .Take(50)
                .ToListAsync(cancellationToken);

            ViewBag.SubscriberCount = await _context.Subscribers.CountAsync();

            ViewBag.RecentUpdates = await BuildRecentUpdatesAsync();

            return View(await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.Date)
                .ThenBy(a => a.Id)
                .Skip((page - 1) * Paging.DefaultPageSize)
                .Take(Paging.DefaultPageSize)
                .ToListAsync(cancellationToken));
        }

        // Builds a real, live "recent activity" feed from actual data across the
        // app (most recent Announcement, Match, Event, and Report), each linking
        // to the real record - replaces the old static placeholder list.
        private async Task<List<RecentUpdateItem>> BuildRecentUpdatesAsync()
        {
            var items = new List<RecentUpdateItem>();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var recentCutoff = today.AddDays(-30);

            var latestAnnouncement = await _context.Announcements
                .AsNoTracking()
                .Where(a =>
                    a.Date >= recentCutoff &&
                    a.Date < tomorrow)
                .OrderByDescending(a => a.Date)
                .FirstOrDefaultAsync();

            if (latestAnnouncement != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = latestAnnouncement.Title,
                    Date = latestAnnouncement.Date,
                    Tag = latestAnnouncement.Category.ToString(),
                    Url = Url.Action(
                        "Details",
                        "Announcements",
                        new { id = latestAnnouncement.Id }) ?? "#",
                    IsModal = true
                });
            }

            var latestMatch = await _context.Matches
                .AsNoTracking()
                .Where(m =>
                    m.Date >= recentCutoff &&
                    m.Date < tomorrow)
                .OrderByDescending(m => m.Date)
                .FirstOrDefaultAsync();

            if (latestMatch != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title =
                        $"{latestMatch.TeamA} vs {latestMatch.TeamB} - {latestMatch.Status}",
                    Date = latestMatch.Date,
                    Tag = "Match",
                    Url = Url.Action(
                        "Details",
                        "Matches",
                        new { id = latestMatch.Id }) ?? "#",
                    IsModal = true
                });
            }

            var latestEvent = await _context.Events
                .AsNoTracking()
                .Where(e =>
                    e.Date >= recentCutoff &&
                    e.Date < tomorrow)
                .OrderByDescending(e => e.Date)
                .FirstOrDefaultAsync();

            if (latestEvent != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = latestEvent.Title,
                    Date = latestEvent.Date,
                    Tag = "Event",
                    Url = Url.Action(
                        "Details",
                        "Events",
                        new { id = latestEvent.Id }) ?? "#",
                    IsModal = true
                });
            }

            var latestReport = User.IsInRole(nameof(AppUserRole.Admin))
                ? await _context.Reports
                    .AsNoTracking()
                    .Where(r =>
                        r.Date >= recentCutoff &&
                        r.Date < tomorrow)
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefaultAsync()
                : null;

            if (latestReport != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = latestReport.Title,
                    Date = latestReport.Date,
                    Tag = "Report",
                    Url = Url.Action(
                        "Details",
                        "Reports",
                        new { id = latestReport.Id }) ?? "#",
                    IsModal = true
                });
            }

            return items
                .OrderByDescending(item => item.Date)
                .Take(4)
                .ToList();
        }

        // POST: Announcements/TogglePin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> TogglePin(int id, string? search, string? category)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                announcement.IsPinned = !announcement.IsPinned;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { search, category });
        }

        // GET: Announcements/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id);
            if (announcement == null) return NotFound();

            // Increment view count each time it's read (mirrors the original app's "views" counter)
            announcement.Views += 1;
            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", announcement);
            }
            return View(announcement);
        }

        // GET: Announcements/Create
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", new Announcement { Date = DateTime.Today, Author = "Admin User" });
            }
            return View();
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Create([Bind("Title,Excerpt,Content,Author,Date,Category,IsPinned,Views")] Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Announcement \"{announcement.Title}\" was published.";

                QueueAnnouncementNotifications(announcement);

                if (IsAjaxRequest())
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", announcement);
            }
            return View(announcement);
        }

        private void QueueAnnouncementNotifications(Announcement announcement)
        {
            var category = announcement.Category
                .ToString()
                .ToLowerInvariant();

            var subject = $"New announcement: {announcement.Title}";
            var encodedTitle =
                System.Net.WebUtility.HtmlEncode(announcement.Title);
            var encodedExcerpt =
                System.Net.WebUtility.HtmlEncode(announcement.Excerpt);
            var encodedCategory =
                System.Net.WebUtility.HtmlEncode(category);

            var subscriberBody = $@"
                <p>Hi there,</p>
                <p>ParaVolley Mpumalanga just published a new {encodedCategory}:</p>
                <h3>{encodedTitle}</h3>
                <p>{encodedExcerpt}</p>
                <p><em>You're receiving this because you subscribed to News &amp; Announcements updates.</em></p>";

            BackgroundNotificationDispatcher.QueueSubscriberBroadcast(
                _scopeFactory,
                _logger,
                subject,
                subscriberBody,
                $"announcement {announcement.Id} subscribers");

            var textBody =
                $"ParaVolley Mpumalanga published a new {category}: " +
                $"{announcement.Title}. {announcement.Excerpt}";

            var playerHtmlBody = $@"
                <p>ParaVolley Mpumalanga published a new {encodedCategory}.</p>
                <h3>{encodedTitle}</h3>
                <p>{encodedExcerpt}</p>";

            BackgroundNotificationDispatcher.QueuePlayerNotification(
                _scopeFactory,
                _logger,
                "announcement",
                subject,
                textBody,
                playerHtmlBody,
                $"announcement {announcement.Id} players");
        }

        // POST: Announcements/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Subscribe(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Index));
            }

            email = email.Trim();

            var normalizedEmail = email.ToLower();

            var existing = await _context.Subscribers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Email.ToLower() == normalizedEmail);

            if (existing != null)
            {
                TempData["Success"] =
                    "You're already subscribed to News & Announcements.";

                return RedirectToAction(nameof(Index));
            }

            _context.Subscribers.Add(new Subscriber
            {
                Email = email,
                SubscribedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            BackgroundNotificationDispatcher.QueueEmail(
                _scopeFactory,
                _logger,
                email,
                "You're subscribed to ParaVolley Mpumalanga News",
                "<p>Thanks for subscribing!</p><p>You'll now receive an email every time we publish a new announcement, event, or news update.</p>",
                $"subscriber confirmation {email}");

            TempData["Success"] =
                "Subscribed successfully. A confirmation email is being sent.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Announcements/Edit/5
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", announcement);
            }
            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Excerpt,Content,Author,Date,Category,IsPinned,Views")] Announcement announcement)
        {
            if (id != announcement.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Announcement \"{announcement.Title}\" was updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await AnnouncementExistsAsync(announcement.Id)) return NotFound();
                    throw;
                }

                if (IsAjaxRequest())
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", announcement);
            }
            return View(announcement);
        }

        // GET: Announcements/Delete/5
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id);
            if (announcement == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", announcement);
            }
            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Announcement was deleted.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> AnnouncementExistsAsync(int id) =>
            _context.Announcements.AnyAsync(e => e.Id == id);
    }
}
