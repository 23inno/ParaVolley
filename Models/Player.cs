using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum PlayerStatus
    {
        Active,
        Inactive
    }

    public class Player
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        public string Team { get; set; } = string.Empty;

        public PlayerStatus Status { get; set; } = PlayerStatus.Active;

        [Range(5, 100)]
        public int Age { get; set; }

        [Display(Name = "Matches Played")]
        [Range(0, int.MaxValue)]
        public int Matches { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Disability Classification")]
        public string Disability { get; set; } = string.Empty;

        // Navigation
        public ICollection<Attendance>? AttendanceRecords { get; set; }

        public ICollection<EventRegistration> EventRegistrations { get; set; } =
    new List<EventRegistration>();
    }
}
