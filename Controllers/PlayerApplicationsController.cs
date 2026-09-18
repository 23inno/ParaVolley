using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
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
        private readonly EmailService _emailService;
        private readonly WhatsAppService _whatsAppService;
        private readonly ILogger<PlayerApplicationsController> _logger;

        public PlayerApplicationsController(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            PasswordResetTokenService passwordResetTokens,
            EmailService emailService,
            WhatsAppService whatsAppService,
            ILogger<PlayerApplicationsController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _passwordResetTokens = passwordResetTokens;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
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

            var normalizedEmail =
                NormalizeEmail(application.Email);

            var playerAlreadyExists =
                await _context.Players
                    .AnyAsync(
                        player =>
                            player.Email.ToLower() ==
                            normalizedEmail,
                        cancellationToken);

            var accountAlreadyExists =
                await _context.AppUsers
                    .AnyAsync(
                        user =>
                            user.NormalizedEmail ==
                            normalizedEmail,
                        cancellationToken);

            if (playerAlreadyExists ||
                accountAlreadyExists)
            {
                TempData["ApplicationError"] =
                    "A ParaVolley player or login account already exists with this email address.";

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
                Email = application.Email.Trim(),
                Phone = application.Phone.Trim(),
                Disability =
                    application.Classification.Trim()
            };

            var appUser = new AppUser
            {
                Email = application.Email.Trim(),
                NormalizedEmail = normalizedEmail,
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

            var deliveryWarnings =
                new List<string>();

            if (string.IsNullOrWhiteSpace(setupUrl))
            {
                deliveryWarnings.Add(
                    "The player account was created, but the password setup link could not be generated.");
            }
            else
            {
                var emailResult =
                    await SendApprovalEmailAsync(
                        application,
                        setupUrl);

                if (!emailResult.Success)
                {
                    deliveryWarnings.Add(
                        "Approval email could not be delivered.");
                }

                var whatsappResult =
                    await _whatsAppService
                        .SendPlayerApprovalAsync(
                            application.Phone,
                            application.FullName.Trim(),
                            setupUrl,
                            cancellationToken);

                if (!whatsappResult.Success)
                {
                    deliveryWarnings.Add(
                        whatsappResult.Message);
                }
            }

            TempData["ApplicationSuccess"] =
                $"{application.FullName} has been approved, added as a player, and given a Player App login account.";

            if (deliveryWarnings.Count > 0)
            {
                TempData["ApplicationWarning"] =
                    string.Join(" ", deliveryWarnings);

                _logger.LogWarning(
                    "Player approval {ApplicationId} completed with notification warnings: {Warnings}",
                    application.Id,
                    string.Join(" | ", deliveryWarnings));
            }

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
                    "Only approved applications can receive a new Player App setup link.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var normalizedEmail =
                NormalizeEmail(application.Email);

            var appUser =
                await _context.AppUsers
                    .Include(user => user.Player)
                    .FirstOrDefaultAsync(
                        user =>
                            user.NormalizedEmail ==
                            normalizedEmail &&
                            user.Role ==
                            AppUserRole.Player,
                        cancellationToken);

            if (appUser == null)
            {
                TempData["ApplicationError"] =
                    "The approved player's login account could not be found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

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

            var warnings =
                new List<string>();

            var emailResult =
                await SendApprovalEmailAsync(
                    application,
                    setupUrl);

            if (!emailResult.Success)
            {
                warnings.Add(
                    "Email could not be delivered.");
            }

            if (!string.IsNullOrWhiteSpace(
                    application.Phone))
            {
                var whatsappResult =
                    await _whatsAppService
                        .SendPlayerApprovalAsync(
                            application.Phone,
                            application.FullName.Trim(),
                            setupUrl,
                            cancellationToken);

                if (!whatsappResult.Success)
                {
                    warnings.Add(
                        whatsappResult.Message);
                }
            }

            if (warnings.Count == 0)
            {
                TempData["ApplicationSuccess"] =
                    "A new Player App password setup link was sent by email and WhatsApp.";
            }
            else
            {
                TempData["ApplicationWarning"] =
                    "A new setup link was generated, but: " +
                    string.Join(" ", warnings);
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
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

        private async Task<EmailSendResult>
            SendApprovalEmailAsync(
                PlayerRegistrationApplication application,
                string setupUrl)
        {
            var safeName =
                WebUtility.HtmlEncode(
                    application.FullName.Trim());

            var safeEmail =
                WebUtility.HtmlEncode(
                    application.Email.Trim());

            var safeUrl =
                WebUtility.HtmlEncode(
                    setupUrl);

            var body = $"""
                <div style="font-family:Arial,sans-serif;line-height:1.6;color:#1f2937;">
                    <h2 style="color:#0B6E4F;">Welcome to ParaVolley Mpumalanga</h2>
                    <p>Hello {safeName},</p>
                    <p>
                        Your player registration has been
                        <strong>approved</strong>.
                    </p>
                    <p>
                        Your Player App login email is
                        <strong>{safeEmail}</strong>.
                    </p>
                    <p>
                        Before you can sign in to the Android Player App,
                        create your password using the button below.
                    </p>
                    <p style="margin:28px 0;">
                        <a href="{safeUrl}"
                           style="background:#0B6E4F;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:bold;">
                            Create Player App Password
                        </a>
                    </p>
                    <p>
                        This secure link expires in 48 hours.
                        If it expires, use <strong>Forgot Password</strong>
                        on the Player App login screen.
                    </p>
                    <p>
                        Welcome to ParaVolley Mpumalanga.
                    </p>
                </div>
                """;

            return await _emailService
                .SendDetailedAsync(
                    application.Email.Trim(),
                    "Your ParaVolley player application has been approved",
                    body);
        }

        private static string NormalizeEmail(
            string email)
        {
            return email
                .Trim()
                .ToLowerInvariant();
        }
    }
}
