namespace SportsManagementMVC.Dtos
{
    public class QrAttendanceSessionDto
    {
        public int SessionId { get; set; }

        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
    }
}