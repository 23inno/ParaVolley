using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models.Api
{
    public class PlayerProfileResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string Team { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int Age { get; set; }

        public int Matches { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string EmergencyContactName { get; set; } = string.Empty;

        public string EmergencyContactPhone { get; set; } = string.Empty;

        public string? JoinedDate { get; set; }

        public string Disability { get; set; } = string.Empty;

        public bool HasProfilePhoto { get; set; }
    }

    public class UpdatePlayerProfileRequest
    {
        [Range(5, 100)]
        public int Age { get; set; }

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(120)]
        public string EmergencyContactName { get; set; } = string.Empty;

        [StringLength(30)]
        public string EmergencyContactPhone { get; set; } = string.Empty;
    }
}
