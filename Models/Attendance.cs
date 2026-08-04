using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportsManagementMVC.Models
{
    public enum AttendanceStatus
    {
        Present,
        Absent
    }

    public class Attendance
    {
        public int Id { get; set; }

        [Required, Display(Name = "Player")]
        public int PlayerId { get; set; }

        [ForeignKey(nameof(PlayerId))]
        public Player? Player { get; set; }

        [Required, Display(Name = "Event")]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    }
}
