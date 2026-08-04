using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    // Singleton-style row (Id = 1) holding the signed-in admin's profile.
    public class UserProfile
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = "Admin User";

        [Required, EmailAddress]
        public string Email { get; set; } = "admin@paravolley.com";

        [Phone]
        public string Phone { get; set; } = "+27 13 000 0000";

        public string Role { get; set; } = "Administrator";

        [DataType(DataType.MultilineText)]
        public string Bio { get; set; } = "ParaVolley Mpumalanga system administrator.";

        public string? AvatarPath { get; set; }

        // Simplified password storage for this demo app (SHA-256 hash, no salt/Identity).
        // In a production app this would use ASP.NET Core Identity's password hasher.
        public string PasswordHash { get; set; } = string.Empty;

        public bool TwoFactorEnabled { get; set; }

        // Password reset flow
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }

    public class NotificationPreference
    {
        public int Id { get; set; }
        public string EventKey { get; set; } = string.Empty;
        public string EventLabel { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool PushEnabled { get; set; }
    }

    public enum SystemUserRole { Admin, Coach }

    public class SystemUser
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public SystemUserRole Role { get; set; } = SystemUserRole.Coach;

        public bool IsActive { get; set; } = true;
    }

    // Singleton-style row (Id = 1) holding organisation-wide configuration.
    public class OrganisationSettings
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string OrganisationName { get; set; } = "ParaVolley Mpumalanga";

        public string Timezone { get; set; } = "Africa/Johannesburg (SAST, UTC+2)";

        public string ActiveSeason { get; set; } = "2026";

        [Range(0, 100)]
        public int MinAttendancePercent { get; set; } = 75;

        public string Language { get; set; } = "English";

        public string? LogoPath { get; set; }
    }

    public enum AppTheme { Light, Dark, System }
    public enum LayoutDensity { Compact, Comfortable, Spacious }

    // Singleton-style row (Id = 1) holding the site-wide appearance/theme settings.
    public class AppearanceSettings
    {
        public int Id { get; set; }

        public AppTheme Theme { get; set; } = AppTheme.Light;

        public string AccentColor { get; set; } = "#0B6E4F";

        public LayoutDensity Density { get; set; } = LayoutDensity.Comfortable;
    }

    public class BackupRecord
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public long SizeBytes { get; set; }
        public bool Success { get; set; } = true;
    }
}
