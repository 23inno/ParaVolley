using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options
        ) : base(options)
        {
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Coach> Coaches => Set<Coach>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<EventRegistration> EventRegistrations =>
            Set<EventRegistration>();
        public DbSet<QrAttendanceSession> QrAttendanceSessions =>
            Set<QrAttendanceSession>();
        public DbSet<Sponsor> Sponsors => Set<Sponsor>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<NotificationPreference> NotificationPreferences =>
            Set<NotificationPreference>();
        public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
        public DbSet<OrganisationSettings> OrganisationSettings =>
            Set<OrganisationSettings>();
        public DbSet<AppearanceSettings> AppearanceSettings =>
            Set<AppearanceSettings>();
        public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
        public DbSet<Subscriber> Subscribers => Set<Subscriber>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasOne(user => user.Player)
                .WithOne()
                .HasForeignKey<AppUser>(user => user.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(registration => new
                {
                    registration.PlayerId,
                    registration.EventId
                })
                .IsUnique();

            modelBuilder.Entity<EventRegistration>()
                .HasOne(registration => registration.Player)
                .WithMany(player => player.EventRegistrations)
                .HasForeignKey(registration => registration.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(registration => registration.Event)
                .WithMany(eventItem => eventItem.Registrations)
                .HasForeignKey(registration => registration.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .Property(registration => registration.RegisteredAtUtc)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Attendance>()
                .HasIndex(attendance => new
                {
                    attendance.PlayerId,
                    attendance.EventId
                })
                .IsUnique();

            modelBuilder.Entity<QrAttendanceSession>()
                .HasIndex(session => session.TokenHash)
                .IsUnique();

            modelBuilder.Entity<QrAttendanceSession>()
                .HasOne(session => session.Event)
                .WithMany()
                .HasForeignKey(session => session.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QrAttendanceSession>()
                .HasOne(session => session.CreatedByAppUser)
                .WithMany()
                .HasForeignKey(session => session.CreatedByAppUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<QrAttendanceSession>()
                .Property(session => session.CreatedAtUtc)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<QrAttendanceSession>()
                .Property(session => session.ExpiresAtUtc)
                .HasColumnType("timestamp with time zone");

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