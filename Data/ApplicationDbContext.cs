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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Announcement>()
                .Property(x => x.Date)
                .HasColumnType("date");

            modelBuilder.Entity<Attendance>()
                .Property(x => x.Date)
                .HasColumnType("date");

            modelBuilder.Entity<Event>()
                .Property(x => x.Date)
                .HasColumnType("date");

            modelBuilder.Entity<Match>()
                .Property(x => x.Date)
                .HasColumnType("date");

            modelBuilder.Entity<Report>()
                .Property(x => x.Date)
                .HasColumnType("date");

            modelBuilder.Entity<UserProfile>()
                .Property(x => x.ResetTokenExpiry)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<BackupRecord>()
                .Property(x => x.CreatedAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<Subscriber>()
                .Property(x => x.SubscribedAt)
                .HasColumnType("timestamp without time zone");
        }
    }
}