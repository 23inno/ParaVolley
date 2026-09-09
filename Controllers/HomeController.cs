using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public HomeController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            ViewBag.UpcomingEvents = await _context.Events.AsNoTracking()
                .Where(e => e.Status == EventStatus.Upcoming && e.Date >= today)
                .OrderBy(e => e.Date).Take(3).ToListAsync(cancellationToken);
            ViewBag.RecentAnnouncements = await _context.Announcements.AsNoTracking()
                .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.Date)
                .Take(3).ToListAsync(cancellationToken);
            ViewBag.Sponsors = await _context.Sponsors.AsNoTracking()
                .OrderBy(s => s.Tier).ThenBy(s => s.Name)
                .Take(50).ToListAsync(cancellationToken);
            return View("PublicHome");
        }

        [AllowAnonymous]
        [HttpGet("/About")]
        public IActionResult About() => View();

        [AllowAnonymous]
        [HttpGet("/Join")]
        public IActionResult Join()
        {
            return View(
                "~/Views/Registration/Index.cshtml",
                new PlayerRegistrationApplication());
        }

        [AllowAnonymous]
        [HttpPost("/Join")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("registration")]
        public async Task<IActionResult> Join(
    [Bind(
        "FullName,Email,Phone,DateOfBirth,Province,Town," +
        "ExperienceLevel,PreferredPosition,Classification," +
        "EmergencyContactName,EmergencyContactPhone,MedicalNotes,Consent")]
    PlayerRegistrationApplication input,
    CancellationToken cancellationToken)


        {
            if (!input.Consent)
            {
                ModelState.AddModelError(
                    nameof(input.Consent),
                    "You must agree before submitting.");
            }

            if (string.IsNullOrWhiteSpace(input.Phone))
            {
                ModelState.AddModelError(
                    nameof(input.Phone),
                    "Phone number is required.");
            }

            if (!input.DateOfBirth.HasValue)
            {
                ModelState.AddModelError(
                    nameof(input.DateOfBirth),
                    "Date of birth is required.");
            }

            if (string.IsNullOrWhiteSpace(input.Classification))
            {
                ModelState.AddModelError(
                    nameof(input.Classification),
                    "Disability classification is required.");
            }

            if (!input.Consent)
            {
                ModelState.AddModelError(
                    nameof(input.Consent),
                    "You must agree before submitting.");
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            var email = input.Email.Trim().ToLowerInvariant();

            var alreadyPending = await _context.PlayerRegistrationApplications
                .AsNoTracking()
                .AnyAsync(
                    application =>
                        application.Email.ToLower() == email &&
                        application.Status == PlayerApplicationStatus.Pending,
                    cancellationToken);

            if (alreadyPending)
            {
                ModelState.AddModelError(
                    nameof(input.Email),
                    "A pending player application already exists for this email address.");

                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            input.Email = input.Email.Trim();
            input.FullName = input.FullName.Trim();
            input.SubmittedAtUtc = DateTime.UtcNow;
            input.Status = PlayerApplicationStatus.Pending;
            input.IsRead = false;

            _context.PlayerRegistrationApplications.Add(input);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["RegistrationSuccess"] =
                "Thank you. Your player application has been submitted successfully. " +
                "ParaVolley Mpumalanga will contact you after reviewing it.";

            return Redirect("/Join#player-registration-form");
        }

        [AllowAnonymous]
        [HttpGet("/Contact")]
        public IActionResult Contact()
        {
            return View(new ContactMessage());
        }


        [AllowAnonymous]
        [HttpPost("/Contact")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Contact(
            [Bind("Name,Email,Phone,Subject,Message")]
    ContactMessage input,
            CancellationToken cancellationToken)
        {
            var allowedSubjects = new[]
            {
        "General Enquiry",
        "Player Registration",
        "Volunteering",
        "Events",
        "Partnership",
        "Other"
    };

            if (!allowedSubjects.Contains(input.Subject))
            {
                ModelState.AddModelError(
                    nameof(input.Subject),
                    "Please select a valid enquiry type.");
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            input.SubmittedAtUtc = DateTime.UtcNow;

            _context.ContactMessages.Add(input);

            await _context.SaveChangesAsync(cancellationToken);


            var safeName = WebUtility.HtmlEncode(input.Name);
            var safeEmail = WebUtility.HtmlEncode(input.Email);
            var safePhone = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(input.Phone)
                    ? "Not provided"
                    : input.Phone);

            var safeSubject = input.Subject
                .Replace("\r", " ")
                .Replace("\n", " ");

            var safeMessage = WebUtility
                .HtmlEncode(input.Message)
                .Replace("\r\n", "<br />")
                .Replace("\n", "<br />");


            var emailBody = $"""
        <h2>New ParaVolley Website Enquiry</h2>

        <p><strong>Name:</strong> {safeName}</p>

        <p><strong>Email:</strong> {safeEmail}</p>

        <p><strong>Phone:</strong> {safePhone}</p>

        <p><strong>Enquiry Type:</strong>
        {WebUtility.HtmlEncode(input.Subject)}</p>

        <hr />

        <p><strong>Message:</strong></p>

        <p>{safeMessage}</p>
        """;


            input.EmailSent = await _emailService.SendAsync(
                "paravolleympumalanga@gmail.com",
                $"Website Enquiry: {safeSubject}",
                emailBody);


            await _context.SaveChangesAsync(cancellationToken);


            TempData["ContactSuccess"] =
                "Thank you. Your message has been sent to ParaVolley Mpumalanga.";

            return Redirect("/Contact#send-message");
        }

        [AllowAnonymous]
        [HttpGet("/Athletes")]
        public async Task<IActionResult> Athletes(
    CancellationToken cancellationToken)
        {
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.Status == PlayerStatus.Active)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return View("~/Views/Players/Athletes.cshtml", players);
        }

        [AllowAnonymous]
        [HttpGet("/Results")]
        public IActionResult Results() => RedirectToAction("Index", "Matches");

        [AllowAnonymous]
        [HttpGet("/News")]
        public IActionResult News() => RedirectToAction("Index", "Announcements");

        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> AdminDashboard(
    CancellationToken cancellationToken)
        {
            var today = DateTime.Today;

            var playerCounts = await _context.Players
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Active = group.Count(
                        player => player.Status == PlayerStatus.Active),
                    Inactive = group.Count(
                        player => player.Status == PlayerStatus.Inactive)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var totalPlayers = playerCounts?.Total ?? 0;
            var activePlayers = playerCounts?.Active ?? 0;
            var inactivePlayers = playerCounts?.Inactive ?? 0;

            var vm = new DashboardViewModel
            {
                TotalPlayers = totalPlayers,
                ActivePlayers = activePlayers,

                TotalCoaches = await _context.Coaches
                    .CountAsync(cancellationToken),

                UpcomingEvents = await _context.Events
                    .CountAsync(
                        e => e.Status == EventStatus.Upcoming &&
                             e.Date >= today,
                        cancellationToken),

                UpcomingMatches = await _context.Matches
                    .CountAsync(
                        m => m.Status == MatchStatus.Scheduled &&
                             m.Date >= today,
                        cancellationToken),

                TotalAnnouncements = await _context.Announcements
                    .CountAsync(cancellationToken),

                NextEvents = await _context.Events
                    .AsNoTracking()
                    .Where(
                        e => e.Status == EventStatus.Upcoming &&
                             e.Date >= today)
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.Time)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                NextMatches = await _context.Matches
                    .AsNoTracking()
                    .Where(
                        m => m.Status == MatchStatus.Scheduled &&
                             m.Date >= today)
                    .OrderBy(m => m.Date)
                    .ThenBy(m => m.Time)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                RecentAnnouncements = await _context.Announcements
                    .AsNoTracking()
                    .OrderByDescending(a => a.IsPinned)
                    .ThenByDescending(a => a.Date)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                PlayerStatsChart = new List<MonthlyPlayerStat>
        {
            new()
            {
                Month = "Current",
                Active = activePlayers,
                New = 0,
                Inactive = inactivePlayers
            }
        }
            };

            return View("Index", vm);
        }

        [Authorize(Policy = AuthorizationPolicies.CoachOnly)]
        public async Task<IActionResult> CoachDashboard(CancellationToken cancellationToken)
        {
            var today = DateTime.Today;
            var attendanceCounts = await _context.Attendances.AsNoTracking().GroupBy(_ => 1)
                .Select(group => new { Total = group.Count(), Present = group.Count(item => item.Status == AttendanceStatus.Present) })
                .SingleOrDefaultAsync(cancellationToken);

            var vm = new CoachDashboardViewModel
            {
                ActivePlayers = await _context.Players.CountAsync(p => p.Status == PlayerStatus.Active, cancellationToken),
                UpcomingTraining = await _context.Events.AsNoTracking().Where(e => e.Type == EventType.Practice && e.Status == EventStatus.Upcoming && e.Date >= today).OrderBy(e => e.Date).Take(6).ToListAsync(cancellationToken),
                UpcomingMatches = await _context.Matches.AsNoTracking().Where(m => m.Status == MatchStatus.Scheduled).OrderBy(m => m.Date).Take(6).ToListAsync(cancellationToken),
                RecentAnnouncements = await _context.Announcements.AsNoTracking().OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.Date).Take(5).ToListAsync(cancellationToken),
                AttendanceRecords = attendanceCounts?.Total ?? 0,
                PresentRecords = attendanceCounts?.Present ?? 0
            };
            return View(vm);
        }

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
