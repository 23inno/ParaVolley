using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReportsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? type,
            string? status,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);
            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);
            var nextDay = today.AddDays(1);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonth = monthStart.AddMonths(1);

            var weeklyAttendance = await _context.Attendances.AsNoTracking()
                .Where(record =>
                    record.Date >= weekStart &&
                    record.Date < nextDay)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Present = group.Count(record =>
                        record.Status == AttendanceStatus.Present)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var weeklyAttendanceRate = weeklyAttendance == null ||
                weeklyAttendance.Total == 0
                ? 0
                : Math.Round(
                    weeklyAttendance.Present * 100.0 /
                    weeklyAttendance.Total,
                    1);

            var sessionsThisMonth = await _context.Events.AsNoTracking()
                .CountAsync(eventItem =>
                    eventItem.Date >= monthStart &&
                    eventItem.Date < nextMonth,
                    cancellationToken);

            var perfectAttendanceCount = await _context.Attendances
    .AsNoTracking()
    .Where(record =>
        record.Date >= monthStart &&
        record.Date < nextMonth)
    .GroupBy(record => record.PlayerId)
    .Where(group =>
        group.All(record =>
            record.Status == AttendanceStatus.Present))
    .CountAsync(cancellationToken);

            var reportQuery = _context.Reports.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                reportQuery = reportQuery.Where(report =>
                    report.Title.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ReportType>(type, out var parsedType))
            {
                reportQuery = reportQuery.Where(report => report.Type == parsedType);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, out var parsedStatus))
            {
                reportQuery = reportQuery.Where(report => report.Status == parsedStatus);
            }

            var filteredReportCount = await reportQuery.CountAsync(cancellationToken);
            var pageCount = Paging.PageCount(
                filteredReportCount,
                Paging.DefaultPageSize);
            page = Math.Min(page, pageCount);

            var reportCounts = await _context.Reports.AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Published = group.Count(report =>
                        report.Status == ReportStatus.Published),
                    Draft = group.Count(report =>
                        report.Status == ReportStatus.Draft)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var playerCounts = await _context.Players.AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Active = group.Count(player =>
                        player.Status == PlayerStatus.Active),
                    Inactive = group.Count(player =>
                        player.Status == PlayerStatus.Inactive)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var matchCounts = await _context.Matches.AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Completed = group.Count(match =>
                        match.Status == MatchStatus.Completed),
                    Wins = group.Count(match =>
                        match.Status == MatchStatus.Completed &&
                        match.ScoreA > match.ScoreB),
                    Losses = group.Count(match =>
                        match.Status == MatchStatus.Completed &&
                        match.ScoreA < match.ScoreB)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var teamBreakdowns = await _context.Players.AsNoTracking()
                .GroupBy(player => player.Team)
                .Select(group => new TeamBreakdown
                {
                    Team = group.Key,
                    PlayerCount = group.Count(),
                    ActiveCount = group.Count(player =>
                        player.Status == PlayerStatus.Active)
                })
                .OrderBy(item => item.Team)
                .Take(100)
                .ToListAsync(cancellationToken);

            var eventTypeCounts = await _context.Events.AsNoTracking()
                .GroupBy(eventItem => eventItem.Type)
                .Select(group => new
                {
                    Type = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync(cancellationToken);

            var vm = new ReportsViewModel
            {
                Reports = await reportQuery
                    .OrderByDescending(report => report.Date)
                    .ThenByDescending(report => report.Id)
                    .Skip((page - 1) * Paging.DefaultPageSize)
                    .Take(Paging.DefaultPageSize)
                    .ToListAsync(cancellationToken),
                TotalReportsCount = reportCounts?.Total ?? 0,
                PublishedCount = reportCounts?.Published ?? 0,
                DraftCount = reportCounts?.Draft ?? 0,

                WeeklyAverage = $"{weeklyAttendanceRate:0.#}%",
                SessionsThisMonth = sessionsThisMonth.ToString(),
                PerfectAttendanceCount = perfectAttendanceCount.ToString(),

                TotalPlayers = playerCounts?.Total ?? 0,
                ActivePlayers = playerCounts?.Active ?? 0,
                InactivePlayers = playerCounts?.Inactive ?? 0,
                TotalCoaches = await _context.Coaches.CountAsync(cancellationToken),
                TotalMatches = matchCounts?.Total ?? 0,
                CompletedMatches = matchCounts?.Completed ?? 0,
                WinsA = matchCounts?.Wins ?? 0,
                LossesA = matchCounts?.Losses ?? 0,

                TeamBreakdowns = teamBreakdowns,
                EventTypeBreakdowns = eventTypeCounts
                    .Select(item => new EventTypeBreakdown
                    {
                        Type = item.Type.ToString(),
                        Count = item.Count
                    })
                    .ToList()
            };

            var attendanceByEvent = await _context.Attendances
                .AsNoTracking()
                .GroupBy(a => new { a.EventId, a.Event!.Title, a.Event.Date })
                .Select(g => new AttendanceSummary
                {
                    EventTitle = g.Key.Title,
                    Date = g.Key.Date,
                    PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = g.Count(a => a.Status == AttendanceStatus.Absent)
                })
                .OrderByDescending(a => a.Date)
                .Take(50)
                .ToListAsync(cancellationToken);

            vm.AttendanceSummaries = attendanceByEvent;

            ViewBag.Search = search;
            ViewBag.SelectedType = type;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;

            return View(vm);
        }

        // GET: Reports/Create
        public IActionResult Create()
        {
            var report = new Report { Date = DateTime.Today };
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", report);
            }
            return View(report);
        }

        // POST: Reports/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Type,Status,Date")] Report report, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return PartialView("_CreatePartial", report);
                }
                return View(report);
            }

            if (file != null && file.Length > 0)
            {
                if (!await UploadSecurity.IsSafeReportAsync(file))
                {
                    ModelState.AddModelError(
                        "file",
                        "Attachment must be a valid PDF, Word, Excel, CSV, JPG, PNG, or WebP file no larger than 10 MB.");
                    return IsAjaxRequest()
                        ? PartialView("_CreatePartial", report)
                        : View(report);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reports");
                Directory.CreateDirectory(uploadsFolder);

                var safeFileName = UploadSecurity.GeneratedFileName(file);
                var fullPath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                report.FilePath = $"/uploads/reports/{safeFileName}";
                report.FileName = UploadSecurity.SafeOriginalFileName(file.FileName);
                report.SizeBytes = file.Length;
            }
            else
            {
                report.SizeBytes = 0;
            }

            _context.Add(report);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Report \"{report.Title}\" was generated.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Reports/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", report);
            }
            return View(report);
        }

        // POST: Reports/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Type,Status,Date,FilePath,FileName,SizeBytes")] Report input, IFormFile? file, bool removeAttachment = false)
        {
            if (id != input.Id) return NotFound();

            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return PartialView("_EditPartial", input);
                }
                return View(input);
            }

            report.Title = input.Title;
            report.Type = input.Type;
            report.Status = input.Status;
            report.Date = input.Date;

            void DeleteExistingFile()
            {
                if (!string.IsNullOrEmpty(report.FilePath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, report.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
            }

            if (file != null && file.Length > 0)
            {
                if (!await UploadSecurity.IsSafeReportAsync(file))
                {
                    ModelState.AddModelError(
                        "file",
                        "Attachment must be a valid PDF, Word, Excel, CSV, JPG, PNG, or WebP file no larger than 10 MB.");
                    return IsAjaxRequest()
                        ? PartialView("_EditPartial", input)
                        : View(input);
                }

                // Replace the attachment
                DeleteExistingFile();

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reports");
                Directory.CreateDirectory(uploadsFolder);
                var safeFileName = UploadSecurity.GeneratedFileName(file);
                var fullPath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                report.FilePath = $"/uploads/reports/{safeFileName}";
                report.FileName = UploadSecurity.SafeOriginalFileName(file.FileName);
                report.SizeBytes = file.Length;
            }
            else if (removeAttachment)
            {
                DeleteExistingFile();
                report.FilePath = null;
                report.FileName = null;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Report \"{report.Title}\" was updated.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", report);
            }
            return View(report);
        }

        // GET: Reports/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var report = await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(report.FilePath))
            {
                var fullPath = Path.Combine(
                    _env.WebRootPath,
                    report.FilePath
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar));

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["Error"] =
                        "The uploaded attachment for this report is currently unavailable.";

                    return RedirectToAction(nameof(Index));
                }

                return PhysicalFile(
                    fullPath,
                    "application/octet-stream",
                    report.FileName ?? Path.GetFileName(fullPath),
                    enableRangeProcessing: true);
            }

            // Reports created without an attachment can still be downloaded
            // as a simple generated summary.
            var content =
                $"Report: {report.Title}\n" +
                $"Type: {report.Type}\n" +
                $"Date: {report.Date:yyyy-MM-dd}\n" +
                $"Status: {report.Status}";

            var textBytes =
                System.Text.Encoding.UTF8.GetBytes(content);

            var safeTitle = string.IsNullOrWhiteSpace(report.Title)
                ? "report"
                : report.Title.Replace(' ', '-');

            return File(
                textBytes,
                "text/plain",
                $"{safeTitle}.txt");
        }

        // GET: Reports/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", report);
            }
            return View(report);
        }

        // POST: Reports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                if (!string.IsNullOrEmpty(report.FilePath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, report.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Report was deleted.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
