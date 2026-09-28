using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
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
    public class CoachesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly PasswordResetTokenService _passwordResetTokens;

        public CoachesController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IPasswordHasher<AppUser> passwordHasher,
            PasswordResetTokenService passwordResetTokens)
        {
            _context = context;
            _env = env;
            _passwordHasher = passwordHasher;
            _passwordResetTokens = passwordResetTokens;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }


        // ============================================================
        // COACHES INDEX
        // ============================================================

        // GET: Coaches
        public async Task<IActionResult> Index(
            string? search,
            string? team,
            string? status,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);

            var query = _context.Coaches
                .AsNoTracking()
                .AsQueryable();


            // Case-insensitive search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();

                query = query.Where(coach =>
                    EF.Functions.ILike(
                        coach.Name,
                        $"%{searchTerm}%") ||
                    EF.Functions.ILike(
                        coach.Specialty,
                        $"%{searchTerm}%"));
            }


            // Team filter
            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(coach =>
                    coach.AssignedTeam == team);
            }


            // Status filter
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<CoachStatus>(
                    status,
                    out var parsedStatus))
            {
                query = query.Where(coach =>
                    coach.Status == parsedStatus);
            }


            // Dashboard statistics
            var coachCounts = await _context.Coaches
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),

                    Active = group.Count(coach =>
                        coach.Status == CoachStatus.Active),

                    Available = group.Count(coach =>
                        coach.Status == CoachStatus.Available)
                })
                .SingleOrDefaultAsync(cancellationToken);


            ViewBag.TotalCount =
                coachCounts?.Total ?? 0;

            ViewBag.ActiveCount =
                coachCounts?.Active ?? 0;

            ViewBag.AvailableCount =
                coachCounts?.Available ?? 0;


            ViewBag.TeamsAssignedCount =
                await _context.Coaches
                    .AsNoTracking()
                    .Where(coach =>
                        !string.IsNullOrWhiteSpace(
                            coach.AssignedTeam))
                    .Select(coach =>
                        coach.AssignedTeam)
                    .Distinct()
                    .CountAsync(cancellationToken);


            // Pagination
            var filteredCount =
                await query.CountAsync(cancellationToken);

            ViewBag.FilteredCount = filteredCount;

            var pageCount = Paging.PageCount(
                filteredCount,
                Paging.DefaultPageSize);

            page = Math.Min(page, pageCount);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;


            // Preserve current filters
            ViewBag.Search = search;
            ViewBag.SelectedTeam = team;
            ViewBag.SelectedStatus = status;


            // Dynamic teams used by the filter
            ViewBag.Teams =
                await GetTeamOptionsAsync(
                    cancellationToken);


            var coaches = await query
                .OrderBy(coach => coach.Name)
                .ThenBy(coach => coach.Id)
                .Skip(
                    (page - 1) *
                    Paging.DefaultPageSize)
                .Take(Paging.DefaultPageSize)
                .ToListAsync(cancellationToken);


            return View(coaches);
        }


        // ============================================================
        // EXPORT
        // ============================================================

        // GET: Coaches/Export
        public async Task<IActionResult> Export(
            string? search,
            string? team,
            string? status,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Coaches
                .AsNoTracking()
                .AsQueryable();


            // Case-insensitive search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();

                query = query.Where(coach =>
                    EF.Functions.ILike(
                        coach.Name,
                        $"%{searchTerm}%") ||
                    EF.Functions.ILike(
                        coach.Specialty,
                        $"%{searchTerm}%"));
            }


            if (!string.IsNullOrWhiteSpace(team))
            {
                query = query.Where(coach =>
                    coach.AssignedTeam == team);
            }


            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<CoachStatus>(
                    status,
                    out var parsedStatus))
            {
                query = query.Where(coach =>
                    coach.Status == parsedStatus);
            }


            var coaches = await query
                .OrderBy(coach => coach.Name)
                .ToListAsync(cancellationToken);


            var sb =
                new System.Text.StringBuilder();

            sb.AppendLine(
                "Name,Email,Phone,Specialty,Experience," +
                "Certifications,AssignedTeam,Status");


            foreach (var coach in coaches)
            {
                var exportEmail =
                    LoginIdentifierHelper.IsPlaceholderEmail(
                        coach.Email)
                        ? string.Empty
                        : coach.Email;

                sb.AppendLine(
                    $"\"{coach.Name}\"," +
                    $"\"{exportEmail}\"," +
                    $"\"{coach.Phone}\"," +
                    $"\"{coach.Specialty}\"," +
                    $"\"{coach.Experience}\"," +
                    $"\"{coach.Certifications}\"," +
                    $"\"{coach.AssignedTeam}\"," +
                    $"\"{coach.Status}\"");
            }


            var bytes =
                System.Text.Encoding.UTF8
                    .GetBytes(sb.ToString());


            return File(
                bytes,
                "text/csv",
                $"coaches_{DateTime.Now:yyyyMMdd}.csv");
        }


        // ============================================================
        // ASSIGN TEAM
        // ============================================================

        // GET: Coaches/AssignTeam/5
        public async Task<IActionResult> AssignTeam(
            int? id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }


            var coach = await _context.Coaches
                .FindAsync(
                    new object?[] { id.Value },
                    cancellationToken);


            if (coach == null)
            {
                return NotFound();
            }

            if (LoginIdentifierHelper.IsPlaceholderEmail(
                    coach.Email))
            {
                coach.Email = string.Empty;
            }


            ViewBag.Teams =
                await GetTeamOptionsAsync(
                    cancellationToken);


            if (IsAjaxRequest())
            {
                return PartialView(
                    "_AssignTeamPartial",
                    coach);
            }


            return View(coach);
        }


        // POST: Coaches/AssignTeam/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeam(
            int id,
            string? assignedTeam,
            CancellationToken cancellationToken = default)
        {
            var coach = await _context.Coaches
                .FindAsync(
                    new object?[] { id },
                    cancellationToken);


            if (coach == null)
            {
                return NotFound();
            }


            coach.AssignedTeam =
                string.IsNullOrWhiteSpace(assignedTeam)
                    ? null
                    : assignedTeam.Trim();


            await _context.SaveChangesAsync(
                cancellationToken);


            TempData["Success"] =
                string.IsNullOrWhiteSpace(assignedTeam)
                    ? $"{coach.Name} was unassigned from a team."
                    : $"{coach.Name} was assigned to {assignedTeam.Trim()}.";


            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true
                });
            }


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // DETAILS
        // ============================================================

        // GET: Coaches/Details/5
        public async Task<IActionResult> Details(
            int? id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }


            var coach = await _context.Coaches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    coach => coach.Id == id,
                    cancellationToken);


            if (coach == null)
            {
                return NotFound();
            }

            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    coach.Phone);

            var normalizedEmail =
                LoginIdentifierHelper.NormalizeEmail(
                    coach.Email);

            ViewBag.CoachAppUser =
                await _context.AppUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        user =>
                            user.Role == AppUserRole.Coach &&
                            ((normalizedPhone != null &&
                              user.NormalizedPhone ==
                                normalizedPhone) ||
                             user.NormalizedEmail ==
                                normalizedEmail),
                        cancellationToken);


            if (IsAjaxRequest())
            {
                return PartialView(
                    "_DetailsPartial",
                    coach);
            }


            return View(coach);
        }


        // ============================================================
        // CREATE
        // ============================================================

        // GET: Coaches/Create
        public async Task<IActionResult> Create(
            CancellationToken cancellationToken = default)
        {
            ViewBag.Teams =
                await GetTeamOptionsAsync(
                    cancellationToken);


            var coach = new Coach();


            if (IsAjaxRequest())
            {
                return PartialView(
                    "_CreatePartial",
                    coach);
            }


            return View(coach);
        }


        // POST: Coaches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "Name,Email,Phone,AssignedTeam,Status," +
                "Specialty,Experience,Certifications")]
            Coach coach,
            IFormFile? photo,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Teams =
                    await GetTeamOptionsAsync(
                        cancellationToken);


                if (IsAjaxRequest())
                {
                    return PartialView(
                        "_CreatePartial",
                        coach);
                }


                return View(coach);
            }


            if (photo != null &&
                photo.Length > 0)
            {
                if (!await UploadSecurity
                    .IsSafeImageAsync(photo))
                {
                    ModelState.AddModelError(
                        "photo",
                        "Photo must be a valid JPG, PNG, " +
                        "or WebP file no larger than 2 MB.");


                    ViewBag.Teams =
                        await GetTeamOptionsAsync(
                            cancellationToken);


                    return IsAjaxRequest()
                        ? PartialView(
                            "_CreatePartial",
                            coach)
                        : View(coach);
                }


                var savedPath =
                    await SaveCoachPhotoAsync(photo);

                coach.AvatarPath = savedPath;
            }


            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    coach.Phone);

            if (normalizedPhone == null)
            {
                ModelState.AddModelError(
                    nameof(Coach.Phone),
                    "Enter a valid WhatsApp/mobile number.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Teams =
                    await GetTeamOptionsAsync(
                        cancellationToken);

                return IsAjaxRequest()
                    ? PartialView(
                        "_CreatePartial",
                        coach)
                    : View(coach);
            }

            var storageEmail =
                LoginIdentifierHelper.BuildStorageEmail(
                    coach.Email,
                    "coach",
                    normalizedPhone!);

            var phoneAlreadyUsed =
                await _context.AppUsers
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.NormalizedPhone ==
                            normalizedPhone,
                        cancellationToken);

            var emailAlreadyUsed =
                await _context.AppUsers
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.NormalizedEmail ==
                            storageEmail,
                        cancellationToken);

            if (phoneAlreadyUsed)
            {
                ModelState.AddModelError(
                    nameof(Coach.Phone),
                    "That phone number is already used by another login account.");
            }

            if (emailAlreadyUsed)
            {
                ModelState.AddModelError(
                    nameof(Coach.Email),
                    "That email address is already used by another login account.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Teams =
                    await GetTeamOptionsAsync(
                        cancellationToken);

                return IsAjaxRequest()
                    ? PartialView(
                        "_CreatePartial",
                        coach)
                    : View(coach);
            }

            coach.Name = coach.Name.Trim();
            coach.Email = storageEmail;
            coach.Phone = coach.Phone.Trim();

            coach.AssignedTeam =
                string.IsNullOrWhiteSpace(
                    coach.AssignedTeam)
                    ? null
                    : coach.AssignedTeam.Trim();

            var account =
                CreateCoachAccount(
                    coach,
                    normalizedPhone!);

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                _context.Coaches.Add(coach);
                _context.AppUsers.Add(account);

                await _context.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);
                throw;
            }

            TempData["Success"] =
                $"Coach \"{coach.Name}\" was created with a Coach Portal account. Open the coach profile to send their password setup link on WhatsApp.";


            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true
                });
            }


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // EDIT
        // ============================================================

        // GET: Coaches/Edit/5
        public async Task<IActionResult> Edit(
            int? id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }


            var coach = await _context.Coaches
                .FindAsync(
                    new object?[] { id.Value },
                    cancellationToken);


            if (coach == null)
            {
                return NotFound();
            }

            if (LoginIdentifierHelper.IsPlaceholderEmail(
                    coach.Email))
            {
                coach.Email = string.Empty;
            }


            ViewBag.Teams =
                await GetTeamOptionsAsync(
                    cancellationToken);


            if (IsAjaxRequest())
            {
                return PartialView(
                    "_EditPartial",
                    coach);
            }


            return View(coach);
        }


        // POST: Coaches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "Id,Name,Email,Phone,AssignedTeam,Status," +
                "Specialty,Experience,Certifications")]
            Coach input,
            IFormFile? photo,
            bool removePhoto = false,
            CancellationToken cancellationToken = default)
        {
            if (id != input.Id)
            {
                return NotFound();
            }


            var coach = await _context.Coaches
                .FindAsync(
                    new object?[] { id },
                    cancellationToken);


            if (coach == null)
            {
                return NotFound();
            }

            var existingPhone =
                LoginIdentifierHelper.NormalizePhone(
                    coach.Phone);

            var existingEmail =
                LoginIdentifierHelper.NormalizeEmail(
                    coach.Email);

            var linkedAccount =
                await _context.AppUsers
                    .FirstOrDefaultAsync(
                        user =>
                            user.Role == AppUserRole.Coach &&
                            ((existingPhone != null &&
                              user.NormalizedPhone ==
                                existingPhone) ||
                             user.NormalizedEmail ==
                                existingEmail),
                        cancellationToken);

            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    input.Phone);

            if (normalizedPhone == null)
            {
                ModelState.AddModelError(
                    nameof(Coach.Phone),
                    "Enter a valid WhatsApp/mobile number.");
            }

            var storageEmail =
                normalizedPhone == null
                    ? LoginIdentifierHelper.NormalizeEmail(
                        input.Email)
                    : LoginIdentifierHelper.BuildStorageEmail(
                        input.Email,
                        "coach",
                        normalizedPhone);

            if (normalizedPhone != null)
            {
                var phoneConflict =
                    await _context.AppUsers
                        .AsNoTracking()
                        .AnyAsync(
                            user =>
                                user.Id !=
                                    (linkedAccount == null
                                        ? 0
                                        : linkedAccount.Id) &&
                                user.NormalizedPhone ==
                                    normalizedPhone,
                            cancellationToken);

                var emailConflict =
                    await _context.AppUsers
                        .AsNoTracking()
                        .AnyAsync(
                            user =>
                                user.Id !=
                                    (linkedAccount == null
                                        ? 0
                                        : linkedAccount.Id) &&
                                user.NormalizedEmail ==
                                    storageEmail,
                            cancellationToken);

                if (phoneConflict)
                {
                    ModelState.AddModelError(
                        nameof(Coach.Phone),
                        "That phone number is already used by another login account.");
                }

                if (emailConflict)
                {
                    ModelState.AddModelError(
                        nameof(Coach.Email),
                        "That email address is already used by another login account.");
                }
            }


            if (!ModelState.IsValid)
            {
                input.AvatarPath =
                    coach.AvatarPath;


                ViewBag.Teams =
                    await GetTeamOptionsAsync(
                        cancellationToken);


                if (IsAjaxRequest())
                {
                    return PartialView(
                        "_EditPartial",
                        input);
                }


                return View(input);
            }


            coach.Name = input.Name.Trim();
            coach.Email = storageEmail;
            coach.Phone = input.Phone.Trim();

            if (linkedAccount == null)
            {
                linkedAccount =
                    CreateCoachAccount(
                        coach,
                        normalizedPhone!);

                _context.AppUsers.Add(
                    linkedAccount);
            }
            else
            {
                linkedAccount.Email =
                    storageEmail;

                linkedAccount.NormalizedEmail =
                    LoginIdentifierHelper.NormalizeEmail(
                        storageEmail);

                linkedAccount.Phone =
                    coach.Phone;

                linkedAccount.NormalizedPhone =
                    normalizedPhone;

                linkedAccount.IsActive = true;
            }

            coach.AssignedTeam =
                string.IsNullOrWhiteSpace(
                    input.AssignedTeam)
                    ? null
                    : input.AssignedTeam.Trim();

            coach.Status =
                input.Status;

            coach.Specialty =
                input.Specialty?.Trim()
                ?? string.Empty;

            coach.Experience =
                input.Experience?.Trim()
                ?? string.Empty;

            coach.Certifications =
                input.Certifications?.Trim()
                ?? string.Empty;


            if (photo != null &&
                photo.Length > 0)
            {
                if (!await UploadSecurity
                    .IsSafeImageAsync(photo))
                {
                    ModelState.AddModelError(
                        "photo",
                        "Photo must be a valid JPG, PNG, " +
                        "or WebP file no larger than 2 MB.");


                    input.AvatarPath =
                        coach.AvatarPath;


                    ViewBag.Teams =
                        await GetTeamOptionsAsync(
                            cancellationToken);


                    return IsAjaxRequest()
                        ? PartialView(
                            "_EditPartial",
                            input)
                        : View(input);
                }


                DeleteCoachPhotoIfExists(
                    coach.AvatarPath);


                var savedPath =
                    await SaveCoachPhotoAsync(photo);


                coach.AvatarPath =
                    savedPath;
            }
            else if (removePhoto)
            {
                DeleteCoachPhotoIfExists(
                    coach.AvatarPath);

                coach.AvatarPath = null;
            }


            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);


                TempData["Success"] =
                    $"Coach \"{coach.Name}\" was updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CoachExistsAsync(
                    coach.Id,
                    cancellationToken))
                {
                    return NotFound();
                }


                throw;
            }


            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true
                });
            }


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // WHATSAPP ACCOUNT SETUP
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAccountSetupWhatsApp(
            int id,
            CancellationToken cancellationToken = default)
        {
            var coach = await _context.Coaches
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

            if (coach == null)
            {
                return NotFound();
            }

            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    coach.Phone);

            if (normalizedPhone == null)
            {
                TempData["Error"] =
                    "This coach does not have a valid WhatsApp/mobile number.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var normalizedEmail =
                LoginIdentifierHelper.NormalizeEmail(
                    coach.Email);

            var account =
                await _context.AppUsers
                    .FirstOrDefaultAsync(
                        user =>
                            user.Role == AppUserRole.Coach &&
                            (user.NormalizedPhone ==
                                normalizedPhone ||
                             user.NormalizedEmail ==
                                normalizedEmail),
                        cancellationToken);

            var phoneConflict =
                await _context.AppUsers
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                (account == null
                                    ? 0
                                    : account.Id) &&
                            user.NormalizedPhone ==
                                normalizedPhone,
                        cancellationToken);

            if (phoneConflict)
            {
                TempData["Error"] =
                    "That phone number is already linked to another login account.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (account == null)
            {
                account =
                    CreateCoachAccount(
                        coach,
                        normalizedPhone);

                _context.AppUsers.Add(account);
            }
            else
            {
                account.Phone = coach.Phone.Trim();
                account.NormalizedPhone =
                    normalizedPhone;
                account.IsActive = true;
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            var token =
                _passwordResetTokens.CreateToken(
                    account,
                    TimeSpan.FromHours(48));

            var setupUrl =
                Url.Action(
                    nameof(AccountController.CreatePlayerPassword),
                    "Account",
                    new { token },
                    Request.Scheme);

            if (string.IsNullOrWhiteSpace(setupUrl))
            {
                TempData["Error"] =
                    "The password setup link could not be generated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var loginNumber =
                LoginIdentifierHelper.DisplayPhone(
                    coach.Phone);

            var message =
                $"Hello {coach.Name}, you have been added to the ParaVolley Mpumalanga Coach Portal.\n\n" +
                $"Please create your own password using this secure link:\n{setupUrl}\n\n" +
                "The link expires in 48 hours. " +
                $"After setup, sign in with {loginNumber} and your password.";

            return Redirect(
                LoginIdentifierHelper.BuildWhatsAppUrl(
                    normalizedPhone,
                    message));
        }


        // ============================================================
        // DELETE
        // ============================================================

        // GET: Coaches/Delete/5
        public async Task<IActionResult> Delete(
            int? id,
            CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return NotFound();
            }


            var coach = await _context.Coaches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    coach => coach.Id == id,
                    cancellationToken);


            if (coach == null)
            {
                return NotFound();
            }


            if (IsAjaxRequest())
            {
                return PartialView(
                    "_DeletePartial",
                    coach);
            }


            return View(coach);
        }


        // POST: Coaches/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id,
            CancellationToken cancellationToken = default)
        {
            var coach = await _context.Coaches
                .FindAsync(
                    new object?[] { id },
                    cancellationToken);


            if (coach != null)
            {
                DeleteCoachPhotoIfExists(
                    coach.AvatarPath);

                var normalizedPhone =
                    LoginIdentifierHelper.NormalizePhone(
                        coach.Phone);

                var normalizedEmail =
                    LoginIdentifierHelper.NormalizeEmail(
                        coach.Email);

                var account =
                    await _context.AppUsers
                        .FirstOrDefaultAsync(
                            user =>
                                user.Role == AppUserRole.Coach &&
                                ((normalizedPhone != null &&
                                  user.NormalizedPhone ==
                                    normalizedPhone) ||
                                 user.NormalizedEmail ==
                                    normalizedEmail),
                            cancellationToken);

                if (account != null)
                {
                    _context.AppUsers.Remove(account);
                }

                _context.Coaches.Remove(coach);

                await _context.SaveChangesAsync(
                    cancellationToken);

                TempData["Success"] =
                    "Coach and Coach Portal account were deleted.";
            }


            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true
                });
            }


            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // TEAM OPTIONS
        // ============================================================

        private async Task<List<string>> GetTeamOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            var playerTeams = await _context.Players
                .AsNoTracking()
                .Where(player =>
                    !string.IsNullOrWhiteSpace(
                        player.Team))
                .Select(player =>
                    player.Team)
                .Distinct()
                .ToListAsync(cancellationToken);


            var coachTeams = await _context.Coaches
                .AsNoTracking()
                .Where(coach =>
                    !string.IsNullOrWhiteSpace(
                        coach.AssignedTeam))
                .Select(coach =>
                    coach.AssignedTeam!)
                .Distinct()
                .ToListAsync(cancellationToken);


            return playerTeams
                .Concat(coachTeams)
                .Where(teamName =>
                    !string.IsNullOrWhiteSpace(
                        teamName))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(teamName =>
                    teamName)
                .ToList();
        }


        // ============================================================
        // HELPERS
        // ============================================================

        private AppUser CreateCoachAccount(
            Coach coach,
            string normalizedPhone)
        {
            var storageEmail =
                LoginIdentifierHelper.BuildStorageEmail(
                    coach.Email,
                    "coach",
                    normalizedPhone);

            var account = new AppUser
            {
                Email = storageEmail,
                NormalizedEmail =
                    LoginIdentifierHelper.NormalizeEmail(
                        storageEmail),
                Phone = coach.Phone.Trim(),
                NormalizedPhone = normalizedPhone,
                Role = AppUserRole.Coach,
                IsActive = true
            };

            var temporarySecret =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(48));

            account.PasswordHash =
                _passwordHasher.HashPassword(
                    account,
                    temporarySecret);

            return account;
        }


        private Task<bool> CoachExistsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.Coaches
                .AnyAsync(
                    coach => coach.Id == id,
                    cancellationToken);
        }


        private async Task<string> SaveCoachPhotoAsync(
    IFormFile photo)
        {
            var ext = Path.GetExtension(photo.FileName)
                .ToLowerInvariant();

            // This folder is inside the existing Railway volume:
            // /app/wwwroot/uploads/reports
            var uploadsFolder = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "reports",
                "coaches");

            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{ext}";

            var fullPath = Path.Combine(
                uploadsFolder,
                fileName);

            await using var stream = new FileStream(
                fullPath,
                FileMode.Create);

            await photo.CopyToAsync(stream);

            return $"/uploads/reports/coaches/{fileName}";
        }


        private void DeleteCoachPhotoIfExists(
    string? avatarPath)
        {
            if (string.IsNullOrWhiteSpace(avatarPath))
            {
                return;
            }

            var fileName = Path.GetFileName(
                avatarPath.Replace('\\', '/'));

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "reports",
                "coaches",
                fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}