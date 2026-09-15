using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public class QrAttendanceAttempt
    {
        public int Id { get; set; }

        public int EventId { get; set; }

        public int QrAttendanceSessionId { get; set; }

        public int? PlayerId { get; set; }

        public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;

        [Required, StringLength(40)]
        public string Outcome { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Message { get; set; } = string.Empty;
    }
}
