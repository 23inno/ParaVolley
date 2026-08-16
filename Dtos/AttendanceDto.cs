namespace SportsManagementMVC.Dtos
{
    public class AttendanceDto
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }

        public string PlayerName { get; set; } = string.Empty;

        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string EventTime { get; set; } = string.Empty;

        public string EventLocation { get; set; } = string.Empty;

        public DateTime AttendanceDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}