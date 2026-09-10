using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Events
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            string? type,
            string? status,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);

            var query = _context.Events
                .AsNoTracking()
                .AsQueryable();

            var canManageEvents =
                User.IsInRole(nameof(AppUserRole.Admin)) ||
                User.IsInRole(nameof(AppUserRole.Coach));

            if (!canManageEvents)
            {
                var today = DateTime.Today;

                query = query.Where(e =>
                    e.Status == EventStatus.Upcoming &&
                    e.Date >= today);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Title.Contains(search) || e.Location.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<EventType>(type, out var parsedType))
            {
                query = query.Where(e => e.Type == parsedType);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatus>(status, out var parsedStatus))
            {
                query = query.Where(e => e.Status == parsedStatus);
            }

            ViewBag.TotalCount = await _context.Events.CountAsync(cancellationToken);
            var filteredCount = await query.CountAsync(cancellationToken);
            var pageCount = Paging.PageCount(filteredCount, Paging.DefaultPageSize);
            page = Math.Min(page, pageCount);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;
            ViewBag.Search = search;
            ViewBag.SelectedType = type;
            ViewBag.SelectedStatus = status;

            return View(await query.OrderBy(e => e.Date).ThenBy(e => e.Id)
                .Skip((page - 1) * Paging.DefaultPageSize)
                .Take(Paging.DefaultPageSize)
                .ToListAsync(cancellationToken));
        }

        // GET: Events/Export - downloads the current filtered list as a CSV file
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public async Task<IActionResult> Export(string? search, string? type, string? status)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Title.Contains(search) || e.Location.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<EventType>(type, out var parsedType))
            {
                query = query.Where(e => e.Type == parsedType);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatus>(status, out var parsedStatus))
            {
                query = query.Where(e => e.Status == parsedStatus);
            }

            var events = await query.OrderBy(e => e.Date).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Title,Date,Time,Location,Type,Participants,Status,Description");
            foreach (var e in events)
            {
                sb.AppendLine($"\"{e.Title}\",{e.Date:yyyy-MM-dd},\"{e.Time}\",\"{e.Location}\",\"{e.Type}\",{e.Participants},\"{e.Status}\",\"{e.Description}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"events_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Events/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", ev);
            }
            return View(ev);
        }

        // GET: Events/Create
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", new Event { Date = DateTime.Today });
            }
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public async Task<IActionResult> Create([Bind("Title,Date,Time,Location,Type,Participants,Status,Description")] Event ev)
        {
            if (User.IsInRole(nameof(AppUserRole.Coach))) ev.Type = EventType.Practice;
            if (ModelState.IsValid)
            {
                _context.Add(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Event \"{ev.Title}\" was created.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", ev);
            }
            return View(ev);
        }

        // GET: Events/Edit/5
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (User.IsInRole(nameof(AppUserRole.Coach)) && ev.Type != EventType.Practice) return Forbid();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", ev);
            }
            return View(ev);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Time,Location,Type,Participants,Status,Description")] Event ev)
        {
            if (id != ev.Id) return NotFound();

            var existing = await _context.Events.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id);
            if (existing == null) return NotFound();
            if (User.IsInRole(nameof(AppUserRole.Coach)) && existing.Type != EventType.Practice) return Forbid();
            if (User.IsInRole(nameof(AppUserRole.Coach))) ev.Type = EventType.Practice;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ev);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Event \"{ev.Title}\" was updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await EventExistsAsync(ev.Id)) return NotFound();
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
                return PartialView("_EditPartial", ev);
            }
            return View(ev);
        }

        // GET: Events/Delete/5
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();
            if (User.IsInRole(nameof(AppUserRole.Coach)) && ev.Type != EventType.Practice) return Forbid();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", ev);
            }
            return View(ev);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev != null && User.IsInRole(nameof(AppUserRole.Coach)) && ev.Type != EventType.Practice) return Forbid();
            if (ev != null)
            {
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Event was deleted.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> EventExistsAsync(int id) =>
            _context.Events.AnyAsync(e => e.Id == id);

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
