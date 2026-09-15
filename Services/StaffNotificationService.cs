using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services;

public sealed class StaffNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationDeliveryService _delivery;
    private readonly ILogger<StaffNotificationService> _logger;

    public StaffNotificationService(
        ApplicationDbContext context,
        NotificationDeliveryService delivery,
        ILogger<StaffNotificationService> logger)
    {
        _context = context;
        _delivery = delivery;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDeliveryResult>> SendAsync(
        string eventKey,
        string subject,
        string textBody,
        string? htmlBody = null,
        bool includeCoaches = true,
        CancellationToken cancellationToken = default)
    {
        await _delivery.EnsureDefaultPreferencesAsync(cancellationToken);

        var preference = await _context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.EventKey == eventKey,
                cancellationToken);

        if (preference == null)
        {
            return new[]
            {
                new NotificationDeliveryResult(
                    false,
                    $"No notification preference exists for '{eventKey}'.")
            };
        }

        var results = new List<NotificationDeliveryResult>();

        if (preference.EmailEnabled && _delivery.EmailConfigured)
        {
            var emails = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            var appUserEmails = await _context.AppUsers
                .AsNoTracking()
                .Where(user =>
                    user.IsActive &&
                    (user.Role == AppUserRole.Admin ||
                     (includeCoaches && user.Role == AppUserRole.Coach)))
                .Select(user => user.Email)
                .ToListAsync(cancellationToken);

            foreach (var email in appUserEmails)
            {
                AddIfPresent(emails, email);
            }

            var profileEmail = await _context.UserProfiles
                .AsNoTracking()
                .Select(profile => profile.Email)
                .FirstOrDefaultAsync(cancellationToken);
            AddIfPresent(emails, profileEmail);

            var organisationEmail = await _context.OrganisationSettings
                .AsNoTracking()
                .Select(settings => settings.OfficialEmail)
                .FirstOrDefaultAsync(cancellationToken);
            AddIfPresent(emails, organisationEmail);

            if (includeCoaches)
            {
                var coachEmails = await _context.Coaches
                    .AsNoTracking()
                    .Where(coach => coach.Status != CoachStatus.OnLeave)
                    .Select(coach => coach.Email)
                    .ToListAsync(cancellationToken);

                foreach (var email in coachEmails)
                {
                    AddIfPresent(emails, email);
                }
            }

            foreach (var email in emails)
            {
                results.Add(
                    await _delivery.SendEmailAsync(
                        email,
                        subject,
                        htmlBody ?? textBody));
            }
        }

        if (preference.SmsEnabled && _delivery.SmsConfigured)
        {
            var phones = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            var profilePhone = await _context.UserProfiles
                .AsNoTracking()
                .Select(profile => profile.Phone)
                .FirstOrDefaultAsync(cancellationToken);
            AddIfPresent(phones, profilePhone);

            var organisationPhone = await _context.OrganisationSettings
                .AsNoTracking()
                .Select(settings => settings.OfficialPhone)
                .FirstOrDefaultAsync(cancellationToken);
            AddIfPresent(phones, organisationPhone);

            if (includeCoaches)
            {
                var coachPhones = await _context.Coaches
                    .AsNoTracking()
                    .Where(coach => coach.Status != CoachStatus.OnLeave)
                    .Select(coach => coach.Phone)
                    .ToListAsync(cancellationToken);

                foreach (var phone in coachPhones)
                {
                    AddIfPresent(phones, phone);
                }
            }

            foreach (var phone in phones)
            {
                results.Add(
                    await _delivery.SendSmsAsync(
                        phone,
                        textBody,
                        cancellationToken));
            }
        }

        if (preference.PushEnabled)
        {
            results.Add(
                new NotificationDeliveryResult(
                    false,
                    "Push is not available for staff-only alerts because the Android app currently subscribes Player accounts only."));
        }

        if (results.Count == 0)
        {
            results.Add(
                new NotificationDeliveryResult(
                    false,
                    "No enabled and configured staff notification channel is available for this event."));
        }

        foreach (var result in results.Where(item => !item.Success))
        {
            _logger.LogWarning(
                "Staff notification '{EventKey}' reported: {Message}",
                eventKey,
                result.Message);
        }

        return results;
    }

    private static void AddIfPresent(
        HashSet<string> values,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value.Trim());
        }
    }
}
