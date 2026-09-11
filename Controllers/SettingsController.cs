using ClosedXML.Excel;
using System.IO.Compression;
using System.Text.Json;
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
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly BackupService _backupService;

        public SettingsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IPasswordHasher<AppUser> passwordHasher,
            BackupService backupService)
        {
            _context = context;
            _env = env;
            _passwordHasher = passwordHasher;
            _backupService = backupService;
        }

        // Redirect the bare /Settings URL to Profile
        public IActionResult Index() => RedirectToAction(nameof(Profile));

        // ================= PROFILE =================
        public async Task<IActionResult> Profile()
        {
            ViewBag.ActiveSettingsTab = "Profile";

            var appUserIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(appUserIdValue, out var appUserId))
            {
                return Forbid();
            }

            var appUser = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
                    user.Id == appUserId &&
                    user.Role == AppUserRole.Admin &&
                    user.IsActive);

            if (appUser == null)
            {
                return Forbid();
            }

            var profile =
                await _context.UserProfiles.FirstOrDefaultAsync()
                ?? new UserProfile();

            // Authentication email is authoritative.
            profile.Email = appUser.Email;

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            [Bind("FullName,Phone,Bio")] UserProfile input)
        {
            var appUserIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(appUserIdValue, out var appUserId))
            {
                return Forbid();
            }

            var appUser = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
                    user.Id == appUserId &&
                    user.Role == AppUserRole.Admin &&
                    user.IsActive);

            if (appUser == null)
            {
                return Forbid();
            }

            var profile =
                await _context.UserProfiles.FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound();
            }

            // Preserve fields that are not editable on this form.
            input.Id = profile.Id;
            input.Email = appUser.Email;
            input.Role = profile.Role;
            input.AvatarPath = profile.AvatarPath;

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveSettingsTab = "Profile";
                return View(input);
            }

            profile.FullName =
                input.FullName?.Trim() ?? string.Empty;

            profile.Phone =
                input.Phone?.Trim() ?? string.Empty;

            profile.Bio =
                input.Bio?.Trim() ?? string.Empty;

            // Keep the legacy profile copy synchronized with
            // the real authentication account.
            profile.Email = appUser.Email;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto()
        {
            var profile =
                await _context.UserProfiles.FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(profile.AvatarPath))
            {
                var fileName =
                    Path.GetFileName(
                        profile.AvatarPath.Replace('\\', '/'));

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var fullPath = Path.Combine(
                        _env.WebRootPath,
                        "uploads",
                        "reports",
                        "avatars",
                        fileName);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                profile.AvatarPath = null;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Profile photo removed.";
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
        [EnableRateLimiting("sensitive")]
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



        // ================= NOTIFICATIONS =================
        public IActionResult Notifications()
        {
            ViewBag.ActiveSettingsTab = "Notifications";

            return View();
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
    string? email,
    string? password,
    CancellationToken cancellationToken = default)
        {
            var normalizedEmail =
                (email ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedEmail) ||
                normalizedEmail.Length > 200 ||
                !new EmailAddressAttribute().IsValid(normalizedEmail) ||
                string.IsNullOrWhiteSpace(password) ||
                password.Length < 8 ||
                password.Length > 128)
            {
                TempData["Error"] =
                    "Enter a valid email address and a password between 8 and 128 characters.";

                return RedirectToAction(nameof(RolesUsers));
            }

            var accountExists =
                await _context.AppUsers
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.NormalizedEmail == normalizedEmail,
                        cancellationToken);

            if (accountExists)
            {
                TempData["Error"] =
                    "An account already exists with this email address.";

                return RedirectToAction(nameof(RolesUsers));
            }

            var coachAccount = new AppUser
            {
                Email = normalizedEmail,
                NormalizedEmail = normalizedEmail,

                // Role is deliberately assigned by the server.
                Role = AppUserRole.Coach,

                IsActive = true
            };

            coachAccount.PasswordHash =
                _passwordHasher.HashPassword(
                    coachAccount,
                    password);

            _context.AppUsers.Add(coachAccount);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
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
                $"Coach account {coachAccount.Email} was created.";

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
        public async Task<IActionResult> SystemConfig(
            CancellationToken cancellationToken = default)
        {
            ViewBag.ActiveSettingsTab = "System";

            var settings =
                await _context.OrganisationSettings
                    .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                settings = new OrganisationSettings();
                _context.OrganisationSettings.Add(settings);
                await _context.SaveChangesAsync(cancellationToken);
            }

            ApplyLegacyOrganisationDefaults(settings);
            PopulateSystemInformation();

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SystemConfig(
            [Bind(
                "OrganisationName,OfficialEmail,OfficialWebsite,OfficialPhone," +
                "OfficeLocation,ActiveSeason,MinAttendancePercent," +
                "AcceptPlayerApplications,AllowEventRegistration," +
                "DefaultTeamName,DefaultProvince,DefaultCountry," +
                "MaintenanceMode,MaintenanceMessage")]
            OrganisationSettings input,
            IFormFile? logo,
            bool removeLogo = false,
            CancellationToken cancellationToken = default)
        {
            ViewBag.ActiveSettingsTab = "System";
            PopulateSystemInformation();

            var settings =
                await _context.OrganisationSettings
                    .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                return NotFound();
            }

            // Preserve the existing logo while validating the posted model.
            input.LogoPath = settings.LogoPath;

            var organisationName =
                input.OrganisationName?.Trim();

            if (string.IsNullOrWhiteSpace(organisationName))
            {
                ModelState.AddModelError(
                    nameof(input.OrganisationName),
                    "Organisation name is required.");
            }

            if (!int.TryParse(input.ActiveSeason, out var activeSeason) ||
                activeSeason < 2020 ||
                activeSeason > 2100)
            {
                ModelState.AddModelError(
                    nameof(input.ActiveSeason),
                    "Enter a valid four-digit season between 2020 and 2100.");
            }

            if (input.MaintenanceMode &&
                string.IsNullOrWhiteSpace(input.MaintenanceMessage))
            {
                ModelState.AddModelError(
                    nameof(input.MaintenanceMessage),
                    "Enter a message to show while maintenance mode is enabled.");
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            if (logo != null && logo.Length > 0)
            {
                if (!await UploadSecurity.IsSafeImageAsync(logo))
                {
                    ModelState.AddModelError(
                        "logo",
                        "Logo must be a valid JPG, PNG, or WebP file no larger than 2 MB.");

                    return View(input);
                }

                DeleteOrganisationLogoIfExists(settings.LogoPath);

                var extension =
                    Path.GetExtension(logo.FileName)
                        .ToLowerInvariant();

                // Stored inside the existing persistent Railway volume:
                // /app/wwwroot/uploads/reports
                var uploadsFolder =
                    Path.Combine(
                        _env.WebRootPath,
                        "uploads",
                        "reports",
                        "branding");

                Directory.CreateDirectory(uploadsFolder);

                var fileName =
                    $"{Guid.NewGuid():N}{extension}";

                var fullPath =
                    Path.Combine(uploadsFolder, fileName);

                await using var stream =
                    new FileStream(
                        fullPath,
                        FileMode.CreateNew);

                await logo.CopyToAsync(
                    stream,
                    cancellationToken);

                settings.LogoPath =
                    $"/uploads/reports/branding/{fileName}";
            }
            else if (removeLogo)
            {
                DeleteOrganisationLogoIfExists(settings.LogoPath);
                settings.LogoPath = null;
            }

            settings.OrganisationName = organisationName!;
            settings.OfficialEmail =
                NullIfWhiteSpace(input.OfficialEmail);
            settings.OfficialWebsite =
                NullIfWhiteSpace(input.OfficialWebsite);
            settings.OfficialPhone =
                NullIfWhiteSpace(input.OfficialPhone);
            settings.OfficeLocation =
                NullIfWhiteSpace(input.OfficeLocation);

            settings.ActiveSeason = activeSeason.ToString();
            settings.MinAttendancePercent =
                input.MinAttendancePercent;
            settings.AcceptPlayerApplications =
                input.AcceptPlayerApplications;
            settings.AllowEventRegistration =
                input.AllowEventRegistration;

            settings.DefaultTeamName =
                string.IsNullOrWhiteSpace(input.DefaultTeamName)
                    ? "ParaVolley Mpumalanga"
                    : input.DefaultTeamName.Trim();

            settings.DefaultProvince =
                string.IsNullOrWhiteSpace(input.DefaultProvince)
                    ? "Mpumalanga"
                    : input.DefaultProvince.Trim();

            settings.DefaultCountry =
                string.IsNullOrWhiteSpace(input.DefaultCountry)
                    ? "South Africa"
                    : input.DefaultCountry.Trim();

            settings.MaintenanceMode = input.MaintenanceMode;
            settings.MaintenanceMessage =
                string.IsNullOrWhiteSpace(input.MaintenanceMessage)
                    ? "ParaVolley Mpumalanga is temporarily undergoing maintenance. Please try again shortly."
                    : input.MaintenanceMessage.Trim();

            // These values are intentionally fixed until localization /
            // multi-timezone behaviour is implemented application-wide.
            settings.Timezone =
                "Africa/Johannesburg (SAST, UTC+2)";
            settings.Language = "English";

            await _context.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                "System configuration saved successfully.";

            return RedirectToAction(nameof(SystemConfig));
        }

        private void PopulateSystemInformation()
        {
            ViewBag.EnvironmentName = _env.EnvironmentName;
            ViewBag.DatabaseProvider =
                _context.Database.ProviderName?.Contains(
                    "Npgsql",
                    StringComparison.OrdinalIgnoreCase) == true
                    ? "PostgreSQL"
                    : (_context.Database.ProviderName ?? "Configured database");
            ViewBag.ServerTimezone = "SAST (UTC+2)";
            ViewBag.ApplicationVersion =
                typeof(SettingsController)
                    .Assembly
                    .GetName()
                    .Version?
                    .ToString(3)
                ?? "1.0.0";
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static void ApplyLegacyOrganisationDefaults(
            OrganisationSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.OfficialEmail))
            {
                settings.OfficialEmail =
                    "paravolleympumalanga@gmail.com";
            }

            if (string.IsNullOrWhiteSpace(settings.OfficialWebsite))
            {
                settings.OfficialWebsite =
                    "https://paravolley-production.up.railway.app/";
            }

            if (string.IsNullOrWhiteSpace(settings.DefaultTeamName))
            {
                settings.DefaultTeamName = "ParaVolley Mpumalanga";
            }

            if (string.IsNullOrWhiteSpace(settings.DefaultProvince))
            {
                settings.DefaultProvince = "Mpumalanga";
            }

            if (string.IsNullOrWhiteSpace(settings.DefaultCountry))
            {
                settings.DefaultCountry = "South Africa";
            }

            if (string.IsNullOrWhiteSpace(settings.MaintenanceMessage))
            {
                settings.MaintenanceMessage =
                    "ParaVolley Mpumalanga is temporarily undergoing maintenance. Please try again shortly.";
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile? photo)
        {
            var profile =
                await _context.UserProfiles.FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound();
            }

            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] =
                    "Please choose a photo to upload.";

                return RedirectToAction(nameof(Profile));
            }

            if (!await UploadSecurity.IsSafeImageAsync(photo))
            {
                TempData["Error"] =
                    "Photo must be a valid JPG, PNG, or WebP file no larger than 2 MB.";

                return RedirectToAction(nameof(Profile));
            }

            // Remove previous persistent avatar if one exists.
            if (!string.IsNullOrWhiteSpace(profile.AvatarPath))
            {
                var oldFileName =
                    Path.GetFileName(
                        profile.AvatarPath.Replace('\\', '/'));

                if (!string.IsNullOrWhiteSpace(oldFileName))
                {
                    var oldPath = Path.Combine(
                        _env.WebRootPath,
                        "uploads",
                        "reports",
                        "avatars",
                        oldFileName);

                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }
            }

            var extension =
                Path.GetExtension(photo.FileName)
                    .ToLowerInvariant();

            // Inside the existing Railway persistent volume.
            var uploadsFolder = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "reports",
                "avatars");

            Directory.CreateDirectory(uploadsFolder);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var fullPath =
                Path.Combine(
                    uploadsFolder,
                    fileName);

            await using var stream =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew);

            await photo.CopyToAsync(stream);

            profile.AvatarPath =
                $"/uploads/reports/avatars/{fileName}";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Profile photo updated.";

            return RedirectToAction(nameof(Profile));
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
        public async Task<IActionResult> Appearance(
    AppTheme theme,
    string? accentColor,
    LayoutDensity density,
    CancellationToken cancellationToken = default)
        {
            var allowedAccentColors =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
            "#0B6E4F",
            "#2563EB",
            "#7C3AED",
            "#DC2626",
            "#C2410C",
            "#0EA5E9",
            "#16A34A"
                };

            var selectedColor =
                accentColor?.Trim();

            if (!Enum.IsDefined(typeof(AppTheme), theme) ||
                !Enum.IsDefined(typeof(LayoutDensity), density) ||
                string.IsNullOrWhiteSpace(selectedColor) ||
                !allowedAccentColors.Contains(selectedColor))
            {
                TempData["Error"] =
                    "The selected appearance settings are invalid.";

                return RedirectToAction(nameof(Appearance));
            }

            var settings =
                await _context.AppearanceSettings
                    .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                return NotFound();
            }

            settings.Theme = theme;
            settings.AccentColor =
                selectedColor.ToUpperInvariant();
            settings.Density = density;

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["Success"] =
                "Dashboard appearance updated.";

            return RedirectToAction(nameof(Appearance));
        }

        // ================= BACKUP & DATA =================
        public async Task<IActionResult> BackupData(
            string? search,
            string? status,
            CancellationToken cancellationToken = default)
        {
            ViewBag.ActiveSettingsTab = "BackupData";

            var normalizedStatus =
                string.IsNullOrWhiteSpace(status)
                    ? "all"
                    : status.Trim().ToLowerInvariant();

            var normalizedSearch =
                string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim();

            var allQuery =
                _context.BackupRecords
                    .AsNoTracking();

            var totalCount =
                await allQuery.CountAsync(
                    cancellationToken);

            var successfulCount =
                await allQuery.CountAsync(
                    record => record.Success,
                    cancellationToken);

            var failedCount =
                totalCount - successfulCount;

            var query =
                _context.BackupRecords
                    .AsNoTracking();

            if (normalizedSearch != null)
            {
                var lowerSearch =
                    normalizedSearch.ToLowerInvariant();

                if (int.TryParse(
                        normalizedSearch,
                        out var backupId))
                {
                    query =
                        query.Where(record =>
                            record.Id == backupId ||
                            (record.Note != null &&
                             record.Note.ToLower()
                                 .Contains(lowerSearch)));
                }
                else
                {
                    query =
                        query.Where(record =>
                            record.Note != null &&
                            record.Note.ToLower()
                                .Contains(lowerSearch));
                }
            }

            query =
                normalizedStatus switch
                {
                    "success" =>
                        query.Where(record =>
                            record.Success),

                    "failed" =>
                        query.Where(record =>
                            !record.Success),

                    "manual" =>
                        query.Where(record =>
                            !record.IsAutomatic),

                    "automatic" =>
                        query.Where(record =>
                            record.IsAutomatic),

                    _ => query
                };

            var backups =
                await query
                    .OrderByDescending(
                        record => record.CreatedAt)
                    .Take(100)
                    .ToListAsync(
                        cancellationToken);

            var settings =
                await _context.BackupSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        cancellationToken)
                ?? new BackupSettings();

            var model =
                new BackupDataViewModel
                {
                    Backups = backups,
                    Settings = settings,
                    Search = normalizedSearch,
                    Status = normalizedStatus,
                    TotalCount = totalCount,
                    SuccessfulCount = successfulCount,
                    FailedCount = failedCount,
                    NextScheduledRunAt =
                        BackupService.GetNextScheduledRun(
                            settings)
                };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> CreateBackup(
            string? note,
            CancellationToken cancellationToken = default)
        {
            if (note?.Trim().Length > 200)
            {
                TempData["Error"] =
                    "Snapshot notes cannot exceed 200 characters.";

                return RedirectToAction(
                    nameof(BackupData));
            }

            try
            {
                var backup =
                    await _backupService.CreateSnapshotAsync(
                        note,
                        isAutomatic: false,
                        cancellationToken);

                var settings =
                    await _context.BackupSettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            cancellationToken);

                var retentionCount =
                    settings?.RetentionCount ?? 30;

                var removed =
                    await _backupService.ApplyRetentionAsync(
                        retentionCount,
                        cancellationToken);

                TempData["Success"] =
                    removed > 0
                        ? $"Snapshot #{backup.Id} created successfully. " +
                          $"{removed} older snapshot(s) were removed by the retention policy."
                        : $"Snapshot #{backup.Id} created successfully.";
            }
            catch (Exception exception)
            {
                TempData["Error"] =
                    _env.IsDevelopment()
                        ? $"Snapshot failed: {exception.GetBaseException().Message}"
                        : "The data snapshot could not be created.";
            }

            return RedirectToAction(
                nameof(BackupData));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> DeleteBackup(
            int id,
            CancellationToken cancellationToken = default)
        {
            var backup =
                await _context.BackupRecords
                    .FirstOrDefaultAsync(
                        record => record.Id == id,
                        cancellationToken);

            if (backup == null)
            {
                return NotFound();
            }

            try
            {
                await _backupService.DeleteBackupAsync(
                    backup,
                    cancellationToken);

                TempData["Success"] =
                    $"Snapshot #{id} was deleted.";
            }
            catch
            {
                TempData["Error"] =
                    "The snapshot could not be deleted.";
            }

            return RedirectToAction(
                nameof(BackupData));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> CleanFailedBackups(
            CancellationToken cancellationToken = default)
        {
            var removed =
                await _backupService.CleanFailedRecordsAsync(
                    cancellationToken);

            TempData["Success"] =
                removed == 0
                    ? "There were no failed snapshot records to remove."
                    : $"{removed} failed snapshot record(s) were removed.";

            return RedirectToAction(
                nameof(BackupData));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> RunRetentionCleanup(
            CancellationToken cancellationToken = default)
        {
            var settings =
                await _context.BackupSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        cancellationToken)
                ?? new BackupSettings();

            var removed =
                await _backupService.ApplyRetentionAsync(
                    settings.RetentionCount,
                    cancellationToken);

            TempData["Success"] =
                removed == 0
                    ? "No snapshots needed to be removed."
                    : $"{removed} old snapshot(s) were removed by the retention policy.";

            return RedirectToAction(
                nameof(BackupData));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> SaveBackupSchedule(
            bool automaticEnabled,
            BackupScheduleFrequency frequency,
            int hourSast,
            DayOfWeek weeklyDay,
            int retentionCount,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(
                    typeof(BackupScheduleFrequency),
                    frequency) ||
                !Enum.IsDefined(
                    typeof(DayOfWeek),
                    weeklyDay) ||
                hourSast is < 0 or > 23 ||
                retentionCount is < 5 or > 100)
            {
                TempData["Error"] =
                    "The backup schedule settings are invalid.";

                return RedirectToAction(
                    nameof(BackupData));
            }

            var settings =
                await _context.BackupSettings
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (settings == null)
            {
                settings =
                    new BackupSettings
                    {
                        Id = 1
                    };

                _context.BackupSettings.Add(
                    settings);
            }

            settings.AutomaticEnabled =
                automaticEnabled;

            settings.Frequency =
                frequency;

            settings.HourSast =
                hourSast;

            settings.WeeklyDay =
                weeklyDay;

            settings.RetentionCount =
                retentionCount;

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["Success"] =
                "Backup schedule settings were saved.";

            return RedirectToAction(
                nameof(BackupData));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> VerifyBackup(
            int id,
            CancellationToken cancellationToken = default)
        {
            var backup =
                await _context.BackupRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record =>
                            record.Id == id &&
                            record.Success,
                        cancellationToken);

            if (backup == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(
                    backup.Sha256))
            {
                TempData["Error"] =
                    "This is a legacy snapshot and does not have a stored integrity checksum. " +
                    "Create a new snapshot to use verification.";

                return RedirectToAction(
                    nameof(BackupData));
            }

            var result =
                await _backupService.VerifyIntegrityAsync(
                    backup,
                    cancellationToken);

            TempData[result == true
                ? "Success"
                : "Error"] =
                result == true
                    ? $"Snapshot #{id} passed the SHA-256 integrity check."
                    : $"Snapshot #{id} failed the integrity check or its file is missing.";

            return RedirectToAction(
                nameof(BackupData));
        }


        public async Task<IActionResult> BackupDetails(
            int id,
            CancellationToken cancellationToken = default)
        {
            ViewBag.ActiveSettingsTab = "BackupData";

            var backup =
                await _context.BackupRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record => record.Id == id,
                        cancellationToken);

            if (backup == null)
            {
                return NotFound();
            }

            var path =
                backup.Success
                    ? _backupService.FindSnapshotPath(
                        backup)
                    : null;

            var counts =
                path != null
                    ? await _backupService.GetRecordCountsAsync(
                        backup,
                        cancellationToken)
                    : new Dictionary<string, int>();

            bool? integrityValid = null;

            if (path != null &&
                !string.IsNullOrWhiteSpace(
                    backup.Sha256))
            {
                integrityValid =
                    await _backupService.VerifyIntegrityAsync(
                        backup,
                        cancellationToken);
            }

            var model =
                new BackupSnapshotDetailsViewModel
                {
                    Backup = backup,
                    RecordCounts = counts,
                    FileExists = path != null,
                    IntegrityValid = integrityValid
                };

            return View(model);
        }


        public async Task<IActionResult> DownloadBackup(
            int id,
            CancellationToken cancellationToken = default)
        {
            var backup =
                await _context.BackupRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record =>
                            record.Id == id &&
                            record.Success,
                        cancellationToken);

            if (backup == null)
            {
                return NotFound();
            }

            var backupPath =
                _backupService.FindSnapshotPath(
                    backup);

            if (backupPath == null)
            {
                TempData["Error"] =
                    "This snapshot file is no longer available.";

                return RedirectToAction(
                    nameof(BackupData));
            }

            return PhysicalFile(
                backupPath,
                "application/json",
                Path.GetFileName(backupPath),
                enableRangeProcessing: true);
        }


        public async Task<IActionResult> DownloadExcelBackup(
            int id,
            CancellationToken cancellationToken = default)
        {
            var backup =
                await _context.BackupRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record =>
                            record.Id == id &&
                            record.Success,
                        cancellationToken);

            if (backup == null)
            {
                return NotFound();
            }

            var backupPath =
                _backupService.FindSnapshotPath(
                    backup);

            if (backupPath == null)
            {
                TempData["Error"] =
                    "This snapshot file is no longer available.";

                return RedirectToAction(
                    nameof(BackupData));
            }

            try
            {
                var bytes =
                    await BuildExcelBackupAsync(
                        backup,
                        backupPath,
                        cancellationToken);

                var excelFileName =
                    $"ParaVolley_Snapshot_{backup.Id}_" +
                    $"{backup.CreatedAt:yyyyMMdd_HHmm}.xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    excelFileName);
            }
            catch
            {
                TempData["Error"] =
                    "The Excel version of this snapshot could not be generated.";

                return RedirectToAction(
                    nameof(BackupData));
            }
        }


        public async Task<IActionResult> DownloadBackupBundle(
            int id,
            CancellationToken cancellationToken = default)
        {
            var backup =
                await _context.BackupRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record =>
                            record.Id == id &&
                            record.Success,
                        cancellationToken);

            if (backup == null)
            {
                return NotFound();
            }

            var backupPath =
                _backupService.FindSnapshotPath(
                    backup);

            if (backupPath == null)
            {
                TempData["Error"] =
                    "This snapshot file is no longer available.";

                return RedirectToAction(
                    nameof(BackupData));
            }

            try
            {
                var excelBytes =
                    await BuildExcelBackupAsync(
                        backup,
                        backupPath,
                        cancellationToken);

                using var output =
                    new MemoryStream();

                using (var archive =
                       new ZipArchive(
                           output,
                           ZipArchiveMode.Create,
                           leaveOpen: true))
                {
                    var jsonEntry =
                        archive.CreateEntry(
                            Path.GetFileName(backupPath),
                            CompressionLevel.Optimal);

                    await using (var jsonTarget =
                                 jsonEntry.Open())
                    await using (var jsonSource =
                                 System.IO.File.OpenRead(
                                     backupPath))
                    {
                        await jsonSource.CopyToAsync(
                            jsonTarget,
                            cancellationToken);
                    }

                    var excelName =
                        $"ParaVolley_Snapshot_{backup.Id}_" +
                        $"{backup.CreatedAt:yyyyMMdd_HHmm}.xlsx";

                    var excelEntry =
                        archive.CreateEntry(
                            excelName,
                            CompressionLevel.Optimal);

                    await using (var excelStream =
                                 excelEntry.Open())
                    {
                        await excelStream.WriteAsync(
                            excelBytes,
                            cancellationToken);
                    }

                    var manifestEntry =
                        archive.CreateEntry(
                            "snapshot-info.txt",
                            CompressionLevel.Optimal);

                    await using (var manifestStream =
                                 new StreamWriter(
                                     manifestEntry.Open()))
                    {
                        await manifestStream.WriteLineAsync(
                            "ParaVolley Mpumalanga Snapshot Bundle");

                        await manifestStream.WriteLineAsync(
                            $"Snapshot ID: {backup.Id}");

                        await manifestStream.WriteLineAsync(
                            $"Created: {backup.CreatedAt:yyyy-MM-dd HH:mm} SAST");

                        await manifestStream.WriteLineAsync(
                            $"Type: {(backup.IsAutomatic ? "Automatic" : "Manual")}");

                        await manifestStream.WriteLineAsync(
                            $"Note: {backup.Note ?? "-"}");

                        await manifestStream.WriteLineAsync(
                            $"SHA-256: {backup.Sha256 ?? "Legacy snapshot - unavailable"}");
                    }
                }

                return File(
                    output.ToArray(),
                    "application/zip",
                    $"ParaVolley_Snapshot_{backup.Id}_{backup.CreatedAt:yyyyMMdd_HHmm}.zip");
            }
            catch
            {
                TempData["Error"] =
                    "The snapshot ZIP bundle could not be generated.";

                return RedirectToAction(
                    nameof(BackupData));
            }
        }


        private static async Task<byte[]> BuildExcelBackupAsync(
            BackupRecord backup,
            string backupPath,
            CancellationToken cancellationToken)
        {
            await using var jsonStream =
                System.IO.File.OpenRead(
                    backupPath);

            using var document =
                await JsonDocument.ParseAsync(
                    jsonStream,
                    cancellationToken:
                        cancellationToken);

            using var workbook =
                new XLWorkbook();

            var summary =
                workbook.Worksheets.Add(
                    "Summary");

            summary.Cell("A1").Value =
                "ParaVolley Mpumalanga Data Snapshot";

            summary.Cell("A1").Style.Font.Bold =
                true;

            summary.Cell("A1").Style.Font.FontSize =
                16;

            summary.Cell("A3").Value =
                "Snapshot ID";

            summary.Cell("B3").Value =
                backup.Id;

            summary.Cell("A4").Value =
                "Created";

            summary.Cell("B4").Value =
                backup.CreatedAt;

            summary.Cell("B4")
                .Style.DateFormat.Format =
                "yyyy-mm-dd hh:mm";

            summary.Cell("A5").Value =
                "Type";

            summary.Cell("B5").Value =
                backup.IsAutomatic
                    ? "Automatic"
                    : "Manual";

            summary.Cell("A6").Value =
                "Note";

            summary.Cell("B6").Value =
                backup.Note ?? "-";

            summary.Cell("A7").Value =
                "SHA-256";

            summary.Cell("B7").Value =
                backup.Sha256 ??
                "Legacy snapshot - unavailable";

            summary.Cell("A9").Value =
                "Important";

            summary.Cell("B9").Value =
                "This workbook contains administrative ParaVolley data and should be stored securely.";

            summary.Cell("A9").Style.Font.Bold =
                true;

            summary.Column("A").Width = 20;
            summary.Column("B").Width = 65;
            summary.Column("B")
                .Style.Alignment.WrapText = true;

            var root =
                document.RootElement;

            var sheetDefinitions =
                new (string JsonName, string SheetName)[]
                {
                    ("Players", "Players"),
                    ("Coaches", "Coaches"),
                    ("Events", "Events"),
                    ("Matches", "Matches"),
                    ("Announcements", "Announcements"),
                    ("Attendances", "Attendance"),
                    ("EventRegistrations", "Event Registrations"),
                    ("Sponsors", "Sponsors"),
                    ("Reports", "Reports")
                };

            var summaryRow = 12;

            summary.Cell(
                summaryRow,
                1).Value = "Dataset";

            summary.Cell(
                summaryRow,
                2).Value = "Records";

            var summaryHeader =
                summary.Range(
                    summaryRow,
                    1,
                    summaryRow,
                    2);

            summaryHeader.Style.Font.Bold = true;
            summaryHeader.Style.Font.FontColor =
                XLColor.White;
            summaryHeader.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#0B6E4F");

            foreach (var definition
                     in sheetDefinitions)
            {
                AddJsonArrayWorksheet(
                    workbook,
                    root,
                    definition.JsonName,
                    definition.SheetName);

                var count =
                    root.TryGetProperty(
                        definition.JsonName,
                        out var array) &&
                    array.ValueKind ==
                        JsonValueKind.Array
                        ? array.GetArrayLength()
                        : 0;

                summaryRow++;

                summary.Cell(
                    summaryRow,
                    1).Value =
                    FormatHeader(
                        definition.JsonName);

                summary.Cell(
                    summaryRow,
                    2).Value =
                    count;
            }

            using var outputStream =
                new MemoryStream();

            workbook.SaveAs(
                outputStream);

            return outputStream.ToArray();
        }


        private static void AddJsonArrayWorksheet(
            XLWorkbook workbook,
            JsonElement root,
            string jsonPropertyName,
            string worksheetName)
        {
            var worksheet =
                workbook.Worksheets.Add(
                    worksheetName);

            if (!root.TryGetProperty(
                    jsonPropertyName,
                    out var array) ||
                array.ValueKind !=
                    JsonValueKind.Array ||
                array.GetArrayLength() == 0)
            {
                worksheet.Cell("A1").Value =
                    "No records in this snapshot.";

                worksheet.Cell("A1")
                    .Style.Font.Italic = true;

                worksheet.Cell("A1")
                    .Style.Font.FontColor =
                    XLColor.Gray;

                return;
            }

            var headers =
                new List<string>();

            foreach (var item
                     in array.EnumerateArray())
            {
                if (item.ValueKind !=
                    JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var property
                         in item.EnumerateObject())
                {
                    if (property.Value.ValueKind ==
                            JsonValueKind.Object ||
                        property.Value.ValueKind ==
                            JsonValueKind.Array)
                    {
                        continue;
                    }

                    if (!headers.Contains(
                            property.Name,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        headers.Add(
                            property.Name);
                    }
                }
            }

            if (headers.Count == 0)
            {
                worksheet.Cell("A1").Value =
                    "No readable records in this snapshot.";

                return;
            }

            for (var column = 0;
                 column < headers.Count;
                 column++)
            {
                var cell =
                    worksheet.Cell(
                        1,
                        column + 1);

                cell.Value =
                    FormatHeader(
                        headers[column]);
            }

            var headerRange =
                worksheet.Range(
                    1,
                    1,
                    1,
                    headers.Count);

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor =
                XLColor.White;
            headerRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#0B6E4F");
            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            var row = 2;

            foreach (var item
                     in array.EnumerateArray())
            {
                if (item.ValueKind !=
                    JsonValueKind.Object)
                {
                    continue;
                }

                for (var column = 0;
                     column < headers.Count;
                     column++)
                {
                    var propertyName =
                        headers[column];

                    if (!item.TryGetProperty(
                            propertyName,
                            out var value))
                    {
                        continue;
                    }

                    var cell =
                        worksheet.Cell(
                            row,
                            column + 1);

                    WriteExcelValue(
                        cell,
                        jsonPropertyName,
                        propertyName,
                        value);
                }

                row++;
            }

            var lastRow =
                Math.Max(
                    row - 1,
                    1);

            var usedRange =
                worksheet.Range(
                    1,
                    1,
                    lastRow,
                    headers.Count);

            usedRange.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            usedRange.Style.Border.BottomBorderColor =
                XLColor.FromHtml("#DEE2E6");

            worksheet.SheetView.FreezeRows(1);

            usedRange.SetAutoFilter();

            worksheet.Columns()
                .AdjustToContents();

            foreach (var column
                     in worksheet.ColumnsUsed())
            {
                if (column.Width > 40)
                {
                    column.Width = 40;
                }

                if (column.Width < 10)
                {
                    column.Width = 10;
                }
            }

            worksheet.RowsUsed()
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Top;

            worksheet.CellsUsed()
                .Style.Alignment.WrapText = true;
        }


        private static void WriteExcelValue(
            IXLCell cell,
            string sectionName,
            string propertyName,
            JsonElement value)
        {
            if (value.ValueKind ==
                JsonValueKind.Null)
            {
                cell.Value = string.Empty;
                return;
            }

            if (value.ValueKind ==
                    JsonValueKind.Number &&
                value.TryGetInt32(
                    out var enumNumber))
            {
                var enumText =
                    GetReadableEnumValue(
                        sectionName,
                        propertyName,
                        enumNumber);

                if (enumText != null)
                {
                    cell.Value = enumText;
                    return;
                }
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                {
                    var text =
                        value.GetString()
                        ?? string.Empty;

                    var looksLikeDate =
                        propertyName.Contains(
                            "Date",
                            StringComparison.OrdinalIgnoreCase) ||
                        propertyName.Contains(
                            "At",
                            StringComparison.OrdinalIgnoreCase);

                    if (looksLikeDate &&
                        DateTime.TryParse(
                            text,
                            out var date))
                    {
                        cell.Value = date;

                        cell.Style.DateFormat.Format =
                            propertyName.Equals(
                                "Date",
                                StringComparison.OrdinalIgnoreCase)
                                ? "yyyy-mm-dd"
                                : "yyyy-mm-dd hh:mm";

                        return;
                    }

                    cell.Value = text;
                    return;
                }

                case JsonValueKind.Number:
                {
                    if (value.TryGetInt64(
                            out var wholeNumber))
                    {
                        cell.Value =
                            wholeNumber;
                    }
                    else if (value.TryGetDouble(
                                 out var decimalNumber))
                    {
                        cell.Value =
                            decimalNumber;
                    }

                    return;
                }

                case JsonValueKind.True:
                    cell.Value = "Yes";
                    return;

                case JsonValueKind.False:
                    cell.Value = "No";
                    return;

                default:
                    cell.Value =
                        value.ToString();
                    return;
            }
        }


        private static string? GetReadableEnumValue(
            string sectionName,
            string propertyName,
            int value)
        {
            string? enumName =
                (sectionName, propertyName) switch
                {
                    ("Players", "Status") =>
                        Enum.GetName(
                            typeof(PlayerStatus),
                            value),

                    ("Coaches", "Status") =>
                        Enum.GetName(
                            typeof(CoachStatus),
                            value),

                    ("Events", "Type") =>
                        Enum.GetName(
                            typeof(EventType),
                            value),

                    ("Events", "Status") =>
                        Enum.GetName(
                            typeof(EventStatus),
                            value),

                    ("Matches", "Status") =>
                        Enum.GetName(
                            typeof(MatchStatus),
                            value),

                    ("Announcements", "Category") =>
                        Enum.GetName(
                            typeof(AnnouncementCategory),
                            value),

                    ("Attendances", "Status") =>
                        Enum.GetName(
                            typeof(AttendanceStatus),
                            value),

                    ("EventRegistrations", "Status") =>
                        Enum.GetName(
                            typeof(EventRegistrationStatus),
                            value),

                    ("Sponsors", "Tier") =>
                        Enum.GetName(
                            typeof(SponsorTier),
                            value),

                    ("Reports", "Type") =>
                        Enum.GetName(
                            typeof(ReportType),
                            value),

                    ("Reports", "Status") =>
                        Enum.GetName(
                            typeof(ReportStatus),
                            value),

                    _ => null
                };

            return enumName == null
                ? null
                : FormatHeader(enumName);
        }


        private static string FormatHeader(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return System.Text.RegularExpressions.Regex.Replace(
                text,
                "([a-z0-9])([A-Z])",
                "$1 $2");
        }


        private void DeleteOrganisationLogoIfExists(
            string? logoPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return;
            }

            var normalizedPath =
                logoPath.Replace('\\', '/');

            var fileName =
                Path.GetFileName(normalizedPath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            string? folder = null;

            // Current persistent Railway location.
            if (normalizedPath.StartsWith(
                "/uploads/reports/branding/",
                StringComparison.OrdinalIgnoreCase))
            {
                folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "reports",
                    "branding");
            }

            // Support logos created by the older implementation.
            else if (normalizedPath.StartsWith(
                "/uploads/branding/",
                StringComparison.OrdinalIgnoreCase))
            {
                folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "branding");
            }

            if (folder == null)
            {
                return;
            }

            var fullPath =
                Path.Combine(
                    folder,
                    fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}