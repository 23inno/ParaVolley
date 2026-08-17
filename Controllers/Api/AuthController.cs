using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Models;
using SportsManagementMVC.Models.Api;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            LoginRequest request)
        {
            var normalizedEmail = request.Email
                .Trim()
                .ToLowerInvariant();

            var user = await _context.AppUsers
                .Include(x => x.Player)
                .FirstOrDefaultAsync(x =>
                    x.Email.ToLower() == normalizedEmail);

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

            var expiresAt = DateTime.UtcNow.AddHours(8);
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
        public async Task<IActionResult> RegisterPlayer(
            RegisterPlayerRequest request)
        {
            var normalizedEmail = request.Email
                .Trim()
                .ToLowerInvariant();

            var accountExists = await _context.AppUsers
                .AnyAsync(user =>
                    user.Email.ToLower() == normalizedEmail);

            if (accountExists)
            {
                return Conflict(new
                {
                    message =
                        "An account already exists with this email address."
                });
            }

            var playerExists = await _context.Players
                .AnyAsync(player =>
                    player.Email.ToLower() == normalizedEmail);

            if (playerExists)
            {
                return Conflict(new
                {
                    message =
                        "A player already exists with this email address."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

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

                await _context.SaveChangesAsync();

                var appUser = new AppUser
                {
                    Email = normalizedEmail,
                    Role = AppUserRole.Player,
                    IsActive = false,
                    PlayerId = player.Id
                };

                appUser.PasswordHash =
                    _passwordHasher.HashPassword(
                        appUser,
                        request.Password);

                _context.AppUsers.Add(appUser);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return StatusCode(
                    StatusCodes.Status201Created,
                    new
                    {
                        message =
                            "Registration submitted successfully. An administrator must approve the account before login.",
                        playerId = player.Id
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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