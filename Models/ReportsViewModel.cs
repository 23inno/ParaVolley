namespace SportsManagementMVC.Models
{
    public class TeamBreakdown
    {
        public string Team { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public int ActiveCount { get; set; }
    }

    public class EventTypeBreakdown
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AttendanceSummary
    {
        public string EventTitle { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public double AttendanceRate =>
            (PresentCount + AbsentCount) == 0 ? 0 : Math.Round(100.0 * PresentCount / (PresentCount + AbsentCount), 1);
    }

    public class ReportsViewModel
    {
        // Reports Library
        public List<Report> Reports { get; set; } = new();
        public int TotalReportsCount { get; set; }
        public int PublishedCount { get; set; }
        public int DraftCount { get; set; }

        // Quick statistics calculated from PostgreSQL data.
        public string WeeklyAverage { get; set; } = "0%";
        public string SessionsThisMonth { get; set; } = "0";
        public string PerfectAttendanceCount { get; set; } = "0";

        // Real data-driven analytics (unchanged from before)
        public int TotalPlayers { get; set; }
        public int ActivePlayers { get; set; }
        public int InactivePlayers { get; set; }
        public int TotalCoaches { get; set; }
        public int TotalMatches { get; set; }
        public int CompletedMatches { get; set; }
        public int WinsA { get; set; } // Matches where our team (TeamA) had the higher score
        public int LossesA { get; set; }

        public List<TeamBreakdown> TeamBreakdowns { get; set; } = new();
        public List<EventTypeBreakdown> EventTypeBreakdowns { get; set; } = new();
        public List<AttendanceSummary> AttendanceSummaries { get; set; } = new();
    }
}
