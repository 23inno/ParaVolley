using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class QrAttendanceSession
    {
        public int Id { get; set; }

        public int EventId { get; set; }

        public Event Event { get; set; } = null!;

        [Required]
        [StringLength(64, MinimumLength = 64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public bool IsRevoked { get; set; }

        public int? CreatedByAppUserId { get; set; }

        public AppUser? CreatedByAppUser { get; set; }
    }
}