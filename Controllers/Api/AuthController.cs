using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
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
            var normalizedEmail = request.Email
                .Trim()
                .ToLowerInvariant();

            var accountExists = await _context.AppUsers
                .AnyAsync(
                    user => user.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (accountExists)
            {
                return Conflict(new
                {
                    message =
                        "An account already exists with this email address."
                });
            }

            var playerExists = await _context.Players
                .AnyAsync(
                    player => player.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (playerExists)
            {
                return Conflict(new
                {
                    message =
                        "A player already exists with this email address."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var player = new Player
                {
                    Name = request.Name.Trim(),
                    Position = request.Position.Trim(),
                    Team = request.Team.Trim(),
                    Status = PlayerStatus.Inactive,
                    Age = request.Age,
                    Matches = 0,
                    Email = normalizedEmail,
                    Phone = request.Phone.Trim(),
                    Disability = request.Disability.Trim()
                };

                _context.Players.Add(player);

                await _context.SaveChangesAsync(cancellationToken);

                _context.PlayerProfileDetails.Add(
                    new PlayerProfileDetails
                    {
                        PlayerId = player.Id,
                        JoinedDate = DateTime.UtcNow.Date
                    });

                var appUser = new AppUser
                {
                    Email = normalizedEmail,
                    NormalizedEmail = normalizedEmail,
                    Role = AppUserRole.Player,
                    IsActive = false,
                    PlayerId = player.Id
                };

                appUser.PasswordHash =
                    _passwordHasher.HashPassword(
                        appUser,
                        request.Password);

                _context.AppUsers.Add(appUser);

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                await NotifyNewPlayerRegistrationAsync(
                    player,
                    cancellationToken);

                return StatusCode(
                    StatusCodes.Status201Created,
                    new
                    {
                        message =
                            "Registration submitted successfully. An administrator must approve the account before login.",
                        playerId = player.Id
                    });
            }
            catch (DbUpdateException exception)
                when (DatabaseConflictClassifier.IsUniqueViolation(
                    exception,
                    _context.Database,
                    "IX_AppUsers_NormalizedEmail"))
            {
                await transaction.RollbackAsync(cancellationToken);

                return Conflict(new
                {
                    message =
                        "An account already exists with this email address."
                });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task NotifyNewPlayerRegistrationAsync(
            Player player,
            CancellationToken cancellationToken)
        {
            try
            {
                var subject = $"New player registration: {player.Name}";
                var textBody =
                    $"A new Android player registration was submitted by {player.Name}. " +
                    $"Email: {player.Email}. Phone: {player.Phone}. Team: {player.Team}. " +
                    "The account is inactive until an administrator approves it.";

                var safeName = System.Net.WebUtility.HtmlEncode(player.Name);
                var safeEmail = System.Net.WebUtility.HtmlEncode(player.Email);
                var safePhone = System.Net.WebUtility.HtmlEncode(player.Phone);
                var safeTeam = System.Net.WebUtility.HtmlEncode(player.Team);

                var htmlBody = $"""
                    <p>A new ParaVolley Mpumalanga Android player registration has been submitted.</p>
                    <p><strong>Name:</strong> {safeName}</p>
                    <p><strong>Email:</strong> {safeEmail}</p>
                    <p><strong>Phone:</strong> {safePhone}</p>
                    <p><strong>Team:</strong> {safeTeam}</p>
                    <p>The account is inactive until an administrator approves it.</p>
                    """;

                var results = await _staffNotifications.SendAsync(
                    "new_player",
                    subject,
                    textBody,
                    htmlBody,
                    includeCoaches: false,
                    cancellationToken);

                foreach (var result in results.Where(item => !item.Success))
                {
                    _logger.LogWarning(
                        "New Android player registration {PlayerId} notification reported: {Message}",
                        player.Id,
                        result.Message);
                }
            }
            catch (Exception exception)
            {
                // Registration has already committed; notification failure must
                // not turn a successful registration into an API error.
                _logger.LogWarning(
                    exception,
                    "Player registration {PlayerId} was saved, but staff notification delivery failed.",
                    player.Id);
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
