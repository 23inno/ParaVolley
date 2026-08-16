namespace SportsManagementMVC.Dtos
{
    public class EventRegistrationDto
    {
        public int Id { get; set; }

        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string EventTime { get; set; } = string.Empty;

        public string EventLocation { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string EventStatus { get; set; } = string.Empty;

        public string RegistrationStatus { get; set; } = string.Empty;

        public DateTime RegisteredAtUtc { get; set; }
    }
}