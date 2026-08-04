using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReportsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search, string? type, string? status)
        {
            var players = await _context.Players.ToListAsync();
            var matches = await _context.Matches.ToListAsync();
            var events = await _context.Events.ToListAsync();

            var completedMatches = matches.Where(m => m.Status == MatchStatus.Completed).ToList();

            var allReports = await _context.Reports.ToListAsync();

            var filteredReports = allReports.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredReports = filteredReports.Where(r => r.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ReportType>(type, out var parsedType))
            {
                filteredReports = filteredReports.Where(r => r.Type == parsedType);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, out var parsedStatus))
            {
                filteredReports = filteredReports.Where(r => r.Status == parsedStatus);
            }

            var vm = new ReportsViewModel
            {
                Reports = filteredReports.OrderByDescending(r => r.Date).ToList(),
                TotalReportsCount = allReports.Count,
                PublishedCount = allReports.Count(r => r.Status == ReportStatus.Published),
                DraftCount = allReports.Count(r => r.Status == ReportStatus.Draft),

                TotalPlayers = players.Count,
                ActivePlayers = players.Count(p => p.Status == PlayerStatus.Active),
                InactivePlayers = players.Count(p => p.Status == PlayerStatus.Inactive),
                TotalCoaches = await _context.Coaches.CountAsync(),
                TotalMatches = matches.Count,
                CompletedMatches = completedMatches.Count,
                WinsA = completedMatches.Count(m => m.ScoreA > m.ScoreB),
                LossesA = completedMatches.Count(m => m.ScoreA < m.ScoreB),

                TeamBreakdowns = players
                    .GroupBy(p => p.Team)
                    .Select(g => new TeamBreakdown
                    {
                        Team = g.Key,
                        PlayerCount = g.Count(),
                        ActiveCount = g.Count(p => p.Status == PlayerStatus.Active)
                    })
                    .OrderBy(t => t.Team)
                    .ToList(),

                EventTypeBreakdowns = events
                    .GroupBy(e => e.Type)
                    .Select(g => new EventTypeBreakdown { Type = g.Key.ToString(), Count = g.Count() })
                    .OrderByDescending(e => e.Count)
                    .ToList(),
            };

            var attendanceByEvent = await _context.Attendances
                .Include(a => a.Event)
                .GroupBy(a => new { a.EventId, a.Event!.Title, a.Event.Date })
                .Select(g => new AttendanceSummary
                {
                    EventTitle = g.Key.Title,
                    Date = g.Key.Date,
                    PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = g.Count(a => a.Status == AttendanceStatus.Absent)
                })
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            vm.AttendanceSummaries = attendanceByEvent;

            ViewBag.Search = search;
            ViewBag.SelectedType = type;
            ViewBag.SelectedStatus = status;

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
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reports");
                Directory.CreateDirectory(uploadsFolder);

                var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                report.FilePath = $"/uploads/reports/{safeFileName}";
                report.FileName = file.FileName;
                report.SizeBytes = file.Length;
            }
            else
            {
                // No file uploaded - generate a placeholder size like a freshly
                // generated report would have, matching the earlier prototype's behavior.
                report.SizeBytes = (long)(new Random().NextDouble() * 3 * 1024 * 1024 + 500 * 1024);
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
                // Replace the attachment
                DeleteExistingFile();

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reports");
                Directory.CreateDirectory(uploadsFolder);
                var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                report.FilePath = $"/uploads/reports/{safeFileName}";
                report.FileName = file.FileName;
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
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            if (!string.IsNullOrEmpty(report.FilePath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, report.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    var contentType = "application/octet-stream";
                    var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                    return File(bytes, contentType, report.FileName ?? Path.GetFileName(fullPath));
                }
            }

            // No uploaded file on record - generate a simple text summary instead.
            var content = $"Report: {report.Title}\nType: {report.Type}\nDate: {report.Date:yyyy-MM-dd}\nStatus: {report.Status}\nSize: {report.SizeLabel}";
            var textBytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(textBytes, "text/plain", $"{report.Title.Replace(' ', '-')}.txt");
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
