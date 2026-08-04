namespace SportsManagementMVC.Models
{
    public class MonthlyAttendanceTrend
    {
        public string Label { get; set; } = string.Empty;
        public int Actual { get; set; }
        public int Target { get; set; }
    }

    public class ParticipationSplitStat
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TeamComparisonStat
    {
        public string Team { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Absent { get; set; }
    }

    public class TopPlayerStat
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Rate { get; set; }
    }

    public class SessionRecordRow
    {
        public DateTime Date { get; set; }
        public string Session { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Absent { get; set; }
        public double Rate => (Present + Absent) == 0 ? 0 : Math.Round(100.0 * Present / (Present + Absent), 0);
        public string Status => Rate >= 90 ? "Excellent" : "Good";
    }

    public class AttendanceDashboardViewModel
    {
        public int TotalAttendance { get; set; }
        public double AttendanceRate { get; set; }
        public int ActiveSessions { get; set; }
        public string TopPerformerName { get; set; } = string.Empty;
        public int TopPerformerRate { get; set; }

        public List<MonthlyAttendanceTrend> TrendsWeek { get; set; } = new();
        public List<MonthlyAttendanceTrend> TrendsMonth { get; set; } = new();
        public List<MonthlyAttendanceTrend> TrendsYear { get; set; } = new();

        public List<ParticipationSplitStat> ParticipationSplit { get; set; } = new();
        public List<TeamComparisonStat> TeamComparison { get; set; } = new();
        public List<TopPlayerStat> MostActivePlayers { get; set; } = new();
        public List<SessionRecordRow> SessionRecords { get; set; } = new();

        public List<string> Teams { get; set; } = new();
    }
}
