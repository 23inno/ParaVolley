using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AnnouncementsController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

            var latestAnnouncement = await _context.Announcements.OrderByDescending(a => a.Date).FirstOrDefaultAsync();
            if (latestAnnouncement != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = latestAnnouncement.Title,
                    Date = latestAnnouncement.Date,
                    Tag = latestAnnouncement.Category.ToString(),
                    Url = Url.Action("Details", "Announcements", new { id = latestAnnouncement.Id }) ?? "#",
                    IsModal = true,
                });
            }

            var latestMatch = await _context.Matches
                .Where(m => m.Date <= DateTime.Today)
                .OrderByDescending(m => m.Date)
                .FirstOrDefaultAsync();
            if (latestMatch != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = $"{latestMatch.TeamA} vs {latestMatch.TeamB} - {latestMatch.Status}",
                    Date = latestMatch.Date,
                    Tag = "Match",
                    Url = Url.Action("Details", "Matches", new { id = latestMatch.Id }) ?? "#",
                    IsModal = true,
                });
            }

            var latestEvent = await _context.Events
                .Where(e => e.Date <= DateTime.Today)
                .OrderByDescending(e => e.Date)
                .FirstOrDefaultAsync();
            if (latestEvent != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = latestEvent.Title,
                    Date = latestEvent.Date,
                    Tag = "Event",
                    Url = Url.Action("Details", "Events", new { id = latestEvent.Id }) ?? "#",
                    IsModal = true,
                });
            }

            var latestReport = User.IsInRole(nameof(AppUserRole.Admin))
                ? await _context.Reports.OrderByDescending(r => r.Date).FirstOrDefaultAsync()
                : null;
            if (latestReport != null)
            {
                items.Add(new RecentUpdateItem
                {
                    Title = latestReport.Title,
                    Date = latestReport.Date,
                    Tag = "Report",
                    Url = Url.Action("Details", "Reports", new { id = latestReport.Id }) ?? "#",
                    IsModal = true,
                });
            }

            return items.OrderByDescending(i => i.Date).Take(4).ToList();
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

                // Best-effort: notify every subscriber. Failures here (e.g. no SMTP
                // configured) must never block publishing the announcement itself.
                _ = NotifySubscribersAsync(announcement);

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

        private async Task NotifySubscribersAsync(Announcement announcement)
        {
            var subscribers = await _context.Subscribers.ToListAsync();
            var subject = $"New announcement: {announcement.Title}";
            var body = $@"
                <p>Hi there,</p>
                <p>ParaVolley Mpumalanga just published a new {announcement.Category.ToString().ToLower()}:</p>
                <h3>{announcement.Title}</h3>
                <p>{announcement.Excerpt}</p>
                <p><em>You're receiving this because you subscribed to News &amp; Announcements updates.</em></p>";

            foreach (var sub in subscribers)
            {
                await _emailService.SendAsync(sub.Email, subject, body);
            }
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

            var existing = await _context.Subscribers.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
            if (existing != null)
            {
                TempData["Success"] = "You're already subscribed to News & Announcements.";
                return RedirectToAction(nameof(Index));
            }

            _context.Subscribers.Add(new Subscriber { Email = email, SubscribedAt = DateTime.Now });
            await _context.SaveChangesAsync();

            var sent = await _emailService.SendAsync(
                email,
                "You're subscribed to ParaVolley Mpumalanga News",
                "<p>Thanks for subscribing!</p><p>You'll now receive an email every time we publish a new announcement, event, or news update.</p>");

            TempData["Success"] = sent
                ? "Subscribed! Check your inbox for a confirmation email."
                : "Subscribed! (Confirmation email not sent - this demo has no SMTP server configured yet. See appsettings.json > EmailSettings.)";

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
