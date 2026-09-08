using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool EmailSent { get; set; } = false;
    }
}