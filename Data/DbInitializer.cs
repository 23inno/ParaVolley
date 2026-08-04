using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Players.Any())
            {
                return; // already seeded
            }

            var players = new List<Player>
            {
                new() { Name = "John Doe", Position = "Outside Hitter", Team = "Team A", Status = PlayerStatus.Active, Age = 24, Matches = 45, Email = "john.doe@email.com", Phone = "+27 76 000 0001", Disability = "Wheelchair" },
                new() { Name = "Sarah Smith", Position = "Setter", Team = "Team A", Status = PlayerStatus.Active, Age = 22, Matches = 38, Email = "sarah.smith@email.com", Phone = "+27 76 000 0002", Disability = "Limb Difference" },
                new() { Name = "Michael Johnson", Position = "Middle Blocker", Team = "Team B", Status = PlayerStatus.Active, Age = 26, Matches = 52, Email = "michael.j@email.com", Phone = "+27 76 000 0003", Disability = "Wheelchair" },
                new() { Name = "Emily Davis", Position = "Libero", Team = "Team A", Status = PlayerStatus.Active, Age = 23, Matches = 41, Email = "emily.davis@email.com", Phone = "+27 76 000 0004", Disability = "Visual Impairment" },
                new() { Name = "Robert Brown", Position = "Opposite Hitter", Team = "Team B", Status = PlayerStatus.Inactive, Age = 25, Matches = 36, Email = "robert.b@email.com", Phone = "+27 76 000 0005", Disability = "Hearing Impairment" },
                new() { Name = "Lisa Wilson", Position = "Outside Hitter", Team = "Team C", Status = PlayerStatus.Active, Age = 21, Matches = 29, Email = "lisa.wilson@email.com", Phone = "+27 76 000 0006", Disability = "Wheelchair" },
                new() { Name = "David Martinez", Position = "Setter", Team = "Team B", Status = PlayerStatus.Active, Age = 27, Matches = 58, Email = "david.m@email.com", Phone = "+27 76 000 0007", Disability = "Limb Difference" },
                new() { Name = "Jennifer Taylor", Position = "Middle Blocker", Team = "Team C", Status = PlayerStatus.Active, Age = 24, Matches = 43, Email = "jennifer.t@email.com", Phone = "+27 76 000 0008", Disability = "Wheelchair" },
            };
            context.Players.AddRange(players);

            var coaches = new List<Coach>
            {
                new() { Name = "John Smith", Email = "john.smith@paravolley.com", Phone = "+27 82 123 4567", AssignedTeam = "Team A", Status = CoachStatus.Active, Specialty = "Attack Strategy", Experience = "8 years", Certifications = "Level 3 Certified" },
                new() { Name = "Sarah Johnson", Email = "sarah.johnson@paravolley.com", Phone = "+27 83 234 5678", AssignedTeam = "Team B", Status = CoachStatus.Active, Specialty = "Defense Training", Experience = "6 years", Certifications = "Level 2 Certified" },
                new() { Name = "Michael Williams", Email = "michael.w@paravolley.com", Phone = "+27 84 345 6789", AssignedTeam = "Team C", Status = CoachStatus.Active, Specialty = "Fitness & Conditioning", Experience = "10 years", Certifications = "Level 3 Certified" },
                new() { Name = "Emily Davis", Email = "emily.davis@paravolley.com", Phone = "+27 85 456 7890", AssignedTeam = "Team A", Status = CoachStatus.Active, Specialty = "Youth Development", Experience = "5 years", Certifications = "Level 2 Certified" },
                new() { Name = "David Martinez", Email = "david.m@paravolley.com", Phone = "+27 86 567 8901", AssignedTeam = null, Status = CoachStatus.Available, Specialty = "Technical Skills", Experience = "7 years", Certifications = "Level 3 Certified" },
                new() { Name = "Lisa Anderson", Email = "lisa.anderson@paravolley.com", Phone = "+27 87 678 9012", AssignedTeam = "Team D", Status = CoachStatus.Active, Specialty = "Mental Coaching", Experience = "4 years", Certifications = "Level 1 Certified" },
                new() { Name = "Robert Brown", Email = "robert.brown@paravolley.com", Phone = "+27 88 789 0123", AssignedTeam = null, Status = CoachStatus.OnLeave, Specialty = "Tactical Analysis", Experience = "12 years", Certifications = "Level 4 Certified" },
                new() { Name = "Jennifer Wilson", Email = "jennifer.w@paravolley.com", Phone = "+27 89 890 1234", AssignedTeam = "Team B", Status = CoachStatus.Active, Specialty = "Injury Prevention", Experience = "6 years", Certifications = "Level 2 Certified" },
            };
            context.Coaches.AddRange(coaches);

            var events = new List<Event>
            {
                new() { Title = "Provincial Championship", Date = new DateTime(2026, 5, 15), Time = "14:00", Location = "Mbombela Stadium", Type = EventType.Tournament, Participants = 120, Status = EventStatus.Upcoming, Description = "Annual provincial championship featuring all regional teams." },
                new() { Title = "Training Session", Date = new DateTime(2026, 5, 12), Time = "16:00", Location = "Training Grounds", Type = EventType.Practice, Participants = 45, Status = EventStatus.Upcoming, Description = "Regular weekly training session for all squads." },
                new() { Title = "Regional Finals", Date = new DateTime(2026, 4, 28), Time = "10:00", Location = "Sports Complex", Type = EventType.Tournament, Participants = 80, Status = EventStatus.Completed, Description = "Regional finals concluded with ParaVolley Mpumalanga winning 3-1." },
                new() { Title = "Team Building Workshop", Date = new DateTime(2026, 5, 14), Time = "09:00", Location = "Community Center", Type = EventType.Workshop, Participants = 35, Status = EventStatus.Upcoming, Description = "Team cohesion and leadership development workshop." },
                new() { Title = "Inter-Provincial Match", Date = new DateTime(2026, 5, 8), Time = "15:30", Location = "Main Arena", Type = EventType.Match, Participants = 60, Status = EventStatus.InProgress, Description = "Competitive inter-provincial fixture against Gauteng." },
            };
            context.Events.AddRange(events);

            var matches = new List<Match>
            {
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "Gauteng Thunder", Date = new DateTime(2026, 5, 15), Time = "14:00", Venue = "Mbombela Stadium", Tournament = "Provincial Championship", Status = MatchStatus.Scheduled, ScoreA = null, ScoreB = null },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "KZN Warriors", Date = new DateTime(2026, 5, 12), Time = "16:00", Venue = "Training Grounds", Tournament = "Friendly Match", Status = MatchStatus.InProgress, ScoreA = 2, ScoreB = 1 },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "Limpopo Lions", Date = new DateTime(2026, 4, 28), Time = "15:30", Venue = "Sports Complex", Tournament = "Regional Finals", Status = MatchStatus.Completed, ScoreA = 3, ScoreB = 1 },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "Western Cape Waves", Date = new DateTime(2026, 4, 22), Time = "18:00", Venue = "Main Arena", Tournament = "Regional Finals", Status = MatchStatus.Completed, ScoreA = 2, ScoreB = 3 },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "Eastern Cape Eagles", Date = new DateTime(2026, 5, 18), Time = "13:00", Venue = "Community Stadium", Tournament = "Provincial Championship", Status = MatchStatus.Scheduled, ScoreA = null, ScoreB = null },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "Free State Strikers", Date = new DateTime(2026, 5, 20), Time = "17:00", Venue = "Mbombela Stadium", Tournament = "Provincial Championship", Status = MatchStatus.Scheduled, ScoreA = null, ScoreB = null },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "Northern Cape Knights", Date = new DateTime(2026, 4, 15), Time = "14:30", Venue = "Training Grounds", Tournament = "Friendly Match", Status = MatchStatus.Completed, ScoreA = 3, ScoreB = 0 },
                new() { TeamA = "ParaVolley Mpumalanga", TeamB = "North West Titans", Date = new DateTime(2026, 5, 10), Time = "19:00", Venue = "Sports Complex", Tournament = "Friendly Match", Status = MatchStatus.Cancelled, ScoreA = null, ScoreB = null },
            };
            context.Matches.AddRange(matches);

            var announcements = new List<Announcement>
            {
                new() { Title = "Provincial Championship Registration Now Open", Excerpt = "Register your team for the upcoming Provincial Championship scheduled for May 15, 2026. Early bird discounts available until May 12.", Content = "Teams wishing to participate must submit their registration forms along with proof of eligibility and player disability classifications. Contact admin@paravolley.com for the registration package.", Author = "Admin User", Date = new DateTime(2026, 5, 10), Category = AnnouncementCategory.Event, IsPinned = true, Views = 324 },
                new() { Title = "New Training Facility Opening", Excerpt = "We are excited to announce the opening of our new state-of-the-art training facility in Mbombela.", Content = "The facility includes two full-size volleyball courts with wheelchair-accessible surfaces, a gym, physiotherapy suite, and a video analysis room. Opening ceremony is scheduled for May 20, 2026.", Author = "Admin User", Date = new DateTime(2026, 5, 8), Category = AnnouncementCategory.Announcement, IsPinned = true, Views = 287 },
                new() { Title = "Team ParaVolley Wins Regional Finals", Excerpt = "Congratulations to Team ParaVolley for securing first place at the Regional Finals with a decisive 3-1 victory.", Content = "The team delivered an outstanding performance across all four sets. Captain David Martinez was named MVP of the tournament. The team now advances to the national semifinals.", Author = "Coach Smith", Date = new DateTime(2026, 4, 29), Category = AnnouncementCategory.News, IsPinned = false, Views = 456 },
                new() { Title = "Updated Training Schedule", Excerpt = "Please note the updated training schedule for May 2026. All sessions will now start at 4:00 PM.", Content = "The schedule change applies to all teams effective May 6. Monday and Wednesday sessions are for Teams A and B; Tuesday and Thursday for Teams C and D. Friday remains a joint session.", Author = "Admin User", Date = new DateTime(2026, 5, 5), Category = AnnouncementCategory.Update, IsPinned = false, Views = 198 },
            };
            context.Announcements.AddRange(announcements);

            context.SaveChanges();

            // Seed a handful of attendance records now that Players/Events have Ids
            var attendance = new List<Attendance>
            {
                new() { PlayerId = players[0].Id, EventId = events[1].Id, Date = events[1].Date, Status = AttendanceStatus.Present },
                new() { PlayerId = players[1].Id, EventId = events[1].Id, Date = events[1].Date, Status = AttendanceStatus.Present },
                new() { PlayerId = players[2].Id, EventId = events[1].Id, Date = events[1].Date, Status = AttendanceStatus.Absent },
                new() { PlayerId = players[3].Id, EventId = events[2].Id, Date = events[2].Date, Status = AttendanceStatus.Present },
                new() { PlayerId = players[4].Id, EventId = events[2].Id, Date = events[2].Date, Status = AttendanceStatus.Present },
                new() { PlayerId = players[5].Id, EventId = events[4].Id, Date = events[4].Date, Status = AttendanceStatus.Present },
            };
            context.Attendances.AddRange(attendance);

            var sponsors = new List<Sponsor>
            {
                new() { Name = "Sport South Africa", Tier = SponsorTier.Gold },
                new() { Name = "Mpumalanga Tourism", Tier = SponsorTier.Gold },
                new() { Name = "Fitness Pro", Tier = SponsorTier.Silver },
                new() { Name = "Sports Gear Co", Tier = SponsorTier.Silver },
                new() { Name = "Local Business Hub", Tier = SponsorTier.Bronze },
            };
            context.Sponsors.AddRange(sponsors);

            var reports = new List<Report>
            {
                new() { Title = "Monthly Performance Report - April 2026", Date = new DateTime(2026, 5, 1), Type = ReportType.Performance, Status = ReportStatus.Published, SizeBytes = (long)(2.4 * 1024 * 1024) },
                new() { Title = "Player Attendance Summary Q1 2026", Date = new DateTime(2026, 4, 15), Type = ReportType.Attendance, Status = ReportStatus.Published, SizeBytes = (long)(1.8 * 1024 * 1024) },
                new() { Title = "Tournament Results - Regional Finals", Date = new DateTime(2026, 4, 29), Type = ReportType.Tournament, Status = ReportStatus.Published, SizeBytes = (long)(3.1 * 1024 * 1024) },
                new() { Title = "Training Progress Report", Date = new DateTime(2026, 5, 5), Type = ReportType.Training, Status = ReportStatus.Draft, SizeBytes = (long)(1.5 * 1024 * 1024) },
                new() { Title = "Financial Summary - Q1 2026", Date = new DateTime(2026, 4, 10), Type = ReportType.Financial, Status = ReportStatus.Published, SizeBytes = (long)(2.9 * 1024 * 1024) },
                new() { Title = "Player Development Analysis", Date = new DateTime(2026, 5, 3), Type = ReportType.Analysis, Status = ReportStatus.Published, SizeBytes = (long)(4.2 * 1024 * 1024) },
            };
            context.Reports.AddRange(reports);

            context.UserProfiles.Add(new UserProfile
            {
                FullName = "Admin User",
                Email = "admin@paravolley.com",
                Phone = "+27 13 000 0000",
                Role = "Administrator",
                Bio = "ParaVolley Mpumalanga system administrator.",
                PasswordHash = PasswordHasher.Hash("Admin123!"),
                TwoFactorEnabled = false,
            });

            context.NotificationPreferences.AddRange(new List<NotificationPreference>
            {
                new() { EventKey = "new_player", EventLabel = "New Player Registration", EventDescription = "When a new player is added to the system", EmailEnabled = true, SmsEnabled = false, PushEnabled = true },
                new() { EventKey = "match_results", EventLabel = "Match Results", EventDescription = "When match results are recorded", EmailEnabled = true, SmsEnabled = true, PushEnabled = true },
                new() { EventKey = "event_reminders", EventLabel = "Event Reminders", EventDescription = "Reminders before scheduled events", EmailEnabled = true, SmsEnabled = false, PushEnabled = false },
                new() { EventKey = "low_attendance", EventLabel = "Low Attendance Alert", EventDescription = "When attendance drops below threshold", EmailEnabled = true, SmsEnabled = true, PushEnabled = false },
                new() { EventKey = "report_generated", EventLabel = "Report Generated", EventDescription = "When a report is ready to download", EmailEnabled = false, SmsEnabled = false, PushEnabled = true },
                new() { EventKey = "coach_updates", EventLabel = "Coach Profile Updates", EventDescription = "When coach information is modified", EmailEnabled = true, SmsEnabled = false, PushEnabled = false },
            });

            context.SystemUsers.AddRange(new List<SystemUser>
            {
                new() { Name = "Admin User", Email = "admin@paravolley.com", Role = SystemUserRole.Admin, IsActive = true },
                new() { Name = "Coach Sibusiso", Email = "sibusiso@paravolley.com", Role = SystemUserRole.Coach, IsActive = true },
                new() { Name = "Coach Nomsa", Email = "nomsa@paravolley.com", Role = SystemUserRole.Coach, IsActive = true },
                new() { Name = "Coach Themba", Email = "themba@paravolley.com", Role = SystemUserRole.Coach, IsActive = false },
            });

            context.OrganisationSettings.Add(new OrganisationSettings
            {
                OrganisationName = "ParaVolley Mpumalanga",
                Timezone = "Africa/Johannesburg (SAST, UTC+2)",
                ActiveSeason = "2026",
                MinAttendancePercent = 75,
                Language = "English",
            });

            context.AppearanceSettings.Add(new AppearanceSettings
            {
                Theme = AppTheme.Light,
                AccentColor = "#0B6E4F",
                Density = LayoutDensity.Comfortable,
            });

            context.BackupRecords.AddRange(new List<BackupRecord>
            {
                new() { CreatedAt = new DateTime(2026, 7, 7, 2, 0, 0), SizeBytes = (long)(4.2 * 1024 * 1024), Success = true },
                new() { CreatedAt = new DateTime(2026, 7, 6, 2, 0, 0), SizeBytes = (long)(4.1 * 1024 * 1024), Success = true },
                new() { CreatedAt = new DateTime(2026, 7, 5, 2, 0, 0), SizeBytes = (long)(4.0 * 1024 * 1024), Success = true },
                new() { CreatedAt = new DateTime(2026, 7, 4, 2, 0, 0), SizeBytes = (long)(3.9 * 1024 * 1024), Success = false },
            });

            context.SaveChanges();
        }
    }
}
