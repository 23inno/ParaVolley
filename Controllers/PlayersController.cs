using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly IPasswordHasher<AppUser> _passwordHasher;

        public PlayersController(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
        public async Task<IActionResult> Details(
            int? id,
            CancellationToken cancellationToken = default)
        {
            if (id == null) return NotFound();

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (player == null) return NotFound();

            if (User.IsInRole(nameof(AppUserRole.Coach)))
            {
                return View("CoachDetails", new CoachPlayerViewModel
                {
                    Id = player.Id,
                    Name = player.Name,
                    Position = player.Position,
                    Team = player.Team,
                    Status = player.Status,
                    Matches = player.Matches
                });
            }

            ViewBag.MobileAppUser = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user =>
                        user.Role == AppUserRole.Player &&
                        user.PlayerId == player.Id,
                    cancellationToken);

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", player);
            }

            return View(player);
        }

        // POST: Players/SaveMobileAccess/5
        // Creates a Player AppUser when one does not exist, or resets the
        // linked mobile password and re-activates the account when it does.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> SaveMobileAccess(
            int id,
            string? password,
            string? confirmPassword,
            CancellationToken cancellationToken = default)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

            if (player == null)
            {
                return NotFound();
            }

            if (player.Status != PlayerStatus.Active)
            {
                TempData["Error"] =
                    "Activate this player record before enabling mobile app access.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var normalizedEmail =
                (player.Email ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedEmail) ||
                normalizedEmail.Length > 200 ||
                !new EmailAddressAttribute().IsValid(normalizedEmail))
            {
                TempData["Error"] =
                    "The player must have a valid email address before mobile app access can be configured.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrEmpty(password) ||
                password.Length < 8 ||
                password.Length > 128)
            {
                TempData["Error"] =
                    "The mobile app password must be between 8 and 128 characters.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (password != confirmPassword)
            {
                TempData["Error"] =
                    "The mobile app password and confirmation do not match.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var linkedAccount = await _context.AppUsers
                .FirstOrDefaultAsync(
                    user => user.PlayerId == id,
                    cancellationToken);

            var emailAccount = await _context.AppUsers
                .FirstOrDefaultAsync(
                    user => user.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (linkedAccount != null &&
                emailAccount != null &&
                linkedAccount.Id != emailAccount.Id)
            {
                TempData["Error"] =
                    "Mobile access could not be configured because this player email is already used by another authentication account.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var account = linkedAccount ?? emailAccount;
            var created = account == null;

            if (account != null)
            {
                if (account.Role != AppUserRole.Player ||
                    (account.PlayerId.HasValue &&
                     account.PlayerId.Value != id))
                {
                    TempData["Error"] =
                        "Mobile access could not be configured because the email belongs to a different authentication account.";

                    return RedirectToAction(nameof(Details), new { id });
                }

                account.Email = normalizedEmail;
                account.NormalizedEmail = normalizedEmail;
                account.PlayerId = id;
                account.IsActive = true;
            }
            else
            {
                account = new AppUser
                {
                    Email = normalizedEmail,
                    NormalizedEmail = normalizedEmail,
                    Role = AppUserRole.Player,
                    IsActive = true,
                    PlayerId = id
                };

                _context.AppUsers.Add(account);
            }

            account.PasswordHash =
                _passwordHasher.HashPassword(
                    account,
                    password);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (DatabaseConflictClassifier.IsUniqueViolation(
                    exception,
                    _context.Database,
                    "IX_AppUsers_NormalizedEmail"))
            {
                TempData["Error"] =
                    "Mobile access could not be configured because this email is already used by another authentication account.";

                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = created
                ? $"Mobile app access was created and activated for {player.Email}."
                : $"Mobile app password was reset and access activated for {player.Email}.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Players/ToggleMobileAccess/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ToggleMobileAccess(
            int id,
            CancellationToken cancellationToken = default)
        {
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

            if (player == null)
            {
                return NotFound();
            }

            var account = await _context.AppUsers
                .FirstOrDefaultAsync(
                    user =>
                        user.Role == AppUserRole.Player &&
                        user.PlayerId == id,
                    cancellationToken);

            if (account == null)
            {
                TempData["Error"] =
                    "This player does not have a mobile app account yet.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (!account.IsActive &&
                player.Status != PlayerStatus.Active)
            {
                TempData["Error"] =
                    "Activate the player record before re-activating mobile app access.";

                return RedirectToAction(nameof(Details), new { id });
            }

            account.IsActive = !account.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            TempData["Success"] = account.IsActive
                ? $"Mobile app access was activated for {account.Email}."
                : $"Mobile app access was deactivated for {account.Email}.";

            return RedirectToAction(nameof(Details), new { id });
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
