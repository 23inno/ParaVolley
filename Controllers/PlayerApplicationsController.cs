using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class PlayerApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayerApplicationsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /PlayerApplications
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

        // GET: /PlayerApplications/Details/5
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

            var playerAlreadyExists =
                await _context.Players
                    .AnyAsync(
                        p => p.Email.ToLower() ==
                             application.Email.ToLower(),
                        cancellationToken);

            if (playerAlreadyExists)
            {
                TempData["ApplicationError"] =
                    "A player with this email address already exists.";

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

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dateOfBirth = application.DateOfBirth.Value;

            var age = today.Year - dateOfBirth.Year;

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

                Disability = application.Classification.Trim()
            };

            _context.Players.Add(player);

            application.Status =
                PlayerApplicationStatus.Approved;

            application.IsRead = true;

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["ApplicationSuccess"] =
                $"{application.FullName} has been approved and added as a player.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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