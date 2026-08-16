namespace SportsManagementMVC.Dtos
{
    public class EventDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public string Time { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public int Participants { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}