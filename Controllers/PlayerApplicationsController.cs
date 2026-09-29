using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
    public class PlayerApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly PasswordResetTokenService _passwordResetTokens;
        private readonly ILogger<PlayerApplicationsController> _logger;

        public PlayerApplicationsController(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            PasswordResetTokenService passwordResetTokens,
            ILogger<PlayerApplicationsController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _passwordResetTokens = passwordResetTokens;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var applications =
                await _context.PlayerRegistrationApplications
                    .AsNoTracking()
                    .OrderBy(a => a.Status)
                    .ThenBy(a => a.IsRead)
                    .ThenByDescending(a => a.SubmittedAtUtc)
                    .ToListAsync(cancellationToken);

            return View(applications);
        }

        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var application =
                await _context.PlayerRegistrationApplications
                    .FirstOrDefaultAsync(
                        a => a.Id == id,
                        cancellationToken);

            if (application == null)
            {
                return NotFound();
            }

            if (!application.IsRead)
            {
                application.IsRead = true;

                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Approve(
            int id,
            PlayerApprovalInput input,
            CancellationToken cancellationToken)
        {
            var application =
                await _context.PlayerRegistrationApplications
                    .FirstOrDefaultAsync(
                        a => a.Id == id,
                        cancellationToken);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != PlayerApplicationStatus.Pending)
            {
                TempData["ApplicationError"] =
                    "This application has already been processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                TempData["ApplicationError"] =
                    string.Join(" ", errors);

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!application.DateOfBirth.HasValue)
            {
                TempData["ApplicationError"] =
                    "The application does not contain a date of birth.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (string.IsNullOrWhiteSpace(application.Phone))
            {
                TempData["ApplicationError"] =
                    "The application does not contain a phone number.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (string.IsNullOrWhiteSpace(application.Classification))
            {
                TempData["ApplicationError"] =
                    "The application does not contain a disability classification.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    application.Phone);

            if (normalizedPhone == null)
            {
                TempData["ApplicationError"] =
                    "The application does not contain a valid WhatsApp/mobile number.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var storageEmail =
                LoginIdentifierHelper.BuildStorageEmail(
                    application.Email,
                    "player",
                    normalizedPhone);

            var existingPlayerPhones =
                await _context.Players
                    .AsNoTracking()
                    .Select(player => player.Phone)
                    .ToListAsync(cancellationToken);

            var playerAlreadyExistsByPhone =
                existingPlayerPhones.Any(phone =>
                    LoginIdentifierHelper.NormalizePhone(phone) ==
                    normalizedPhone);

            var accountAlreadyExists =
                await _context.AppUsers
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.NormalizedPhone ==
                                normalizedPhone ||
                            user.NormalizedEmail ==
                                storageEmail,
                        cancellationToken);

            if (playerAlreadyExistsByPhone ||
                accountAlreadyExists)
            {
                TempData["ApplicationError"] =
                    "A ParaVolley player or login account already exists with this phone number or email address.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var today =
                DateOnly.FromDateTime(DateTime.Today);

            var dateOfBirth =
                application.DateOfBirth.Value;

            var age =
                today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
            {
                age--;
            }

            if (age < 5 || age > 100)
            {
                TempData["ApplicationError"] =
                    "The applicant's calculated age is outside the allowed player age range.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var player = new Player
            {
                Name = application.FullName.Trim(),
                Position = input.Position.Trim(),
                Team = input.Team.Trim(),
                Age = age,
                Matches = 0,
                Status = PlayerStatus.Active,
                Email = storageEmail,
                Phone = application.Phone.Trim(),
                Disability =
                    application.Classification.Trim()
            };

            var appUser = new AppUser
            {
                Email = storageEmail,
                NormalizedEmail =
                    LoginIdentifierHelper.NormalizeEmail(
                        storageEmail),
                Phone = application.Phone.Trim(),
                NormalizedPhone = normalizedPhone,
                Role = AppUserRole.Player,
                IsActive = true,
                Player = player
            };

            var temporarySecret =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(48));

            appUser.PasswordHash =
                _passwordHasher.HashPassword(
                    appUser,
                    temporarySecret);

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                _context.Players.Add(player);
                _context.AppUsers.Add(appUser);

                application.Status =
                    PlayerApplicationStatus.Approved;

                application.IsRead = true;

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

            TempData["ApplicationSuccess"] =
                $"{application.FullName} has been approved, added as a player, and given a Player App login account. Open the WhatsApp setup message below so the player can create their own password.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> ResendApprovalAccess(
            int id,
            CancellationToken cancellationToken)
        {
            var application =
                await _context.PlayerRegistrationApplications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.Id == id,
                        cancellationToken);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status !=
                PlayerApplicationStatus.Approved)
            {
                TempData["ApplicationError"] =
                    "Only approved applications can receive a Player App setup link.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    application.Phone);

            if (normalizedPhone == null)
            {
                TempData["ApplicationError"] =
                    "This approved player does not have a valid WhatsApp/mobile number.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var appUser =
                await _context.AppUsers
                    .Include(user => user.Player)
                    .FirstOrDefaultAsync(
                        user =>
                            user.Role == AppUserRole.Player &&
                            user.NormalizedPhone ==
                                normalizedPhone,
                        cancellationToken);

            if (appUser == null)
            {
                TempData["ApplicationError"] =
                    "The approved player's login account could not be found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            appUser.IsActive = true;
            await _context.SaveChangesAsync(
                cancellationToken);

            var setupToken =
                _passwordResetTokens.CreateToken(
                    appUser,
                    TimeSpan.FromHours(48));

            var setupUrl =
                Url.Action(
                    nameof(AccountController.CreatePlayerPassword),
                    "Account",
                    new { token = setupToken },
                    Request.Scheme);

            if (string.IsNullOrWhiteSpace(setupUrl))
            {
                TempData["ApplicationError"] =
                    "A new Player App setup link could not be generated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var loginNumber =
                LoginIdentifierHelper.DisplayPhone(
                    application.Phone);

            var message =
                $"Hello {application.FullName.Trim()}, your ParaVolley Mpumalanga player application has been approved.\n\n" +
                $"Create your own Player App password using this secure link:\n{setupUrl}\n\n" +
                "The link expires in 48 hours. " +
                $"After setup, sign in with {loginNumber} and your password.";

            return Redirect(
                LoginIdentifierHelper.BuildWhatsAppUrl(
                    normalizedPhone,
                    message));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Decline(
            int id,
            CancellationToken cancellationToken)
        {
            var application =
                await _context.PlayerRegistrationApplications
                    .FirstOrDefaultAsync(
                        a => a.Id == id,
                        cancellationToken);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status !=
                PlayerApplicationStatus.Pending)
            {
                TempData["ApplicationError"] =
                    "This application has already been processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            application.Status =
                PlayerApplicationStatus.Declined;

            application.IsRead = true;

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["ApplicationSuccess"] =
                $"{application.FullName}'s application has been declined.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

    }
}
