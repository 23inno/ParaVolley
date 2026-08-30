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
    public class CoachDashboardViewModel
    {
    public int ActivePlayers { get; set; }
    public int AttendanceRecords { get; set; }
    public int PresentRecords { get; set; }
    public List<Event> UpcomingTraining { get; set; } = new();
    public List<Match> UpcomingMatches { get; set; } = new();
    public List<Announcement> RecentAnnouncements { get; set; } = new();
    public int AttendanceRate => AttendanceRecords == 0 ? 0 : (int)Math.Round(PresentRecords * 100d / AttendanceRecords);
    }

    public class CoachPlayerViewModel
    {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public PlayerStatus Status { get; set; }
    public int Matches { get; set; }
    }
}
