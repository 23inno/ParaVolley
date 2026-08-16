namespace SportsManagementMVC.Models
{
    public enum EventRegistrationStatus
    {
        Registered,
        Cancelled
    }

    public class EventRegistration
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }

        public Player Player { get; set; } = null!;

        public int EventId { get; set; }

        public Event Event { get; set; } = null!;

        public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

        public EventRegistrationStatus Status { get; set; } =
            EventRegistrationStatus.Registered;
    }
}