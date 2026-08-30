using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Players
        public async Task<IActionResult> Index(
            string? search,
            string? team,
            string? position,
            string? status,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);
            var query = _context.Players.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(p => p.Team == team);
            }

            if (!string.IsNullOrWhiteSpace(position))
            {
                query = query.Where(p => p.Position == position);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PlayerStatus>(status, out var parsedStatus))
            {
                query = query.Where(p => p.Status == parsedStatus);
            }

            var totalCount = await _context.Players.CountAsync(cancellationToken);
            var filteredCount = await query.CountAsync(cancellationToken);
            var pageCount = Paging.PageCount(filteredCount, Paging.DefaultPageSize);
            page = Math.Min(page, pageCount);

            ViewBag.Teams = await _context.Players.AsNoTracking()
                .Select(p => p.Team).Distinct().OrderBy(t => t)
                .ToListAsync(cancellationToken);
            ViewBag.Positions = await _context.Players.AsNoTracking()
                .Select(p => p.Position).Distinct().OrderBy(p => p)
                .ToListAsync(cancellationToken);
            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;
            ViewBag.Search = search;
            ViewBag.SelectedTeam = team;
            ViewBag.SelectedPosition = position;
            ViewBag.SelectedStatus = status;

            var pagedQuery = query.OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip((page - 1) * Paging.DefaultPageSize)
                .Take(Paging.DefaultPageSize);
            if (User.IsInRole(nameof(AppUserRole.Coach)))
            {
                return View("CoachIndex", await pagedQuery
                    .Select(p => new CoachPlayerViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Position = p.Position,
                        Team = p.Team,
                        Status = p.Status,
                        Matches = p.Matches
                    })
                    .ToListAsync(cancellationToken));
            }
            return View(await pagedQuery.ToListAsync(cancellationToken));
        }

        // GET: Players/Export - downloads the current filtered list as a CSV file
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Export(string? search, string? team, string? position, string? status)
        {
            var query = _context.Players.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Email.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(p => p.Team == team);
            }
            if (!string.IsNullOrWhiteSpace(position))
            {
                query = query.Where(p => p.Position == position);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PlayerStatus>(status, out var parsedStatus))
            {
                query = query.Where(p => p.Status == parsedStatus);
            }

            var players = await query.OrderBy(p => p.Name).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Name,Position,Team,Age,Matches,Status,Email,Phone,Disability");
            foreach (var p in players)
            {
                sb.AppendLine($"\"{p.Name}\",\"{p.Position}\",\"{p.Team}\",{p.Age},{p.Matches},\"{p.Status}\",\"{p.Email}\",\"{p.Phone}\",\"{p.Disability}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"players_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Players/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            if (User.IsInRole(nameof(AppUserRole.Coach)))
            {
                return View("CoachDetails", new CoachPlayerViewModel
                {
                    Id = player.Id, Name = player.Name, Position = player.Position,
                    Team = player.Team, Status = player.Status, Matches = player.Matches
                });
            }

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", player);
            }
            return View(player);
        }

        // GET: Players/Create
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", new Player());
            }
            return View();
        }

        // POST: Players/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Create([Bind("Name,Position,Team,Status,Age,Matches,Email,Phone,Disability")] Player player)
        {
            if (ModelState.IsValid)
            {
                _context.Add(player);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Player \"{player.Name}\" was created.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", player);
            }
            return View(player);
        }

        // GET: Players/Edit/5
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", player);
            }
            return View(player);
        }

        // POST: Players/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Position,Team,Status,Age,Matches,Email,Phone,Disability")] Player player)
        {
            if (id != player.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(player);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Player \"{player.Name}\" was updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await PlayerExistsAsync(player.Id)) return NotFound();
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
                return PartialView("_EditPartial", player);
            }
            return View(player);
        }

        // GET: Players/Delete/5
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", player);
            }
            return View(player);
        }

        // POST: Players/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player != null)
            {
                _context.Players.Remove(player);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Player was deleted.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> PlayerExistsAsync(int id) =>
            _context.Players.AnyAsync(e => e.Id == id);

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
