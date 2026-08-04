using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Coach> Coaches => Set<Coach>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Sponsor> Sponsors => Set<Sponsor>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
        public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
        public DbSet<OrganisationSettings> OrganisationSettings => Set<OrganisationSettings>();
        public DbSet<AppearanceSettings> AppearanceSettings => Set<AppearanceSettings>();
        public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
        public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    }
}
