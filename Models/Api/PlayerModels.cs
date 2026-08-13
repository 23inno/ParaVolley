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

        public string Disability { get; set; } = string.Empty;
    }
}