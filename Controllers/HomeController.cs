using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalPlayers = await _context.Players.CountAsync(),
                ActivePlayers = await _context.Players.CountAsync(p => p.Status == PlayerStatus.Active),
                TotalCoaches = await _context.Coaches.CountAsync(),
                UpcomingEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Upcoming),
                UpcomingMatches = await _context.Matches.CountAsync(m => m.Status == MatchStatus.Scheduled),
                TotalAnnouncements = await _context.Announcements.CountAsync(),

                NextEvents = await _context.Events
                    .Where(e => e.Status == EventStatus.Upcoming)
                    .OrderBy(e => e.Date)
                    .Take(4)
                    .ToListAsync(),

                NextMatches = await _context.Matches
                    .Where(m => m.Status == MatchStatus.Scheduled)
                    .OrderBy(m => m.Date)
                    .Take(4)
                    .ToListAsync(),

                RecentAnnouncements = await _context.Announcements
                    .OrderByDescending(a => a.IsPinned)
                    .ThenByDescending(a => a.Date)
                    .Take(4)
                    .ToListAsync(),

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

            return View(vm);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
