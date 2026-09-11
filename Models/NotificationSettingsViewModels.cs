namespace SportsManagementMVC.Models;

public sealed class NotificationProviderStatus
{
    public bool Configured { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

public sealed class NotificationSettingsViewModel
{
    public List<NotificationPreference> Preferences { get; init; } = new();

    public NotificationProviderStatus Email { get; init; } = new();
    public NotificationProviderStatus Sms { get; init; } = new();
    public NotificationProviderStatus Push { get; init; } = new();

    public string? DefaultTestEmail { get; init; }
    public string? DefaultTestPhone { get; init; }
}
