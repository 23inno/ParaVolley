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

        // Legacy, non-authoritative column retained for migration compatibility.
        // Authentication credentials are stored only on AppUser.
        public string PasswordHash { get; set; } = string.Empty;

        public bool TwoFactorEnabled { get; set; }

        // Legacy display-profile reset fields. They are not used for authentication.
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

        [EmailAddress, StringLength(200)]
        public string? OfficialEmail { get; set; } =
            "paravolleympumalanga@gmail.com";

        [Url, StringLength(300)]
        public string? OfficialWebsite { get; set; } =
            "https://paravolley-production.up.railway.app/";

        [Phone, StringLength(50)]
        public string? OfficialPhone { get; set; }

        [StringLength(200)]
        public string? OfficeLocation { get; set; } =
            "Mpumalanga, South Africa";

        // ParaVolley operates in South Africa. This value is retained for
        // compatibility and is intentionally fixed in the System UI.
        public string Timezone { get; set; } =
            "Africa/Johannesburg (SAST, UTC+2)";

        [Required, RegularExpression(@"^\d{4}$")]
        public string ActiveSeason { get; set; } = "2026";

        [Range(0, 100)]
        public int MinAttendancePercent { get; set; } = 75;

        // Localization is not implemented yet. Keep the stored legacy value
        // fixed to English rather than exposing a non-functional selector.
        public string Language { get; set; } = "English";

        public bool AcceptPlayerApplications { get; set; } = true;

        public bool AllowEventRegistration { get; set; } = true;

        [StringLength(150)]
        public string DefaultTeamName { get; set; } =
            "ParaVolley Mpumalanga";

        [StringLength(100)]
        public string DefaultProvince { get; set; } = "Mpumalanga";

        [StringLength(100)]
        public string DefaultCountry { get; set; } = "South Africa";

        public bool MaintenanceMode { get; set; }

        [StringLength(300)]
        public string MaintenanceMessage { get; set; } =
            "ParaVolley Mpumalanga is temporarily undergoing maintenance. Please try again shortly.";

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

        [StringLength(200)]
        public string? Note { get; set; }

        [StringLength(64)]
        public string? Sha256 { get; set; }

        [StringLength(260)]
        public string? FileName { get; set; }

        public bool IsAutomatic { get; set; }
    }

    public enum BackupScheduleFrequency
    {
        Daily,
        Weekly
    }

    public class BackupSettings
    {
        public int Id { get; set; } = 1;

        public bool AutomaticEnabled { get; set; }

        public BackupScheduleFrequency Frequency { get; set; } =
            BackupScheduleFrequency.Daily;

        [Range(0, 23)]
        public int HourSast { get; set; } = 2;

        public DayOfWeek WeeklyDay { get; set; } = DayOfWeek.Sunday;

        [Range(5, 100)]
        public int RetentionCount { get; set; } = 30;

        public DateTime? LastAutomaticRunAt { get; set; }
    }

    public class BackupDataViewModel
    {
        public List<BackupRecord> Backups { get; set; } = new();

        public BackupSettings Settings { get; set; } = new();

        public string? Search { get; set; }

        public string Status { get; set; } = "all";

        public int TotalCount { get; set; }

        public int SuccessfulCount { get; set; }

        public int FailedCount { get; set; }

        public DateTime? NextScheduledRunAt { get; set; }
    }

    public class BackupSnapshotDetailsViewModel
    {
        public BackupRecord Backup { get; set; } = new();

        public Dictionary<string, int> RecordCounts { get; set; } = new();

        public bool FileExists { get; set; }

        public bool? IntegrityValid { get; set; }
    }
}
