namespace SportsManagementMVC.Models
{
    public class RecentUpdateItem
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Tag { get; set; } = string.Empty;

        /// <summary>URL to load. If IsModal is true, it's fetched via AJAX into the shared modal; otherwise it's a normal navigation link.</summary>
        public string Url { get; set; } = string.Empty;
        public bool IsModal { get; set; }
    }
}
