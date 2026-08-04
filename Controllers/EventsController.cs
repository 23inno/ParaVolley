using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index(string? search, string? type, string? status)
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

            ViewBag.TotalCount = await _context.Events.CountAsync();
            ViewBag.Search = search;
            ViewBag.SelectedType = type;
            ViewBag.SelectedStatus = status;

            return View(await query.OrderBy(e => e.Date).ToListAsync());
        }

        // GET: Events/Export - downloads the current filtered list as a CSV file
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
        public async Task<IActionResult> Create([Bind("Title,Date,Time,Location,Type,Participants,Status,Description")] Event ev)
        {
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", ev);
            }
            return View(ev);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Time,Location,Type,Participants,Status,Description")] Event ev)
        {
            if (id != ev.Id) return NotFound();

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
                    if (!EventExists(ev.Id)) return NotFound();
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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", ev);
            }
            return View(ev);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events.FindAsync(id);
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

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
