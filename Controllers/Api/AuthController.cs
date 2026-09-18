using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Models;
using SportsManagementMVC.Models.Api;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly StaffNotificationService _staffNotifications;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration,
            StaffNotificationService staffNotifications,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _staffNotifications = staffNotifications;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            LoginRequest request)
        {
            var normalizedEmail = request.Email
                .Trim()
                .ToLowerInvariant();

            var user = await _context.AppUsers
                .Include(x => x.Player)
                .FirstOrDefaultAsync(x =>
                    x.NormalizedEmail == normalizedEmail);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            if (passwordResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    request.Password);

                await _context.SaveChangesAsync();
            }

            var accessTokenMinutes =
                _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
            var expiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);
            var token = CreateToken(user, expiresAt);

            return Ok(new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = new AppUserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    PlayerId = user.PlayerId,
                    PlayerName = user.Player?.Name
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("register/player")]
        [EnableRateLimiting("registration")]
        public async Task<IActionResult> RegisterPlayer(
            RegisterPlayerRequest request,
            CancellationToken cancellationToken = default)
        {
            var settings = await _context.OrganisationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (settings?.AcceptPlayerApplications == false)
            {
                return Conflict(new
                {
                    message = "Player applications are currently closed."
                });
            }

            if (!request.Consent)
            {
                return BadRequest(new
                {
                    message =
                        "You must confirm that the information is correct and agree to be contacted."
                });
            }

            if (!request.DateOfBirth.HasValue)
            {
                return BadRequest(new
                {
                    message = "Date of birth is required."
                });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dateOfBirth = request.DateOfBirth.Value;

            if (dateOfBirth > today)
            {
                return BadRequest(new
                {
                    message = "Date of birth cannot be in the future."
                });
            }

            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age))
            {
                age--;
            }

            if (age < 5 || age > 100)
            {
                return BadRequest(new
                {
                    message = "Player age must be between 5 and 100 years."
                });
            }

            var normalizedEmail = request.Email
                .Trim()
                .ToLowerInvariant();

            var accountExists = await _context.AppUsers
                .AsNoTracking()
                .AnyAsync(
                    user => user.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            var playerExists = await _context.Players
                .AsNoTracking()
                .AnyAsync(
                    player => player.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (accountExists || playerExists)
            {
                return Conflict(new
                {
                    message =
                        "A registered ParaVolley account already exists with this email address."
                });
            }

            var alreadyPending = await _context.PlayerRegistrationApplications
                .AsNoTracking()
                .AnyAsync(
                    application =>
                        application.Email.ToLower() == normalizedEmail &&
                        application.Status == PlayerApplicationStatus.Pending,
                    cancellationToken);

            if (alreadyPending)
            {
                return Conflict(new
                {
                    message =
                        "A pending player application already exists for this email address."
                });
            }

            var application = new PlayerRegistrationApplication
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone.Trim(),
                DateOfBirth = request.DateOfBirth,
                Province = request.Province?.Trim(),
                Town = request.Town?.Trim(),
                ExperienceLevel = request.ExperienceLevel?.Trim(),
                PreferredPosition = request.PreferredPosition?.Trim(),
                Classification = request.Classification.Trim(),
                EmergencyContactName = request.EmergencyContactName?.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
                MedicalNotes = request.MedicalNotes?.Trim(),
                Consent = true,
                Status = PlayerApplicationStatus.Pending,
                IsRead = false,
                SubmittedAtUtc = DateTime.UtcNow
            };

            _context.PlayerRegistrationApplications.Add(application);
            await _context.SaveChangesAsync(cancellationToken);

            await NotifyNewPlayerApplicationAsync(
                application,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message =
                        "Player application submitted successfully. ParaVolley Mpumalanga will contact you after it has been reviewed.",
                    applicationId = application.Id
                });
        }

        private async Task NotifyNewPlayerApplicationAsync(
            PlayerRegistrationApplication application,
            CancellationToken cancellationToken)
        {
            try
            {
                var subject = $"New player application: {application.FullName}";
                var textBody =
                    $"A new Android player application was submitted by {application.FullName}. " +
                    $"Email: {application.Email}. Phone: {application.Phone}. " +
                    $"Province: {application.Province ?? "Not provided"}. " +
                    $"Preferred position: {application.PreferredPosition ?? "Not provided"}.";

                var safeName =
                    System.Net.WebUtility.HtmlEncode(application.FullName);
                var safeEmail =
                    System.Net.WebUtility.HtmlEncode(application.Email);
                var safePhone =
                    System.Net.WebUtility.HtmlEncode(application.Phone);
                var safeProvince =
                    System.Net.WebUtility.HtmlEncode(
                        application.Province ?? "Not provided");
                var safePosition =
                    System.Net.WebUtility.HtmlEncode(
                        application.PreferredPosition ?? "Not provided");

                var htmlBody = $"""
                    <p>A new ParaVolley Mpumalanga player application was submitted from the Android app.</p>
                    <p><strong>Name:</strong> {safeName}</p>
                    <p><strong>Email:</strong> {safeEmail}</p>
                    <p><strong>Phone:</strong> {safePhone}</p>
                    <p><strong>Province:</strong> {safeProvince}</p>
                    <p><strong>Preferred position:</strong> {safePosition}</p>
                    <p>Open Player Applications in the ParaVolley web portal to review it.</p>
                    """;

                var results = await _staffNotifications.SendAsync(
                    "new_player",
                    subject,
                    textBody,
                    htmlBody,
                    includeCoaches: true,
                    cancellationToken);

                foreach (var result in results.Where(item => !item.Success))
                {
                    _logger.LogWarning(
                        "New player application {ApplicationId} notification reported: {Message}",
                        application.Id,
                        result.Message);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Player application {ApplicationId} was saved, but staff notification delivery failed.",
                    application.Id);
            }
        }

        private string CreateToken(
            AppUser user,
            DateTime expiresAt)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT signing key was not found.");

            var jwtIssuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT issuer was not found.");

            var jwtAudience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT audience was not found.");

            var claims = new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),
                new(
                    JwtRegisteredClaimNames.Email,
                    user.Email),
                new(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),
                new(
                    ClaimTypes.Email,
                    user.Email),
                new(
                    ClaimTypes.Role,
                    user.Role.ToString())
            };

            if (user.PlayerId.HasValue)
            {
                claims.Add(new Claim(
                    "playerId",
                    user.PlayerId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(jwtToken);
        }
    }
}
