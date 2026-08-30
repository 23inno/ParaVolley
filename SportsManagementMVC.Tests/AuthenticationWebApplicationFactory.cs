using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Tests;

public sealed class AuthenticationWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin.tests@paravolley.test";
    public const string CoachEmail = "coach.tests@paravolley.test";
    public const string PlayerEmail = "player.tests@paravolley.test";
    public const string PendingEmail = "pending.tests@paravolley.test";
    public const string Password = "ValidPassword!42";

    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"paravolley-tests-{Guid.NewGuid():N}.db");

    private string ConnectionString =>
        $"Data Source={_databasePath};Cache=Shared;Default Timeout=15";

    public AuthenticationWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=unused;Database=unused;Username=unused;Password=unused");
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "integration-test-signing-key-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ParaVolley.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ParaVolley.Tests");
        Environment.SetEnvironmentVariable("SkipDatabaseInitialization", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=unused;Database=unused;Username=unused;Password=unused",
                ["Jwt:Key"] =
                    "integration-test-signing-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "ParaVolley.Tests",
                ["Jwt:Audience"] = "ParaVolley.Tests",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["AllowedHosts"] = "localhost",
                ["SkipDatabaseInitialization"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddLogging(logging => logging.ClearProviders());
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(ConnectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<AppUser>>();

        context.Database.EnsureCreated();
        Seed(context, hasher);

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in new[]
            {
                _databasePath,
                $"{_databasePath}-wal",
                $"{_databasePath}-shm"
            })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    private static void Seed(
        ApplicationDbContext context,
        IPasswordHasher<AppUser> hasher)
    {
        var activePlayer = new Player
        {
            Name = "Active Test Player",
            Position = "Setter",
            Team = "Tests",
            Status = PlayerStatus.Active,
            Age = 25,
            Email = PlayerEmail,
            Phone = "+27 82 000 0001"
        };
        var pendingPlayer = new Player
        {
            Name = "Pending Test Player",
            Position = "Setter",
            Team = "Tests",
            Status = PlayerStatus.Inactive,
            Age = 25,
            Email = PendingEmail,
            Phone = "+27 82 000 0002"
        };

        context.Players.AddRange(activePlayer, pendingPlayer);
        context.SaveChanges();

        AddUser(context, hasher, AdminEmail, AppUserRole.Admin, true, null);
        AddUser(context, hasher, CoachEmail, AppUserRole.Coach, true, null);
        AddUser(
            context,
            hasher,
            PlayerEmail,
            AppUserRole.Player,
            true,
            activePlayer.Id);
        AddUser(
            context,
            hasher,
            PendingEmail,
            AppUserRole.Player,
            false,
            pendingPlayer.Id);

        context.UserProfiles.Add(new UserProfile
        {
            FullName = "Legacy Display Profile",
            Email = AdminEmail,
            PasswordHash = "not-an-authoritative-password-hash"
        });
        context.SaveChanges();
    }

    private static void AddUser(
        ApplicationDbContext context,
        IPasswordHasher<AppUser> hasher,
        string email,
        AppUserRole role,
        bool isActive,
        int? playerId)
    {
        var user = new AppUser
        {
            Email = email,
            NormalizedEmail = email,
            Role = role,
            IsActive = isActive,
            PlayerId = playerId
        };
        user.PasswordHash = hasher.HashPassword(user, Password);
        context.AppUsers.Add(user);
        context.SaveChanges();
    }
}
