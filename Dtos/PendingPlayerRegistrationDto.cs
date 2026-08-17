namespace SportsManagementMVC.Dtos
{
    public class PendingPlayerRegistrationDto
    {
        public int PlayerId { get; set; }

        public int AppUserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string Team { get; set; } = string.Empty;

        public int Age { get; set; }

        public string Disability { get; set; } = string.Empty;
    }
}