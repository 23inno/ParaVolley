using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            ViewBag.UpcomingEvents = await _context.Events.AsNoTracking()
                .Where(e =>
                    e.Status == EventStatus.Upcoming &&
                    e.Date >= today)
                .OrderBy(e => e.Date).Take(3).ToListAsync(cancellationToken);
            ViewBag.RecentAnnouncements = await _context.Announcements.AsNoTracking()
                .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.Date)
                .Take(3).ToListAsync(cancellationToken);
            ViewBag.Sponsors = await _context.Sponsors.AsNoTracking()
                .OrderBy(s => s.Tier).ThenBy(s => s.Name)
                .Take(50).ToListAsync(cancellationToken);
            return View("PublicHome");
        }

        [AllowAnonymous]
        [HttpGet("/About")]
        public IActionResult About() => View();

        [AllowAnonymous]
        [HttpGet("/Contact")]
        public IActionResult Contact() => View();

        [AllowAnonymous]
        [HttpGet("/Results")]
        public IActionResult Results() => RedirectToAction("Index", "Matches");

        [AllowAnonymous]
        [HttpGet("/News")]
        public IActionResult News() => RedirectToAction("Index", "Announcements");

        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> AdminDashboard(
            CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var playerCounts = await _context.Players
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Active = group.Count(player =>
                        player.Status == PlayerStatus.Active)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var vm = new DashboardViewModel
            {
                TotalPlayers = playerCounts?.Total ?? 0,
                ActivePlayers = playerCounts?.Active ?? 0,
                TotalCoaches = await _context.Coaches.CountAsync(cancellationToken),
                UpcomingEvents = await _context.Events.CountAsync(
                    e => e.Status == EventStatus.Upcoming && e.Date >= today,
                    cancellationToken),
                UpcomingMatches = await _context.Matches.CountAsync(
                    m => m.Status == MatchStatus.Scheduled,
                    cancellationToken),
                TotalAnnouncements = await _context.Announcements.CountAsync(
                    cancellationToken),

                NextEvents = await _context.Events.AsNoTracking()
                    .Where(e =>
                        e.Status == EventStatus.Upcoming &&
                        e.Date >= today)
                    .OrderBy(e => e.Date)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                NextMatches = await _context.Matches.AsNoTracking()
                    .Where(m => m.Status == MatchStatus.Scheduled)
                    .OrderBy(m => m.Date)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                RecentAnnouncements = await _context.Announcements.AsNoTracking()
                    .OrderByDescending(a => a.IsPinned)
                    .ThenByDescending(a => a.Date)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                // Demo time-series for the "Player Statistics" chart. The data model
                // doesn't track historical monthly snapshots, so this is illustrative
                // data for the chart - in a production system this would come from
                // periodic snapshots of the Players table.
                PlayerStatsChart = new List<MonthlyPlayerStat>
                {
                    new() { Month = "Jan", Active = 180, New = 15, Inactive = 8 },
                    new() { Month = "Feb", Active = 130, New = 20, Inactive = 10 },
                    new() { Month = "Mar", Active = 195, New = 18, Inactive = 7 },
                    new() { Month = "Apr", Active = 230, New = 25, Inactive = 9 },
                    new() { Month = "May", Active = 250, New = 22, Inactive = 6 },
                },
            };

            return View("Index", vm);
        }

        [Authorize(Policy = AuthorizationPolicies.CoachOnly)]
        public async Task<IActionResult> CoachDashboard(
            CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var attendanceCounts = await _context.Attendances
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Present = group.Count(item =>
                        item.Status == AttendanceStatus.Present)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var vm = new CoachDashboardViewModel
            {
                ActivePlayers = await _context.Players.CountAsync(
                    p => p.Status == PlayerStatus.Active,
                    cancellationToken),
                UpcomingTraining = await _context.Events.AsNoTracking()
                    .Where(e =>
                        e.Type == EventType.Practice &&
                        e.Status == EventStatus.Upcoming &&
                        e.Date >= today)
                    .OrderBy(e => e.Date).Take(6).ToListAsync(cancellationToken),
                UpcomingMatches = await _context.Matches.AsNoTracking()
                    .Where(m => m.Status == MatchStatus.Scheduled)
                    .OrderBy(m => m.Date).Take(6).ToListAsync(cancellationToken),
                RecentAnnouncements = await _context.Announcements.AsNoTracking()
                    .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.Date)
                    .Take(5).ToListAsync(cancellationToken),
                AttendanceRecords = attendanceCounts?.Total ?? 0,
                PresentRecords = attendanceCounts?.Present ?? 0
            };
            return View(vm);
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
