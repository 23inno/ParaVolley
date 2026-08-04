using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class MatchesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MatchesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Matches
        public async Task<IActionResult> Index(string? search, string? tournament, string? status)
        {
            var query = _context.Matches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmed = search.Trim();
                var idPart = trimmed.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? trimmed[1..] : trimmed;
                int? searchId = int.TryParse(idPart, out var parsedId) ? parsedId : null;

                query = query.Where(m => m.Tournament.Contains(search) || (searchId != null && m.Id == searchId));
            }

            if (!string.IsNullOrWhiteSpace(tournament))
            {
                query = query.Where(m => m.Tournament == tournament);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MatchStatus>(status, out var parsedStatus))
            {
                query = query.Where(m => m.Status == parsedStatus);
            }

            var allMatches = await _context.Matches.ToListAsync();
            ViewBag.TotalCount = allMatches.Count;
            ViewBag.UpcomingCount = allMatches.Count(m => m.Status == MatchStatus.Scheduled);
            ViewBag.CompletedCount = allMatches.Count(m => m.Status == MatchStatus.Completed);
            ViewBag.InProgressCount = allMatches.Count(m => m.Status == MatchStatus.InProgress);
            ViewBag.TournamentsCount = allMatches.Select(m => m.Tournament).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().Count();
            ViewBag.Tournaments = allMatches.Select(m => m.Tournament).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();

            ViewBag.Search = search;
            ViewBag.SelectedTournament = tournament;
            ViewBag.SelectedStatus = status;

            return View(await query.OrderBy(m => m.Date).ToListAsync());
        }

        // GET: Matches/Export - downloads the current filtered list as a CSV file
        public async Task<IActionResult> Export(string? search, string? tournament, string? status)
        {
            var query = _context.Matches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmed = search.Trim();
                var idPart = trimmed.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? trimmed[1..] : trimmed;
                int? searchId = int.TryParse(idPart, out var parsedId) ? parsedId : null;

                query = query.Where(m => m.Tournament.Contains(search) || (searchId != null && m.Id == searchId));
            }
            if (!string.IsNullOrWhiteSpace(tournament))
            {
                query = query.Where(m => m.Tournament == tournament);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MatchStatus>(status, out var parsedStatus))
            {
                query = query.Where(m => m.Status == parsedStatus);
            }

            var matches = await query.OrderBy(m => m.Date).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,TeamA,TeamB,Score,Date,Time,Venue,Tournament,Status");
            foreach (var m in matches)
            {
                var score = m.ScoreA.HasValue && m.ScoreB.HasValue ? $"{m.ScoreA}-{m.ScoreB}" : "";
                sb.AppendLine($"\"M{m.Id:000}\",\"{m.TeamA}\",\"{m.TeamB}\",\"{score}\",{m.Date:yyyy-MM-dd},\"{m.Time}\",\"{m.Venue}\",\"{m.Tournament}\",\"{m.Status}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"matches_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", match);
            }
            return View(match);
        }

        // GET: Matches/Create
        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", new Match { Date = DateTime.Today });
            }
            return View();
        }

        // POST: Matches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TeamA,TeamB,Date,Time,Venue,Tournament,Status,ScoreA,ScoreB")] Match match)
        {
            if (ModelState.IsValid)
            {
                _context.Add(match);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Match \"{match.TeamA} vs {match.TeamB}\" was created.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", match);
            }
            return View(match);
        }

        // GET: Matches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", match);
            }
            return View(match);
        }

        // POST: Matches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TeamA,TeamB,Date,Time,Venue,Tournament,Status,ScoreA,ScoreB")] Match match)
        {
            if (id != match.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(match);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Match \"{match.TeamA} vs {match.TeamB}\" was updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchExists(match.Id)) return NotFound();
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
                return PartialView("_EditPartial", match);
            }
            return View(match);
        }

        // GET: Matches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", match);
            }
            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match != null)
            {
                _context.Matches.Remove(match);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Match was deleted.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MatchExists(int id)
        {
            return _context.Matches.Any(e => e.Id == id);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
