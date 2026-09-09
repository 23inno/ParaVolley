using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum PlayerApplicationStatus
    {
        Pending,
        Approved,
        Declined
    }

    public class PlayerRegistrationApplication
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        [Display(Name = "Town / City")]
        public string? Town { get; set; }

        [StringLength(50)]
        [Display(Name = "Experience Level")]
        public string? ExperienceLevel { get; set; }

        [StringLength(50)]
        [Display(Name = "Preferred Position")]
        public string? PreferredPosition { get; set; }

        [StringLength(500)]
        [Display(Name = "Classification / Participation Information")]
        public string? Classification { get; set; }

        [StringLength(120)]
        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [Phone]
        [StringLength(30)]
        [Display(Name = "Emergency Contact Number")]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(1000)]
        [Display(Name = "Additional Support Information")]
        public string? MedicalNotes { get; set; }
        public bool Consent { get; set; }

        public PlayerApplicationStatus Status { get; set; }
            = PlayerApplicationStatus.Pending;

        public bool IsRead { get; set; }

        public DateTime SubmittedAtUtc { get; set; }
            = DateTime.UtcNow;
    }
}