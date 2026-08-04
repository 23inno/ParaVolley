namespace SportsManagementMVC.Models
{
    public class MonthlyPlayerStat
    {
        public string Month { get; set; } = string.Empty;
        public int Active { get; set; }
        public int New { get; set; }
        public int Inactive { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalPlayers { get; set; }
        public int ActivePlayers { get; set; }
        public int TotalCoaches { get; set; }
        public int UpcomingEvents { get; set; }
        public int UpcomingMatches { get; set; }
        public int TotalAnnouncements { get; set; }

        public List<Event> NextEvents { get; set; } = new();
        public List<Match> NextMatches { get; set; } = new();
        public List<Announcement> RecentAnnouncements { get; set; } = new();
        public List<MonthlyPlayerStat> PlayerStatsChart { get; set; } = new();
    }
}
