using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum CoachStatus
    {
        Active,
        Available,
        [Display(Name = "On Leave")]
        OnLeave
    }

    public class Coach
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Assigned Team")]
        public string? AssignedTeam { get; set; }

        public CoachStatus Status { get; set; } = CoachStatus.Available;

        public string Specialty { get; set; } = string.Empty;

        public string Experience { get; set; } = string.Empty;

        public string Certifications { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }
    }
}
