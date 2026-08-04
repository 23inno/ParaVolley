using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attendance
        // Builds the full attendance analytics dashboard (stat cards, charts, session table).
        //
        // NOTE: the underlying Attendance table only tracks individual Player/Event
        // check-ins for our small 8-player demo roster, so it can't realistically
        // power a 250+ person, multi-team analytics view on its own. The figures
        // below are illustrative aggregate/demo data (clearly marked here), built
        // in the same spirit as the Dashboard's "Player Statistics" chart. Swap
        // this out for real aggregation once you're tracking full team rosters.
        public async Task<IActionResult> Index(string? search, string? team, string? status)
        {
            var vm = new AttendanceDashboardViewModel
            {
                TotalAttendance = 247,
                AttendanceRate = 87,
                ActiveSessions = 50,
                TopPerformerName = "Sarah S.",
                TopPerformerRate = 98,

                TrendsWeek = new List<MonthlyAttendanceTrend>
                {
                    new() { Label = "Mon", Actual = 44, Target = 50 },
                    new() { Label = "Tue", Actual = 47, Target = 50 },
                    new() { Label = "Wed", Actual = 41, Target = 50 },
                    new() { Label = "Thu", Actual = 48, Target = 50 },
                    new() { Label = "Fri", Actual = 45, Target = 50 },
                },
                TrendsMonth = new List<MonthlyAttendanceTrend>
                {
                    new() { Label = "Jan", Actual = 210, Target = 245 },
                    new() { Label = "Feb", Actual = 225, Target = 245 },
                    new() { Label = "Mar", Actual = 220, Target = 245 },
                    new() { Label = "Apr", Actual = 235, Target = 240 },
                    new() { Label = "May", Actual = 247, Target = 240 },
                },
                TrendsYear = new List<MonthlyAttendanceTrend>
                {
                    new() { Label = "2022", Actual = 1980, Target = 2200 },
                    new() { Label = "2023", Actual = 2150, Target = 2300 },
                    new() { Label = "2024", Actual = 2260, Target = 2350 },
                    new() { Label = "2025", Actual = 2400, Target = 2400 },
                    new() { Label = "2026", Actual = 1247, Target = 1200 },
                },

                ParticipationSplit = new List<ParticipationSplitStat>
                {
                    new() { Label = "Regular", Count = 180 },
                    new() { Label = "Occasional", Count = 45 },
                    new() { Label = "Inactive", Count = 22 },
                },

                TeamComparison = new List<TeamComparisonStat>
                {
                    new() { Team = "Team A", Present = 45, Absent = 5 },
                    new() { Team = "Team B", Present = 41, Absent = 8 },
                    new() { Team = "Team C", Present = 37, Absent = 12 },
                    new() { Team = "Team D", Present = 39, Absent = 9 },
                },

                MostActivePlayers = new List<TopPlayerStat>
                {
                    new() { Rank = 1, Name = "Sarah Smith", Sessions = 49, Rate = 98 },
                    new() { Rank = 2, Name = "John Doe", Sessions = 48, Rate = 96 },
                    new() { Rank = 3, Name = "Michael Johnson", Sessions = 47, Rate = 94 },
                    new() { Rank = 4, Name = "Emily Davis", Sessions = 46, Rate = 92 },
                    new() { Rank = 5, Name = "David Martinez", Sessions = 45, Rate = 90 },
                },

                SessionRecords = new List<SessionRecordRow>
                {
                    new() { Date = new DateTime(2026, 5, 10), Session = "Training Session", Team = "Team A", Present = 42, Absent = 8 },
                    new() { Date = new DateTime(2026, 5, 9), Session = "Team Practice", Team = "Team B", Present = 38, Absent = 12 },
                    new() { Date = new DateTime(2026, 5, 8), Session = "Strength Training", Team = "Team A", Present = 45, Absent = 5 },
                    new() { Date = new DateTime(2026, 5, 7), Session = "Team Meeting", Team = "Team C", Present = 48, Absent = 2 },
                    new() { Date = new DateTime(2026, 5, 6), Session = "Skills Workshop", Team = "Team D", Present = 40, Absent = 10 },
                    new() { Date = new DateTime(2026, 5, 5), Session = "Tactical Training", Team = "Team B", Present = 35, Absent = 15 },
                    new() { Date = new DateTime(2026, 5, 4), Session = "Fitness Session", Team = "Team C", Present = 44, Absent = 6 },
                    new() { Date = new DateTime(2026, 5, 3), Session = "Scrimmage", Team = "Team A", Present = 50, Absent = 0 },
                },

                Teams = new List<string> { "Team A", "Team B", "Team C", "Team D" },
            };

            IEnumerable<SessionRecordRow> filtered = vm.SessionRecords;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(r =>
                    r.Session.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    r.Team.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(team))
            {
                filtered = filtered.Where(r => r.Team == team);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(r => r.Status == status);
            }

            vm.SessionRecords = filtered.OrderByDescending(r => r.Date).ToList();

            ViewBag.Search = search;
            ViewBag.SelectedTeam = team;
            ViewBag.SelectedStatus = status;
            ViewBag.TotalSessionCount = 8;

            return View(vm);
        }

        // GET: Attendance/Export - downloads the session records as CSV
        public IActionResult Export()
        {
            // Rebuild the same demo dataset used by Index (see note above).
            var records = new List<SessionRecordRow>
            {
                new() { Date = new DateTime(2026, 5, 10), Session = "Training Session", Team = "Team A", Present = 42, Absent = 8 },
                new() { Date = new DateTime(2026, 5, 9), Session = "Team Practice", Team = "Team B", Present = 38, Absent = 12 },
                new() { Date = new DateTime(2026, 5, 8), Session = "Strength Training", Team = "Team A", Present = 45, Absent = 5 },
                new() { Date = new DateTime(2026, 5, 7), Session = "Team Meeting", Team = "Team C", Present = 48, Absent = 2 },
                new() { Date = new DateTime(2026, 5, 6), Session = "Skills Workshop", Team = "Team D", Present = 40, Absent = 10 },
                new() { Date = new DateTime(2026, 5, 5), Session = "Tactical Training", Team = "Team B", Present = 35, Absent = 15 },
                new() { Date = new DateTime(2026, 5, 4), Session = "Fitness Session", Team = "Team C", Present = 44, Absent = 6 },
                new() { Date = new DateTime(2026, 5, 3), Session = "Scrimmage", Team = "Team A", Present = 50, Absent = 0 },
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Session,Team,Present,Absent,Rate,Status");
            foreach (var r in records)
            {
                sb.AppendLine($"{r.Date:yyyy-MM-dd},\"{r.Session}\",\"{r.Team}\",{r.Present},{r.Absent},{r.Rate}%,\"{r.Status}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"session_records_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Attendance/Records - the raw individual Player/Event check-in log
        // (the original per-player attendance table still lives here)
        public async Task<IActionResult> Records(int? eventId)
        {
            var query = _context.Attendances
                .Include(a => a.Player)
                .Include(a => a.Event)
                .AsQueryable();

            if (eventId.HasValue)
            {
                query = query.Where(a => a.EventId == eventId);
            }

            ViewBag.Events = new SelectList(await _context.Events.OrderByDescending(e => e.Date).ToListAsync(), "Id", "Title", eventId);
            ViewBag.SelectedEventId = eventId;

            return View(await query.OrderByDescending(a => a.Date).ToListAsync());
        }

        // GET: Attendance/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new Attendance { Date = DateTime.Today });
        }

        // POST: Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PlayerId,EventId,Date,Status")] Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                _context.Add(attendance);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Attendance record was added.";
                return RedirectToAction(nameof(Records));
            }
            PopulateDropdowns(attendance.PlayerId, attendance.EventId);
            return View(attendance);
        }

        // GET: Attendance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances
                .Include(a => a.Player)
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null) return NotFound();

            return View(attendance);
        }

        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Attendance record was deleted.";
            }
            return RedirectToAction(nameof(Records));
        }

        private void PopulateDropdowns(int? selectedPlayerId = null, int? selectedEventId = null)
        {
            ViewBag.PlayerId = new SelectList(_context.Players.OrderBy(p => p.Name).ToList(), "Id", "Name", selectedPlayerId);
            ViewBag.EventId = new SelectList(_context.Events.OrderByDescending(e => e.Date).ToList(), "Id", "Title", selectedEventId);
        }
    }
}
