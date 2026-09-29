using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            EmailService emailService,
            IServiceScopeFactory scopeFactory,
            ILogger<HomeController> logger)
        {
            _context = context;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
            _logger = logger;
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
        public async Task<IActionResult> Join(
            CancellationToken cancellationToken)
        {
            var settings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (settings?.AcceptPlayerApplications == false)
            {
                return View(
                    "~/Views/Registration/Closed.cshtml",
                    settings);
            }

            var application = new PlayerRegistrationApplication
            {
                Province = string.IsNullOrWhiteSpace(settings?.DefaultProvince)
                    ? "Mpumalanga"
                    : settings.DefaultProvince
            };

            return View(
                "~/Views/Registration/Index.cshtml",
                application);
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
            var organisationSettings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (organisationSettings?.AcceptPlayerApplications == false)
            {
                return View(
                    "~/Views/Registration/Closed.cshtml",
                    organisationSettings);
            }

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
            else
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var dateOfBirth = input.DateOfBirth.Value;

                if (dateOfBirth > today)
                {
                    ModelState.AddModelError(
                        nameof(input.DateOfBirth),
                        "Date of birth cannot be in the future.");
                }
                else
                {
                    var age = today.Year - dateOfBirth.Year;

                    if (dateOfBirth > today.AddYears(-age))
                    {
                        age--;
                    }

                    if (age < 5 || age > 100)
                    {
                        ModelState.AddModelError(
                            nameof(input.DateOfBirth),
                            "Player age must be between 5 and 100 years.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(input.Classification))
            {
                ModelState.AddModelError(
                    nameof(input.Classification),
                    "Disability classification is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            var normalizedPhone =
                LoginIdentifierHelper.NormalizePhone(
                    input.Phone);

            if (normalizedPhone == null)
            {
                ModelState.AddModelError(
                    nameof(input.Phone),
                    "Enter a valid WhatsApp/mobile number.");

                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            var email =
                LoginIdentifierHelper.NormalizeEmail(
                    input.Email);

            var existingPlayerPhones =
                await _context.Players
                    .AsNoTracking()
                    .Select(player => player.Phone)
                    .ToListAsync(cancellationToken);

            var playerAlreadyExistsByPhone =
                existingPlayerPhones.Any(phone =>
                    LoginIdentifierHelper.NormalizePhone(phone) ==
                    normalizedPhone);

            var playerAlreadyExistsByEmail =
                !string.IsNullOrWhiteSpace(email) &&
                await _context.Players
                    .AsNoTracking()
                    .AnyAsync(
                        player =>
                            player.Email.ToLower() == email,
                        cancellationToken);

            if (playerAlreadyExistsByPhone ||
                playerAlreadyExistsByEmail)
            {
                ModelState.AddModelError(
                    playerAlreadyExistsByPhone
                        ? nameof(input.Phone)
                        : nameof(input.Email),
                    playerAlreadyExistsByPhone
                        ? "A registered ParaVolley player already exists with this phone number."
                        : "A registered ParaVolley player already exists with this email address.");

                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            var pendingApplications =
                await _context.PlayerRegistrationApplications
                    .AsNoTracking()
                    .Where(application =>
                        application.Status ==
                        PlayerApplicationStatus.Pending)
                    .Select(application => new
                    {
                        application.Email,
                        application.Phone
                    })
                    .ToListAsync(cancellationToken);

            var alreadyPendingByPhone =
                pendingApplications.Any(application =>
                    LoginIdentifierHelper.NormalizePhone(
                        application.Phone) ==
                    normalizedPhone);

            var alreadyPendingByEmail =
                !string.IsNullOrWhiteSpace(email) &&
                pendingApplications.Any(application =>
                    string.Equals(
                        LoginIdentifierHelper.NormalizeEmail(
                            application.Email),
                        email,
                        StringComparison.Ordinal));

            if (alreadyPendingByPhone ||
                alreadyPendingByEmail)
            {
                ModelState.AddModelError(
                    alreadyPendingByPhone
                        ? nameof(input.Phone)
                        : nameof(input.Email),
                    alreadyPendingByPhone
                        ? "A pending player application already exists for this phone number."
                        : "A pending player application already exists for this email address.");

                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            input.Email = email;
            input.FullName = input.FullName.Trim();
            input.Phone = input.Phone?.Trim();
            input.Classification = input.Classification?.Trim();

            input.SubmittedAtUtc = DateTime.UtcNow;
            input.Status = PlayerApplicationStatus.Pending;
            input.IsRead = false;

            _context.PlayerRegistrationApplications.Add(input);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (!string.IsNullOrWhiteSpace(input.Email) &&
                    DatabaseConflictClassifier.IsUniqueViolation(
                        exception,
                        _context.Database,
                        "IX_PlayerRegistrationApplications_PendingEmail"))
            {
                ModelState.AddModelError(
                    nameof(input.Email),
                    "A pending player application already exists for this email address.");

                return View(
                    "~/Views/Registration/Index.cshtml",
                    input);
            }

            QueueNewPlayerApplicationNotification(
                input);

            TempData["RegistrationSuccess"] =
                "Your player application has been submitted successfully and is pending review.";

            TempData["RegistrationPhone"] =
                LoginIdentifierHelper.DisplayPhone(
                    input.Phone);

            return RedirectToAction(
                nameof(JoinSubmitted));
        }

        [AllowAnonymous]
        [HttpGet("/Join/Submitted")]
        public IActionResult JoinSubmitted()
        {
            return View(
                "~/Views/Registration/Submitted.cshtml");
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


            var contactSettings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            var contactDestination =
                string.IsNullOrWhiteSpace(contactSettings?.OfficialEmail)
                    ? "paravolleympumalanga@gmail.com"
                    : contactSettings.OfficialEmail;

            input.EmailSent = await _emailService.SendAsync(
                contactDestination,
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

            var organisationSettings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            var activeSeason =
                int.TryParse(organisationSettings?.ActiveSeason, out var configuredSeason)
                    ? configuredSeason
                    : today.Year;

            var seasonStart = new DateTime(activeSeason, 1, 1);
            var seasonEnd = seasonStart.AddYears(1);
            ViewBag.ActiveSeason = activeSeason;

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
                             e.Date >= today &&
                             e.Date >= seasonStart &&
                             e.Date < seasonEnd,
                        cancellationToken),

                UpcomingMatches = await _context.Matches
                    .CountAsync(
                        m => m.Status == MatchStatus.Scheduled &&
                             m.Date >= today &&
                             m.Date >= seasonStart &&
                             m.Date < seasonEnd,
                        cancellationToken),

                TotalAnnouncements = await _context.Announcements
                    .CountAsync(cancellationToken),

                NextEvents = await _context.Events
                    .AsNoTracking()
                    .Where(
                        e => e.Status == EventStatus.Upcoming &&
                             e.Date >= today &&
                             e.Date >= seasonStart &&
                             e.Date < seasonEnd)
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.Time)
                    .Take(4)
                    .ToListAsync(cancellationToken),

                NextMatches = await _context.Matches
                    .AsNoTracking()
                    .Where(
                        m => m.Status == MatchStatus.Scheduled &&
                             m.Date >= today &&
                             m.Date >= seasonStart &&
                             m.Date < seasonEnd)
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
        public async Task<IActionResult> CoachDashboard(
            CancellationToken cancellationToken)
        {
            var today = DateTime.Today;

            var organisationSettings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            var activeSeason =
                int.TryParse(organisationSettings?.ActiveSeason, out var configuredSeason)
                    ? configuredSeason
                    : today.Year;

            var seasonStart = new DateTime(activeSeason, 1, 1);
            var seasonEnd = seasonStart.AddYears(1);
            ViewBag.ActiveSeason = activeSeason;

            var attendanceCounts = await _context.Attendances
                .AsNoTracking()
                .Where(item =>
                    item.Date >= seasonStart &&
                    item.Date < seasonEnd)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Present = group.Count(item =>
                        item.Status == AttendanceStatus.Present)
                })
                .SingleOrDefaultAsync(cancellationToken);

            var vm = new CoachDashboardViewModel
            {
                ActivePlayers = await _context.Players
                    .CountAsync(
                        p => p.Status == PlayerStatus.Active,
                        cancellationToken),

                UpcomingTraining = await _context.Events
                    .AsNoTracking()
                    .Where(e =>
                        e.Type == EventType.Practice &&
                        e.Status == EventStatus.Upcoming &&
                        e.Date >= today &&
                        e.Date >= seasonStart &&
                        e.Date < seasonEnd)
                    .OrderBy(e => e.Date)
                    .Take(6)
                    .ToListAsync(cancellationToken),

                UpcomingMatches = await _context.Matches
                    .AsNoTracking()
                    .Where(m =>
                        m.Status == MatchStatus.Scheduled &&
                        m.Date >= today &&
                        m.Date >= seasonStart &&
                        m.Date < seasonEnd)
                    .OrderBy(m => m.Date)
                    .Take(6)
                    .ToListAsync(cancellationToken),

                RecentAnnouncements = await _context.Announcements
                    .AsNoTracking()
                    .OrderByDescending(a => a.IsPinned)
                    .ThenByDescending(a => a.Date)
                    .Take(5)
                    .ToListAsync(cancellationToken),

                AttendanceRecords = attendanceCounts?.Total ?? 0,
                PresentRecords = attendanceCounts?.Present ?? 0
            };

            return View(vm);
        }

        [AllowAnonymous]
        [HttpGet("/Maintenance")]
        public async Task<IActionResult> Maintenance(
            CancellationToken cancellationToken)
        {
            var settings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? new OrganisationSettings();

            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(settings);
        }

        private void QueueNewPlayerApplicationNotification(
            PlayerRegistrationApplication application)
        {
            var subject =
                $"New player application: {application.FullName}";

            var location = string.Join(
                ", ",
                new[]
                {
                    application.Town,
                    application.Province
                }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            var displayEmail =
                string.IsNullOrWhiteSpace(application.Email)
                    ? "Not provided"
                    : application.Email;

            var textBody =
                $"A new ParaVolley player application was received from {application.FullName}. " +
                $"Email: {displayEmail}. Phone: {application.Phone}. " +
                $"Location: {(string.IsNullOrWhiteSpace(location) ? "Not provided" : location)}. " +
                "Open Player Applications in the Admin website to review it.";

            var safeName =
                WebUtility.HtmlEncode(
                    application.FullName);

            var safeEmail =
                WebUtility.HtmlEncode(
                    displayEmail);

            var safePhone =
                WebUtility.HtmlEncode(
                    application.Phone ??
                    "Not provided");

            var safeLocation =
                WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(location)
                        ? "Not provided"
                        : location);

            var htmlBody = $"""
                <p>A new ParaVolley Mpumalanga player application has been received.</p>
                <p><strong>Name:</strong> {safeName}</p>
                <p><strong>Email:</strong> {safeEmail}</p>
                <p><strong>Phone:</strong> {safePhone}</p>
                <p><strong>Location:</strong> {safeLocation}</p>
                <p>Open <strong>Player Applications</strong> in the Admin website to review the application.</p>
                """;

            BackgroundNotificationDispatcher
                .QueueStaffNotification(
                    _scopeFactory,
                    _logger,
                    "new_player",
                    subject,
                    textBody,
                    htmlBody,
                    includeCoaches: false,
                    context:
                        $"player-application-{application.Id}");
        }

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
