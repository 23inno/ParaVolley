using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("Settings/Notifications")]
public sealed class NotificationSettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationDeliveryService _delivery;

    public NotificationSettingsController(
        ApplicationDbContext context,
        EmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationDeliveryService> logger)
    {
        _context = context;
        _delivery = new NotificationDeliveryService(
            context,
            emailService,
            configuration,
            logger);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken = default)
    {
        ViewBag.ActiveSettingsTab = "Notifications";

        await _delivery.EnsureDefaultPreferencesAsync(cancellationToken);

        var preferences = await _context.NotificationPreferences
            .AsNoTracking()
            .OrderBy(item => item.EventLabel)
            .ToListAsync(cancellationToken);

        string? adminEmail = null;
        var appUserIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(appUserIdValue, out var appUserId))
        {
            adminEmail = await _context.AppUsers
                .AsNoTracking()
                .Where(user =>
                    user.Id == appUserId &&
                    user.Role == AppUserRole.Admin)
                .Select(user => user.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var adminPhone = await _context.UserProfiles
            .AsNoTracking()
            .Select(profile => profile.Phone)
            .FirstOrDefaultAsync(cancellationToken);

        return View(
            new NotificationSettingsViewModel
            {
                Preferences = preferences,
                Email = _delivery.GetEmailStatus(),
                Sms = _delivery.GetSmsStatus(),
                Push = _delivery.GetPushStatus(),
                DefaultTestEmail = adminEmail,
                DefaultTestPhone = adminPhone
            });
    }

    [HttpPost("preference")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePreference(
        int id,
        string channel,
        CancellationToken cancellationToken = default)
    {
        var preference = await _context.NotificationPreferences
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (preference == null)
        {
            return NotFound();
        }

        switch ((channel ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "email":
                preference.EmailEnabled = !preference.EmailEnabled;
                break;
            case "sms":
                preference.SmsEnabled = !preference.SmsEnabled;
                break;
            case "push":
                preference.PushEnabled = !preference.PushEnabled;
                break;
            default:
                return BadRequest();
        }

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] =
            $"{preference.EventLabel} notification preference updated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("test-email")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> TestEmail(string? email)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            TempData["Error"] = "Enter a valid email address for the test.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _delivery.SendEmailAsync(
            normalizedEmail,
            "ParaVolley notification test",
            "<p>This is a test email from the ParaVolley Mpumalanga notification system.</p>");

        SetResult(result);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("test-sms")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> TestSms(
        string? phone,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedPhone) ||
            !new PhoneAttribute().IsValid(normalizedPhone))
        {
            TempData["Error"] = "Enter a valid phone number for the test.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _delivery.SendSmsAsync(
            normalizedPhone,
            "ParaVolley Mpumalanga test notification.",
            cancellationToken);

        SetResult(result);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("test-push")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> TestPush(
        CancellationToken cancellationToken = default)
    {
        var result = await _delivery.SendPushToPlayersAsync(
            "ParaVolley Mpumalanga",
            "Push notifications are connected and working.",
            cancellationToken);

        SetResult(result);
        return RedirectToAction(nameof(Index));
    }

    private void SetResult(NotificationDeliveryResult result)
    {
        TempData[result.Success ? "Success" : "Error"] = result.Message;
    }
}
