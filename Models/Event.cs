using System.ComponentModel.DataAnnotations;

namespace SportsManagementMVC.Models
{
    public enum EventType
    {
        Tournament,
        Practice,
        Match,
        Workshop
    }

    public enum EventStatus
    {
        Upcoming,
        [Display(Name = "In Progress")]
        InProgress,
        Completed,
        Cancelled
    }

    public class Event
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Time")]
        public string Time { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        public EventType Type { get; set; } = EventType.Practice;

        [Range(0, int.MaxValue)]
        public int Participants { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Upcoming;

        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        // Navigation
        public ICollection<Attendance>? AttendanceRecords { get; set; }
    }
}
