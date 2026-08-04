using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class CoachesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CoachesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // GET: Coaches
        public async Task<IActionResult> Index(string? search, string? team, string? status)
        {
            var query = _context.Coaches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Specialty.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(c => c.AssignedTeam == team);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CoachStatus>(status, out var parsedStatus))
            {
                query = query.Where(c => c.Status == parsedStatus);
            }

            var allCoaches = await _context.Coaches.ToListAsync();
            ViewBag.TotalCount = allCoaches.Count;
            ViewBag.ActiveCount = allCoaches.Count(c => c.Status == CoachStatus.Active);
            ViewBag.TeamsAssignedCount = allCoaches
                .Where(c => !string.IsNullOrWhiteSpace(c.AssignedTeam))
                .Select(c => c.AssignedTeam)
                .Distinct()
                .Count();
            ViewBag.AvailableCount = allCoaches.Count(c => c.Status == CoachStatus.Available);

            ViewBag.Search = search;
            ViewBag.SelectedTeam = team;
            ViewBag.SelectedStatus = status;

            return View(await query.OrderBy(c => c.Name).ToListAsync());
        }

        // GET: Coaches/Export - downloads the current filtered list as a CSV file
        public async Task<IActionResult> Export(string? search, string? team, string? status)
        {
            var query = _context.Coaches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Specialty.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(c => c.AssignedTeam == team);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CoachStatus>(status, out var parsedStatus))
            {
                query = query.Where(c => c.Status == parsedStatus);
            }

            var coaches = await query.OrderBy(c => c.Name).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Name,Email,Phone,Specialty,Experience,Certifications,AssignedTeam,Status");
            foreach (var c in coaches)
            {
                sb.AppendLine($"\"{c.Name}\",\"{c.Email}\",\"{c.Phone}\",\"{c.Specialty}\",\"{c.Experience}\",\"{c.Certifications}\",\"{c.AssignedTeam}\",\"{c.Status}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"coaches_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Coaches/AssignTeam/5
        public async Task<IActionResult> AssignTeam(int? id)
        {
            if (id == null) return NotFound();
            var coach = await _context.Coaches.FindAsync(id);
            if (coach == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_AssignTeamPartial", coach);
            }
            return View(coach);
        }

        // POST: Coaches/AssignTeam/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeam(int id, string? assignedTeam)
        {
            var coach = await _context.Coaches.FindAsync(id);
            if (coach == null) return NotFound();

            coach.AssignedTeam = string.IsNullOrWhiteSpace(assignedTeam) ? null : assignedTeam;
            await _context.SaveChangesAsync();
            TempData["Success"] = string.IsNullOrWhiteSpace(assignedTeam)
                ? $"{coach.Name} was unassigned from a team."
                : $"{coach.Name} was assigned to {assignedTeam}.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Coaches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.Id == id);
            if (coach == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", coach);
            }
            return View(coach);
        }

        // GET: Coaches/Create
        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", new Coach());
            }
            return View();
        }

        // POST: Coaches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,AssignedTeam,Status,Specialty,Experience,Certifications")] Coach coach, IFormFile? photo)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return PartialView("_CreatePartial", coach);
                }
                return View(coach);
            }

            if (photo != null && photo.Length > 0)
            {
                var savedPath = await SaveCoachPhotoAsync(photo);
                if (savedPath != null)
                {
                    coach.AvatarPath = savedPath;
                }
            }

            _context.Add(coach);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Coach \"{coach.Name}\" was created.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Coaches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var coach = await _context.Coaches.FindAsync(id);
            if (coach == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", coach);
            }
            return View(coach);
        }

        // POST: Coaches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone,AssignedTeam,Status,Specialty,Experience,Certifications")] Coach input, IFormFile? photo, bool removePhoto = false)
        {
            if (id != input.Id) return NotFound();

            var coach = await _context.Coaches.FindAsync(id);
            if (coach == null) return NotFound();

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return PartialView("_EditPartial", input);
                }
                return View(input);
            }

            coach.Name = input.Name;
            coach.Email = input.Email;
            coach.Phone = input.Phone;
            coach.AssignedTeam = input.AssignedTeam;
            coach.Status = input.Status;
            coach.Specialty = input.Specialty;
            coach.Experience = input.Experience;
            coach.Certifications = input.Certifications;

            if (photo != null && photo.Length > 0)
            {
                DeleteCoachPhotoIfExists(coach.AvatarPath);
                var savedPath = await SaveCoachPhotoAsync(photo);
                if (savedPath != null)
                {
                    coach.AvatarPath = savedPath;
                }
            }
            else if (removePhoto)
            {
                DeleteCoachPhotoIfExists(coach.AvatarPath);
                coach.AvatarPath = null;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Coach \"{coach.Name}\" was updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CoachExists(coach.Id)) return NotFound();
                throw;
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Coaches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.Id == id);
            if (coach == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DeletePartial", coach);
            }
            return View(coach);
        }

        // POST: Coaches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coach = await _context.Coaches.FindAsync(id);
            if (coach != null)
            {
                DeleteCoachPhotoIfExists(coach.AvatarPath);
                _context.Coaches.Remove(coach);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Coach was deleted.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CoachExists(int id)
        {
            return _context.Coaches.Any(e => e.Id == id);
        }

        private async Task<string?> SaveCoachPhotoAsync(IFormFile photo)
        {
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
            {
                return null;
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "coaches");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            return $"/uploads/coaches/{fileName}";
        }

        private void DeleteCoachPhotoIfExists(string? avatarPath)
        {
            if (string.IsNullOrEmpty(avatarPath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, avatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
