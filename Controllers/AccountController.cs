using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var profile = await _context.UserProfiles.FirstOrDefaultAsync();

            if (profile != null &&
                model.Email.Equals(profile.Email, StringComparison.OrdinalIgnoreCase) &&
                PasswordHasher.Verify(model.Password, profile.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, profile.FullName),
                    new(ClaimTypes.Email, profile.Email),
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // "Remember me" keeps the user signed in across browser restarts for 30
                // days. Without it, the session cookie expires after 8 hours or when the
                // browser closes (IsPersistent = false), whichever comes first.
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe
                            ? DateTimeOffset.UtcNow.AddDays(30)
                            : DateTimeOffset.UtcNow.AddHours(8)
                    });

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password. Try admin@paravolley.com / Admin123! (unless you've changed it in Settings).");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ================= FORGOT / RESET PASSWORD =================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // Always show the same generic message, whether or not the email
            // matches an account - this avoids leaking which emails exist.
            var genericMessage = "If that email is on file, we've sent a link to reset your password.";

            var profile = await _context.UserProfiles.FirstOrDefaultAsync();
            if (profile != null && !string.IsNullOrWhiteSpace(email) &&
                email.Equals(profile.Email, StringComparison.OrdinalIgnoreCase))
            {
                profile.ResetToken = Guid.NewGuid().ToString("N");
                profile.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                var resetUrl = Url.Action("ResetPassword", "Account", new { token = profile.ResetToken }, Request.Scheme);
                var sent = await _emailService.SendAsync(
                    profile.Email,
                    "Reset your ParaVolley Mpumalanga password",
                    $"<p>We received a request to reset your password.</p><p><a href=\"{resetUrl}\">Click here to reset your password</a></p><p>This link expires in 1 hour. If you didn't request this, you can ignore this email.</p>");

                ViewBag.DemoResetUrl = sent ? null : resetUrl;
            }

            ViewBag.Message = genericMessage;
            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p =>
                p.ResetToken == token && p.ResetTokenExpiry != null && p.ResetTokenExpiry > DateTime.UtcNow);

            if (profile == null)
            {
                TempData["Error"] = "This password reset link is invalid or has expired. Please request a new one.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p =>
                p.ResetToken == model.Token && p.ResetTokenExpiry != null && p.ResetTokenExpiry > DateTime.UtcNow);

            if (profile == null)
            {
                TempData["Error"] = "This password reset link is invalid or has expired. Please request a new one.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 8)
            {
                ModelState.AddModelError(string.Empty, "Password must be at least 8 characters.");
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View(model);
            }

            profile.PasswordHash = PasswordHasher.Hash(model.NewPassword);
            profile.ResetToken = null;
            profile.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your password was reset. Please sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        // ================= LEGAL / SUPPORT PAGES =================

        [HttpGet]
        public IActionResult Privacy() => View();

        [HttpGet]
        public IActionResult Terms() => View();

        [HttpGet]
        public IActionResult Support() => View();
    }
}
