using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IPasswordHasher<AppUser> _passwordHasher;

        public SettingsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IPasswordHasher<AppUser> passwordHasher)
        {
            _context = context;
            _env = env;
            _passwordHasher = passwordHasher;
        }

        // Redirect the bare /Settings URL to Profile
        public IActionResult Index() => RedirectToAction(nameof(Profile));

        // ================= PROFILE =================
        public async Task<IActionResult> Profile()
        {
            ViewBag.ActiveSettingsTab = "Profile";
            var profile = await _context.UserProfiles.FirstOrDefaultAsync() ?? new UserProfile();
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile([Bind("FullName,Email,Phone,Bio")] UserProfile input)
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync();
            if (profile == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveSettingsTab = "Profile";
                input.Id = profile.Id;
                input.AvatarPath = profile.AvatarPath;
                return View(input);
            }

            profile.FullName = input.FullName;
            profile.Email = input.Email;
            profile.Phone = input.Phone;
            profile.Bio = input.Bio;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile? photo)
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync();
            if (profile == null) return NotFound();

            if (photo != null && photo.Length > 0)
            {
                if (!await UploadSecurity.IsSafeImageAsync(photo))
                {
                    TempData["Error"] =
                        "Photo must be a valid JPG, PNG, or WebP file no larger than 2 MB.";
                    return RedirectToAction(nameof(Profile));
                }

                var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();

                // Remove old avatar file if there was one
                if (!string.IsNullOrEmpty(profile.AvatarPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, profile.AvatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                profile.AvatarPath = $"/uploads/avatars/{fileName}";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile photo updated.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto()
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync();
            if (profile != null && !string.IsNullOrEmpty(profile.AvatarPath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, profile.AvatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);

                profile.AvatarPath = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile photo removed.";
            }
            return RedirectToAction(nameof(Profile));
        }

        // ================= SECURITY =================
        public async Task<IActionResult> Security()
        {
            ViewBag.ActiveSettingsTab = "Security";
            var profile = await _context.UserProfiles.FirstOrDefaultAsync() ?? new UserProfile();
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var appUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(appUserIdValue, out var appUserId)) return Forbid();

            var appUser = await _context.AppUsers.FindAsync(appUserId);
            if (appUser == null || !appUser.IsActive) return Forbid();

            if (string.IsNullOrEmpty(currentPassword) ||
                _passwordHasher.VerifyHashedPassword(
                    appUser,
                    appUser.PasswordHash,
                    currentPassword) == PasswordVerificationResult.Failed)
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Security));
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                TempData["Error"] = "New password must be at least 8 characters.";
                return RedirectToAction(nameof(Security));
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Security));
            }

            appUser.PasswordHash = _passwordHasher.HashPassword(
                appUser,
                newPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password updated. Use your new password next time you log in.";
            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactor()
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync();
            if (profile == null) return NotFound();

            profile.TwoFactorEnabled = !profile.TwoFactorEnabled;
            await _context.SaveChangesAsync();

            return Json(new { success = true, enabled = profile.TwoFactorEnabled });
        }

        // ================= NOTIFICATIONS =================
        public async Task<IActionResult> Notifications()
        {
            ViewBag.ActiveSettingsTab = "Notifications";
            var prefs = await _context.NotificationPreferences.ToListAsync();
            return View(prefs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleNotification(int id, string channel)
        {
            var pref = await _context.NotificationPreferences.FindAsync(id);
            if (pref == null) return NotFound();

            switch (channel)
            {
                case "email": pref.EmailEnabled = !pref.EmailEnabled; break;
                case "sms": pref.SmsEnabled = !pref.SmsEnabled; break;
                case "push": pref.PushEnabled = !pref.PushEnabled; break;
                default: return BadRequest();
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ================= ROLES & USERS =================
        public async Task<IActionResult> RolesUsers()
        {
            ViewBag.ActiveSettingsTab = "RolesUsers";
            var users = await _context.AppUsers
                .AsNoTracking()
                .Where(user =>
                    user.Role == AppUserRole.Admin ||
                    user.Role == AppUserRole.Coach)
                .OrderBy(user => user.Email)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ProvisionCoach(
            string email,
            string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!new EmailAddressAttribute().IsValid(normalizedEmail) ||
                string.IsNullOrWhiteSpace(password) ||
                password.Length < 8)
            {
                TempData["Error"] =
                    "Enter a valid email and a password of at least 8 characters.";
                return RedirectToAction(nameof(RolesUsers));
            }

            if (await _context.AppUsers.AnyAsync(user =>
                    user.NormalizedEmail == normalizedEmail))
            {
                TempData["Error"] =
                    "An account already exists with this email address.";
                return RedirectToAction(nameof(RolesUsers));
            }

            var coach = new AppUser
            {
                Email = normalizedEmail,
                NormalizedEmail = normalizedEmail,
                Role = AppUserRole.Coach,
                IsActive = true
            };

            coach.PasswordHash = _passwordHasher.HashPassword(
                coach,
                password);

            _context.AppUsers.Add(coach);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
                when (DatabaseConflictClassifier.IsUniqueViolation(
                    exception,
                    _context.Database,
                    "IX_AppUsers_NormalizedEmail"))
            {
                TempData["Error"] =
                    "An account already exists with this email address.";
                return RedirectToAction(nameof(RolesUsers));
            }

            TempData["Success"] =
                $"Coach account {coach.Email} was created.";
            return RedirectToAction(nameof(RolesUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ToggleAppUserActive(int id)
        {
            var currentIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentIdValue, out var currentId) && currentId == id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(RolesUsers));
            }

            var user = await _context.AppUsers.FirstOrDefaultAsync(appUser =>
                appUser.Id == id &&
                (appUser.Role == AppUserRole.Admin ||
                 appUser.Role == AppUserRole.Coach));
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{user.Email} was {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(RolesUsers));
        }

        // ================= SYSTEM =================
        public async Task<IActionResult> SystemConfig()
        {
            ViewBag.ActiveSettingsTab = "System";
            var settings = await _context.OrganisationSettings.FirstOrDefaultAsync() ?? new OrganisationSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SystemConfig(OrganisationSettings input, IFormFile? logo, bool removeLogo = false)
        {
            var settings = await _context.OrganisationSettings.FirstOrDefaultAsync();
            if (settings == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveSettingsTab = "System";
                input.Id = settings.Id;
                input.LogoPath = settings.LogoPath;
                return View(input);
            }

            settings.OrganisationName = input.OrganisationName;
            settings.Timezone = input.Timezone;
            settings.ActiveSeason = input.ActiveSeason;
            settings.MinAttendancePercent = input.MinAttendancePercent;
            settings.Language = input.Language;

            if (logo != null && logo.Length > 0)
            {
                if (!await UploadSecurity.IsSafeImageAsync(logo))
                {
                    ModelState.AddModelError(
                        "logo",
                        "Logo must be a valid JPG, PNG, or WebP file no larger than 2 MB.");
                    ViewBag.ActiveSettingsTab = "System";
                    input.Id = settings.Id;
                    input.LogoPath = settings.LogoPath;
                    return View(input);
                }

                var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
                if (!string.IsNullOrEmpty(settings.LogoPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, settings.LogoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "branding");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await logo.CopyToAsync(stream);
                }

                settings.LogoPath = $"/uploads/branding/{fileName}";
            }
            else if (removeLogo && !string.IsNullOrEmpty(settings.LogoPath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, settings.LogoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                settings.LogoPath = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "System configuration saved.";
            return RedirectToAction(nameof(SystemConfig));
        }

        // ================= APPEARANCE =================
        public async Task<IActionResult> Appearance()
        {
            ViewBag.ActiveSettingsTab = "Appearance";
            var settings = await _context.AppearanceSettings.FirstOrDefaultAsync() ?? new AppearanceSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Appearance(AppTheme theme, string accentColor, LayoutDensity density)
        {
            var settings = await _context.AppearanceSettings.FirstOrDefaultAsync();
            if (settings == null) return NotFound();

            settings.Theme = theme;
            settings.AccentColor = string.IsNullOrWhiteSpace(accentColor) ? settings.AccentColor : accentColor;
            settings.Density = density;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appearance settings applied.";
            return RedirectToAction(nameof(Appearance));
        }

        // ================= BACKUP & DATA =================
        public async Task<IActionResult> BackupData(
            CancellationToken cancellationToken)
        {
            ViewBag.ActiveSettingsTab = "BackupData";
            var backups = await _context.BackupRecords.AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
            return View(backups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup(
            CancellationToken cancellationToken)
        {
            // Stream each query to disk so backup size does not become equivalent
            // to managed-memory growth as the tables expand.
            var snapshot = new
            {
                GeneratedAt = DateTime.Now,
                Players = _context.Players.AsNoTracking().AsAsyncEnumerable(),
                Coaches = _context.Coaches.AsNoTracking().AsAsyncEnumerable(),
                Events = _context.Events.AsNoTracking().AsAsyncEnumerable(),
                Matches = _context.Matches.AsNoTracking().AsAsyncEnumerable(),
                Announcements = _context.Announcements.AsNoTracking().AsAsyncEnumerable(),
            };

            var backupsFolder = Path.Combine(_env.WebRootPath, "uploads", "backups");
            Directory.CreateDirectory(backupsFolder);
            var fileName =
                $"backup_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
            var backupPath = Path.Combine(backupsFolder, fileName);
            long sizeBytes;
            await using (var stream = new FileStream(
                backupPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true))
            {
                await System.Text.Json.JsonSerializer.SerializeAsync(
                    stream,
                    snapshot,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    },
                    cancellationToken);
                sizeBytes = stream.Length;
            }

            _context.BackupRecords.Add(new BackupRecord
            {
                CreatedAt = DateTime.Now,
                SizeBytes = sizeBytes,
                Success = true,
            });
            await _context.SaveChangesAsync(cancellationToken);

            TempData["Success"] = "Backup created successfully.";
            return RedirectToAction(nameof(BackupData));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestoreFromFile(IFormFile? file)
        {
            // A full restore-and-overwrite is out of scope for this demo (it would need
            // careful merge logic per entity), but we validate the upload and confirm receipt
            // so the workflow is genuine rather than a no-op button.
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a backup file to restore from.";
                return RedirectToAction(nameof(BackupData));
            }

            if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".json")
            {
                TempData["Error"] = "Restore file must be a .json backup exported from this system.";
                return RedirectToAction(nameof(BackupData));
            }

            TempData["Error"] =
                "Automatic PostgreSQL restore is not implemented. No data was changed. Ask the system administrator to restore a verified database backup.";
            return RedirectToAction(nameof(BackupData));
        }

        public async Task<IActionResult> DownloadBackup(
            int id,
            CancellationToken cancellationToken)
        {
            var backup = await _context.BackupRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);
            if (backup == null) return NotFound();

            var backupsFolder = Path.Combine(_env.WebRootPath, "uploads", "backups");
            if (Directory.Exists(backupsFolder))
            {
                var files = Directory.GetFiles(backupsFolder, "*.json").OrderByDescending(f => f).ToList();
                var match = files.FirstOrDefault();
                if (match != null)
                {
                    return PhysicalFile(
                        match,
                        "application/json",
                        Path.GetFileName(match),
                        enableRangeProcessing: true);
                }
            }

            TempData["Error"] = "That backup's file is no longer available (only the most recent backup's file is retained in this demo).";
            return RedirectToAction(nameof(BackupData));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetDemoData()
        {
            TempData["Error"] =
                "Database reset is disabled because this application uses persistent PostgreSQL data.";
            return RedirectToAction(nameof(BackupData));
        }
    }
}
