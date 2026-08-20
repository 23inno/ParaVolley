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
        // Builds the attendance dashboard from persisted player/event records.
        public async Task<IActionResult> Index(string? search, string? team, string? status)
        {
            var attendanceRecords = await _context.Attendances
                .AsNoTracking()
                .Include(record => record.Player)
                .Include(record => record.Event)
                .ToListAsync();

            var players = await _context.Players
                .AsNoTracking()
                .OrderBy(player => player.Name)
                .ToListAsync();

            var totalPresent = attendanceRecords.Count(record =>
                record.Status == AttendanceStatus.Present);

            var attendanceRate = attendanceRecords.Count == 0
                ? 0
                : Math.Round(
                    totalPresent * 100.0 / attendanceRecords.Count,
                    1);

            var playerStatistics = attendanceRecords
                .Where(record => record.Player != null)
                .GroupBy(record => new
                {
                    record.PlayerId,
                    record.Player!.Name
                })
                .Select(group => new
                {
                    group.Key.PlayerId,
                    group.Key.Name,
                    Sessions = group.Count(),
                    Rate = (int)Math.Round(
                        group.Count(record =>
                            record.Status == AttendanceStatus.Present) *
                        100.0 / group.Count(),
                        0)
                })
                .OrderByDescending(item => item.Rate)
                .ThenByDescending(item => item.Sessions)
                .ThenBy(item => item.Name)
                .ToList();

            var topPerformer = playerStatistics.FirstOrDefault();

            var sessionRecords = attendanceRecords
                .Where(record =>
                    record.Event != null &&
                    record.Player != null)
                .GroupBy(record => new
                {
                    record.EventId,
                    record.Event!.Title,
                    record.Event.Date,
                    Team = record.Player!.Team
                })
                .Select(group => new SessionRecordRow
                {
                    Date = group.Key.Date,
                    Session = group.Key.Title,
                    Team = group.Key.Team,
                    Present = group.Count(record =>
                        record.Status == AttendanceStatus.Present),
                    Absent = group.Count(record =>
                        record.Status == AttendanceStatus.Absent)
                })
                .OrderByDescending(record => record.Date)
                .ThenBy(record => record.Session)
                .ThenBy(record => record.Team)
                .ToList();

            var playerAttendanceLookup = attendanceRecords
                .GroupBy(record => record.PlayerId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var vm = new AttendanceDashboardViewModel
            {
                TotalAttendance = attendanceRecords.Count,
                AttendanceRate = attendanceRate,
                ActiveSessions = attendanceRecords
                    .Select(record => record.EventId)
                    .Distinct()
                    .Count(),
                TopPerformerName = topPerformer?.Name ?? "No attendance yet",
                TopPerformerRate = topPerformer?.Rate ?? 0,

                TrendsWeek = attendanceRecords
                    .GroupBy(record => record.Date.Date)
                    .OrderByDescending(group => group.Key)
                    .Take(7)
                    .OrderBy(group => group.Key)
                    .Select(group => new MonthlyAttendanceTrend
                    {
                        Label = group.Key.ToString("ddd"),
                        Actual = group.Count(record =>
                            record.Status == AttendanceStatus.Present),
                        Target = group.Count()
                    })
                    .ToList(),

                TrendsMonth = attendanceRecords
                    .GroupBy(record => new
                    {
                        record.Date.Year,
                        record.Date.Month
                    })
                    .OrderByDescending(group => group.Key.Year)
                    .ThenByDescending(group => group.Key.Month)
                    .Take(6)
                    .OrderBy(group => group.Key.Year)
                    .ThenBy(group => group.Key.Month)
                    .Select(group => new MonthlyAttendanceTrend
                    {
                        Label = new DateTime(
                            group.Key.Year,
                            group.Key.Month,
                            1).ToString("MMM yy"),
                        Actual = group.Count(record =>
                            record.Status == AttendanceStatus.Present),
                        Target = group.Count()
                    })
                    .ToList(),

                TrendsYear = attendanceRecords
                    .GroupBy(record => record.Date.Year)
                    .OrderBy(group => group.Key)
                    .Select(group => new MonthlyAttendanceTrend
                    {
                        Label = group.Key.ToString(),
                        Actual = group.Count(record =>
                            record.Status == AttendanceStatus.Present),
                        Target = group.Count()
                    })
                    .ToList(),

                ParticipationSplit = new List<ParticipationSplitStat>
                {
                    new()
                    {
                        Label = "Regular",
                        Count = players.Count(player =>
                            playerAttendanceLookup.TryGetValue(player.Id, out var records) &&
                            records.Count(record => record.Status == AttendanceStatus.Present) *
                                100.0 / records.Count >= 75)
                    },
                    new()
                    {
                        Label = "Occasional",
                        Count = players.Count(player =>
                            playerAttendanceLookup.TryGetValue(player.Id, out var records) &&
                            records.Count(record => record.Status == AttendanceStatus.Present) *
                                100.0 / records.Count < 75)
                    },
                    new()
                    {
                        Label = "No records",
                        Count = players.Count(player =>
                            !playerAttendanceLookup.ContainsKey(player.Id))
                    }
                },

                TeamComparison = players
                    .Select(player => player.Team)
                    .Where(teamName => !string.IsNullOrWhiteSpace(teamName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(teamName => teamName)
                    .Select(teamName => new TeamComparisonStat
                    {
                        Team = teamName,
                        Present = attendanceRecords.Count(record =>
                            record.Player?.Team == teamName &&
                            record.Status == AttendanceStatus.Present),
                        Absent = attendanceRecords.Count(record =>
                            record.Player?.Team == teamName &&
                            record.Status == AttendanceStatus.Absent)
                    })
                    .ToList(),

                MostActivePlayers = playerStatistics
                    .Take(5)
                    .Select((item, index) => new TopPlayerStat
                    {
                        Rank = index + 1,
                        Name = item.Name,
                        Sessions = item.Sessions,
                        Rate = item.Rate
                    })
                    .ToList(),

                SessionRecords = sessionRecords,
                Teams = players
                    .Select(player => player.Team)
                    .Where(teamName => !string.IsNullOrWhiteSpace(teamName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(teamName => teamName)
                    .ToList()
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
            ViewBag.TotalSessionCount = sessionRecords.Count;

            return View(vm);
        }

        // GET: Attendance/Export - downloads the session records as CSV
        public async Task<IActionResult> Export()
        {
            var records = await _context.Attendances
                .AsNoTracking()
                .Include(record => record.Player)
                .Include(record => record.Event)
                .Where(record =>
                    record.Player != null &&
                    record.Event != null)
                .ToListAsync();

            var sessionRecords = records
                .GroupBy(record => new
                {
                    record.EventId,
                    record.Event!.Title,
                    record.Event.Date,
                    Team = record.Player!.Team
                })
                .Select(group => new SessionRecordRow
                {
                    Date = group.Key.Date,
                    Session = group.Key.Title,
                    Team = group.Key.Team,
                    Present = group.Count(record =>
                        record.Status == AttendanceStatus.Present),
                    Absent = group.Count(record =>
                        record.Status == AttendanceStatus.Absent)
                })
                .OrderByDescending(record => record.Date)
                .ThenBy(record => record.Session)
                .ThenBy(record => record.Team)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Session,Team,Present,Absent,Rate,Status");
            foreach (var r in sessionRecords)
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
