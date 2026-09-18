using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly EmailService _emailService;
        private readonly PasswordResetTokenService _passwordResetTokens;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            EmailService emailService,
            PasswordResetTokenService passwordResetTokens)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _passwordResetTokens = passwordResetTokens;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = NormalizeEmail(model.Email);
            var user = await _context.AppUsers
                .Include(appUser => appUser.Player)
                .SingleOrDefaultAsync(appUser =>
                    appUser.NormalizedEmail == normalizedEmail);

            if (user == null || !user.IsActive)
            {
                return InvalidLogin(model);
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return InvalidLogin(model);
            }

            if (passwordResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    model.Password);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Player?.Name ?? user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            if (user.PlayerId.HasValue)
            {
                claims.Add(new Claim(
                    "playerId",
                    user.PlayerId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8)
                });

            return RedirectForRole(user.Role);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [Authorize(Policy = AuthorizationPolicies.PlayerOnly)]
        [HttpGet]
        public IActionResult PlayerAccess() => View();

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied() => View();

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            TempData["ForgotPasswordMessage"] =
                "If that email is registered, a password reset link has been sent. Check your inbox and spam folder. The link expires in 30 minutes.";

            var normalizedEmail = NormalizeEmail(email ?? string.Empty);

            var user = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(appUser =>
                    appUser.NormalizedEmail == normalizedEmail &&
                    appUser.IsActive);

            if (user != null)
            {
                var token = _passwordResetTokens.CreateToken(
                    user,
                    TimeSpan.FromMinutes(30));

                var resetUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { token },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(resetUrl))
                {
                    var safeUrl =
                        System.Net.WebUtility.HtmlEncode(resetUrl);

                    await _emailService.SendAsync(
                        user.Email,
                        "Reset your ParaVolley password",
                        $"""
                        <p>We received a request to reset your ParaVolley Mpumalanga password.</p>
                        <p><a href="{safeUrl}">Reset your password</a></p>
                        <p>This link expires in 30 minutes. If you did not request a reset, you can ignore this email.</p>
                        """);
                }
            }

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewBag.Message =
                TempData["ForgotPasswordMessage"]
                ?? "If that email is registered, a password reset link has been sent. Check your inbox and spam folder. The link expires in 30 minutes.";

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (!_passwordResetTokens.TryGetUserId(token, out var userId))
            {
                TempData["Error"] =
                    "That password reset link is invalid or has expired.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(appUser =>
                    appUser.Id == userId &&
                    appUser.IsActive);

            if (user == null ||
                !_passwordResetTokens.IsValid(token, user))
            {
                TempData["Error"] =
                    "That password reset link is invalid or has expired.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(
                new ResetPasswordViewModel
                {
                    Token = token
                });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!_passwordResetTokens.TryGetUserId(
                    model.Token,
                    out var userId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "That password reset link is invalid or has expired.");
                return View(model);
            }

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(appUser =>
                    appUser.Id == userId &&
                    appUser.IsActive);

            if (user == null ||
                !_passwordResetTokens.IsValid(model.Token, user))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "That password reset link is invalid or has expired.");
                return View(model);
            }

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.NewPassword);

            await _context.SaveChangesAsync();

            TempData["LoginSuccess"] =
                "Your password has been reset. You can sign in with your new password.";

            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CreatePlayerPassword(
            string token)
        {
            if (!_passwordResetTokens.TryGetUserId(
                    token,
                    out var userId))
            {
                ViewBag.SetupError =
                    "This Player App setup link is invalid or has expired.";

                return View(
                    new ResetPasswordViewModel
                    {
                        Token = token ?? string.Empty
                    });
            }

            var user = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    appUser =>
                        appUser.Id == userId &&
                        appUser.Role == AppUserRole.Player &&
                        appUser.IsActive);

            if (user == null ||
                !_passwordResetTokens.IsValid(
                    token,
                    user))
            {
                ViewBag.SetupError =
                    "This Player App setup link is invalid or has expired.";

                return View(
                    new ResetPasswordViewModel
                    {
                        Token = token ?? string.Empty
                    });
            }

            ViewBag.PlayerEmail = user.Email;

            return View(
                new ResetPasswordViewModel
                {
                    Token = token
                });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> CreatePlayerPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!_passwordResetTokens.TryGetUserId(
                    model.Token,
                    out var userId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This Player App setup link is invalid or has expired.");

                return View(model);
            }

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(
                    appUser =>
                        appUser.Id == userId &&
                        appUser.Role == AppUserRole.Player &&
                        appUser.IsActive);

            if (user == null ||
                !_passwordResetTokens.IsValid(
                    model.Token,
                    user))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This Player App setup link is invalid or has expired.");

                return View(model);
            }

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    model.NewPassword);

            await _context.SaveChangesAsync();

            ViewBag.PlayerEmail = user.Email;

            return View(
                "PlayerPasswordCreated");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Terms() => View();

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Support() => View();

        private IActionResult InvalidLogin(LoginViewModel model)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");
            return View(model);
        }

        private IActionResult RedirectForRole(AppUserRole role)
        {
            return role switch
            {
                AppUserRole.Admin => RedirectToAction("AdminDashboard", "Home"),
                AppUserRole.Coach => RedirectToAction("CoachDashboard", "Home"),
                AppUserRole.Player => RedirectToAction(nameof(PlayerAccess)),
                _ => RedirectToAction(nameof(AccessDenied))
            };
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
