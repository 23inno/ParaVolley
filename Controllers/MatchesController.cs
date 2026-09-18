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
    public class MatchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationDeliveryService _notificationDelivery;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MatchesController> _logger;

        public MatchesController(
            ApplicationDbContext context,
            EmailService emailService,
            IConfiguration configuration,
            ILogger<NotificationDeliveryService> notificationLogger,
            IServiceScopeFactory scopeFactory,
            ILogger<MatchesController> logger)
        {
            _context = context;
            _notificationDelivery = new NotificationDeliveryService(
                context,
                emailService,
                configuration,
                notificationLogger);
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // GET: Matches
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            string? tournament,
            string? status,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);
            var query = _context.Matches.AsNoTracking().AsQueryable();

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

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var matchCounts = await _context.Matches
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),

                    Upcoming = group.Count(match =>
                        match.Status == MatchStatus.Scheduled &&
                        match.Date >= today),

                    Completed = group.Count(match =>
                        match.Status == MatchStatus.Completed),

                    InProgress = group.Count(match =>
                        match.Status == MatchStatus.InProgress &&
                        match.Date >= today &&
                        match.Date < tomorrow)
                })
                .SingleOrDefaultAsync(cancellationToken);

            ViewBag.TotalCount = matchCounts?.Total ?? 0;
            ViewBag.UpcomingCount = matchCounts?.Upcoming ?? 0;
            ViewBag.CompletedCount = matchCounts?.Completed ?? 0;
            ViewBag.InProgressCount = matchCounts?.InProgress ?? 0;

            var tournaments = await _context.Matches.AsNoTracking()
                .Where(match => !string.IsNullOrWhiteSpace(match.Tournament))
                .Select(match => match.Tournament)
                .Distinct()
                .OrderBy(value => value)
                .ToListAsync(cancellationToken);
            ViewBag.Tournaments = tournaments;
            ViewBag.TournamentsCount = tournaments.Count;

            var filteredCount = await query.CountAsync(cancellationToken);
            var pageCount = Paging.PageCount(filteredCount, Paging.DefaultPageSize);
            page = Math.Min(page, pageCount);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;

            ViewBag.Search = search;
            ViewBag.SelectedTournament = tournament;
            ViewBag.SelectedStatus = status;

            return View(await query.OrderBy(m => m.Date).ThenBy(m => m.Id)
                .Skip((page - 1) * Paging.DefaultPageSize)
                .Take(Paging.DefaultPageSize)
                .ToListAsync(cancellationToken));
        }

        // GET: Matches/Export - downloads the current filtered list as a CSV file
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
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
        [AllowAnonymous]
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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Create(
            CancellationToken cancellationToken = default)
        {
            var settings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            var activeSeason =
                int.TryParse(settings?.ActiveSeason, out var configuredSeason)
                    ? configuredSeason
                    : DateTime.Today.Year;

            var defaultDate = DateTime.Today.Year == activeSeason
                ? DateTime.Today
                : new DateTime(activeSeason, 1, 1);

            var match = new Match
            {
                Date = defaultDate,
                TeamA = string.IsNullOrWhiteSpace(settings?.DefaultTeamName)
                    ? "ParaVolley Mpumalanga"
                    : settings.DefaultTeamName
            };

            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", match);
            }

            return View(match);
        }

        // POST: Matches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Create([Bind("TeamA,TeamB,Date,Time,Venue,Tournament,Status,ScoreA,ScoreB")] Match match)
        {
            if (ModelState.IsValid)
            {
                _context.Add(match);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Match \"{match.TeamA} vs {match.TeamB}\" was created.";

                if (ShouldNotifyMatchResult(previous: null, match))
                {
                    QueueMatchResult(match);
                }

                if (IsUpcomingMatch(match))
                {
                    QueueUpcomingMatch(match);
                }

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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TeamA,TeamB,Date,Time,Venue,Tournament,Status,ScoreA,ScoreB")] Match match)
        {
            if (id != match.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var previousMatch = await _context.Matches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == id);

                if (previousMatch == null)
                {
                    return NotFound();
                }

                var shouldNotifyResult =
                    ShouldNotifyMatchResult(previousMatch, match);

                try
                {
                    _context.Update(match);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Match \"{match.TeamA} vs {match.TeamB}\" was updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await MatchExistsAsync(match.Id)) return NotFound();
                    throw;
                }

                if (shouldNotifyResult)
                {
                    QueueMatchResult(match);
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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
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

        private static bool ShouldNotifyMatchResult(
            Match? previous,
            Match current)
        {
            if (!HasPublishableResult(current))
            {
                return false;
            }

            if (previous == null)
            {
                return true;
            }

            if (!HasPublishableResult(previous))
            {
                return true;
            }

            return previous.ScoreA != current.ScoreA ||
                   previous.ScoreB != current.ScoreB;
        }

        private static bool HasPublishableResult(Match match) =>
            match.Status == MatchStatus.Completed &&
            match.ScoreA.HasValue &&
            match.ScoreB.HasValue;

        private static bool IsUpcomingMatch(Match match) =>
            match.Status == MatchStatus.Scheduled &&
            match.Date.Date >= DateTime.Today;

        private void QueueUpcomingMatch(Match match)
        {
            var subject =
                $"Upcoming match: {match.TeamA} vs {match.TeamB}";

            var details = new List<string>
            {
                $"Date: {match.Date:dd MMM yyyy}"
            };

            if (!string.IsNullOrWhiteSpace(match.Time))
            {
                details.Add($"Time: {match.Time.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(match.Venue))
            {
                details.Add($"Venue: {match.Venue.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(match.Tournament))
            {
                details.Add($"Tournament: {match.Tournament.Trim()}");
            }

            var textBody =
                $"{match.TeamA} vs {match.TeamB}. {string.Join(" • ", details)}";

            var encodedTeamA =
                System.Net.WebUtility.HtmlEncode(match.TeamA);
            var encodedTeamB =
                System.Net.WebUtility.HtmlEncode(match.TeamB);
            var encodedDate =
                System.Net.WebUtility.HtmlEncode(match.Date.ToString("dd MMM yyyy"));
            var encodedTime =
                System.Net.WebUtility.HtmlEncode(match.Time ?? string.Empty);
            var encodedVenue =
                System.Net.WebUtility.HtmlEncode(match.Venue ?? string.Empty);
            var encodedTournament =
                System.Net.WebUtility.HtmlEncode(match.Tournament ?? string.Empty);

            var htmlDetails = new List<string>
            {
                $"<strong>Date:</strong> {encodedDate}"
            };

            if (!string.IsNullOrWhiteSpace(match.Time))
            {
                htmlDetails.Add($"<strong>Time:</strong> {encodedTime}");
            }

            if (!string.IsNullOrWhiteSpace(match.Venue))
            {
                htmlDetails.Add($"<strong>Venue:</strong> {encodedVenue}");
            }

            if (!string.IsNullOrWhiteSpace(match.Tournament))
            {
                htmlDetails.Add($"<strong>Tournament:</strong> {encodedTournament}");
            }

            var htmlBody = $@"
                <p>A new ParaVolley match has been scheduled:</p>
                <h3>{encodedTeamA} vs {encodedTeamB}</h3>
                <p>{string.Join("<br />", htmlDetails)}</p>";

            BackgroundNotificationDispatcher.QueuePlayerNotification(
                _scopeFactory,
                _logger,
                "upcoming_match",
                subject,
                textBody,
                htmlBody,
                $"upcoming match {match.Id}");
        }

        private void QueueMatchResult(Match match)
        {
            var score = $"{match.ScoreA}-{match.ScoreB}";
            var subject =
                $"Match result: {match.TeamA} {score} {match.TeamB}";

            var contextParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(match.Tournament))
            {
                contextParts.Add(match.Tournament.Trim());
            }
            if (!string.IsNullOrWhiteSpace(match.Venue))
            {
                contextParts.Add(match.Venue.Trim());
            }

            var contextText = contextParts.Count == 0
                ? string.Empty
                : $" ({string.Join(" - ", contextParts)})";

            var textBody =
                $"Final result: {match.TeamA} {score} {match.TeamB}{contextText}.";

            var encodedTeamA =
                System.Net.WebUtility.HtmlEncode(match.TeamA);
            var encodedTeamB =
                System.Net.WebUtility.HtmlEncode(match.TeamB);
            var encodedTournament =
                System.Net.WebUtility.HtmlEncode(match.Tournament ?? string.Empty);
            var encodedVenue =
                System.Net.WebUtility.HtmlEncode(match.Venue ?? string.Empty);

            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(match.Tournament))
            {
                details.Add($"<strong>Tournament:</strong> {encodedTournament}");
            }
            if (!string.IsNullOrWhiteSpace(match.Venue))
            {
                details.Add($"<strong>Venue:</strong> {encodedVenue}");
            }

            var htmlDetails = details.Count == 0
                ? string.Empty
                : $"<p>{string.Join("<br />", details)}</p>";

            var htmlBody = $@"
                <p>ParaVolley Mpumalanga match result:</p>
                <h3>{encodedTeamA} {score} {encodedTeamB}</h3>
                {htmlDetails}";

            BackgroundNotificationDispatcher.QueuePlayerNotification(
                _scopeFactory,
                _logger,
                "match_results",
                subject,
                textBody,
                htmlBody,
                $"match {match.Id} result");
        }

        private Task<bool> MatchExistsAsync(int id) =>
            _context.Matches.AnyAsync(e => e.Id == id);

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
