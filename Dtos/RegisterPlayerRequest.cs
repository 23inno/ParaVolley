using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Dtos
{
    public class RegisterPlayerRequest
    {
        [Required]
        [StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Town { get; set; }

        [StringLength(50)]
        public string? ExperienceLevel { get; set; }

        [StringLength(50)]
        public string? PreferredPosition { get; set; }

        [Required]
        [StringLength(500)]
        public string Classification { get; set; } = string.Empty;

        [StringLength(120)]
        public string? EmergencyContactName { get; set; }

        [Phone]
        [StringLength(30)]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(1000)]
        public string? MedicalNotes { get; set; }

        public bool Consent { get; set; }
    }
}
