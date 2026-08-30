using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using Xunit;

namespace SportsManagementMVC.Tests;

public sealed class RealCalendarImportTests
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    private readonly AuthenticationWebApplicationFactory _factory;

    public RealCalendarImportTests(AuthenticationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ProductionDefault_DoesNotSeedDemoBusinessData()
    {
        using var database = new TestDatabase();
        var configuration = Configuration(new Dictionary<string, string?>());

        DbInitializer.Seed(database.Context, configuration);

        Assert.Empty(database.Context.Players);
        Assert.Empty(database.Context.Coaches);
        Assert.Empty(database.Context.Events);
        Assert.Empty(database.Context.Matches);
        Assert.Empty(database.Context.Attendances);
        Assert.Empty(database.Context.Announcements);
        Assert.Empty(database.Context.Sponsors);
    }

    [Fact]
    public void DevelopmentFlag_SeedsDemoDataForLocalDevelopmentOnly()
    {
        using var database = new TestDatabase();
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["SeedData:EnableDemoData"] = "true"
        });

        DbInitializer.Seed(database.Context, configuration);

        Assert.NotEmpty(database.Context.Players);
        Assert.NotEmpty(database.Context.Coaches);
        Assert.NotEmpty(database.Context.Events);
        Assert.NotEmpty(database.Context.Matches);
        Assert.NotEmpty(database.Context.Attendances);
    }

    [Fact]
    public async Task DryRun_ParsesAllRowsWithoutMutationOrInventedDates()
    {
        using var database = new TestDatabase();
        var importer = new RealCalendarImportService(database.Context);

        var result = await importer.DryRunAsync();

        Assert.True(result.IsDryRun);
        Assert.Equal(19, result.Items.Count);
        Assert.Equal(8, result.Created);
        Assert.Equal(11, result.RequiresConfirmation);
        Assert.Equal(0, result.RejectedInvalid);
        Assert.Empty(database.Context.Events);
        Assert.Equal(4, result.Items.Count(item =>
            item.Source.StartDate == null &&
            item.Source.SourceDate.Contains("exact date not supplied")));
        Assert.Equal(4, result.Items.Count(item =>
            item.Source.StartDate == null &&
            item.Source.SourceDate == "TBD"));
    }

    [Fact]
    public async Task ExactDateImport_IsTransactionalAndMapsVerifiedRows()
    {
        using var database = new TestDatabase();
        var importer = new RealCalendarImportService(database.Context);

        var result = await importer.ImportAsync(backupConfirmed: true);

        Assert.False(result.IsDryRun);
        Assert.Equal(8, result.Created);
        Assert.Equal(8, await database.Context.Events.CountAsync());
        var championship = await database.Context.Events.SingleAsync(item =>
            item.Title == "South Africa Sitting Volleyball Championships");
        Assert.Equal(new DateTime(2026, 10, 3), championship.Date);
        Assert.Equal("Middleburg", championship.Location);
        Assert.Equal(EventType.Tournament, championship.Type);
        Assert.Equal("Not supplied", championship.Time);
        Assert.Equal(EventStatus.Upcoming, championship.Status);
    }

    [Fact]
    public async Task DuplicateImport_SkipsWithoutOverwritingExistingRecord()
    {
        using var database = new TestDatabase();
        var importer = new RealCalendarImportService(database.Context);
        await importer.ImportAsync(backupConfirmed: true);
        var existing = await database.Context.Events.SingleAsync(item =>
            item.Title == "South Africa Sitting Volleyball Championships");
        existing.Description = "Locally reviewed description that must remain.";
        await database.Context.SaveChangesAsync();

        var second = await importer.ImportAsync(backupConfirmed: true);

        Assert.Equal(0, second.Created);
        Assert.Equal(8, second.SkippedDuplicate);
        Assert.Equal(8, await database.Context.Events.CountAsync());
        Assert.Equal(
            "Locally reviewed description that must remain.",
            (await database.Context.Events.SingleAsync(item =>
                item.Id == existing.Id)).Description);
    }

    [Fact]
    public async Task InvalidAndTbdRows_AreNotInserted()
    {
        using var database = new TestDatabase();
        var importer = new RealCalendarImportService(database.Context);
        var entries = new[]
        {
            new RealCalendarSourceEntry(
                "1 January 2026",
                "",
                EventType.Practice,
                new DateTime(2026, 1, 1),
                "Verified venue",
                "Invalid because the title is blank."),
            new RealCalendarSourceEntry(
                "TBD",
                "Awaiting confirmation",
                EventType.Practice,
                null,
                null,
                "No date was supplied.")
        };

        var result = await importer.ImportAsync(entries, true);

        Assert.Equal(1, result.RejectedInvalid);
        Assert.Equal(1, result.RequiresConfirmation);
        Assert.Empty(database.Context.Events);
    }

    [Fact]
    public async Task ImportFailure_RollsBackAllCreatedEvents()
    {
        using var database = new TestDatabase();
        await database.Context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER fail_calendar_import BEFORE INSERT ON Events " +
            "WHEN NEW.Title = 'Force rollback' BEGIN " +
            "SELECT RAISE(ABORT, 'test rollback'); END;");
        var importer = new RealCalendarImportService(database.Context);
        var entries = new[]
        {
            ValidEntry("First valid row", new DateTime(2026, 9, 1)),
            ValidEntry("Force rollback", new DateTime(2026, 9, 2))
        };

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            importer.ImportAsync(entries, true));

        database.Context.ChangeTracker.Clear();
        Assert.Empty(await database.Context.Events.ToListAsync());
    }

    [Fact]
    public void EventTypeMappings_UseOnlyExistingLegitimateTypes()
    {
        Assert.All(RealCalendarImportService.OfficialCalendar, item =>
            Assert.True(Enum.IsDefined(item.Type)));
        Assert.Equal(EventType.Workshop,
            RealCalendarImportService.OfficialCalendar.Single(item =>
                item.Title == "Coaching Clinics and Referee Courses").Type);
        Assert.Equal(EventType.Tournament,
            RealCalendarImportService.OfficialCalendar.Single(item =>
                item.Title == "Provincial Championships").Type);
        Assert.Equal(EventType.Practice,
            RealCalendarImportService.OfficialCalendar.Single(item =>
                item.Title == "District Trials").Type);
    }

    [Fact]
    public async Task Coach_CanEditImportedPractice_ButNotImportedTournament()
    {
        await EnsureOfficialCalendarImported();
        using var client = CreateClient();
        await WebsiteLogin(client,
            AuthenticationWebApplicationFactory.CoachEmail,
            AuthenticationWebApplicationFactory.Password);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var practiceId = await context.Events.Where(item =>
                item.Title == "Women's National Team Training Camp")
            .Select(item => item.Id).SingleAsync();
        var tournamentId = await context.Events.Where(item =>
                item.Title == "South Africa Sitting Volleyball Championships")
            .Select(item => item.Id).SingleAsync();

        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"/Events/Edit/{practiceId}")).StatusCode);
        var forbidden = await client.GetAsync($"/Events/Edit/{tournamentId}");
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
        Assert.Equal("/Account/AccessDenied", forbidden.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AndroidEventsApi_RemainsACompatibleJsonArray()
    {
        await EnsureOfficialCalendarImported();
        using var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthenticationWebApplicationFactory.PlayerEmail,
            password = AuthenticationWebApplicationFactory.Password
        });
        login.EnsureSuccessStatusCode();
        using var loginJson = JsonDocument.Parse(
            await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginJson.RootElement.GetProperty("token").GetString());

        var response = await client.GetAsync("/api/events?page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        var item = json.RootElement.EnumerateArray().First();
        foreach (var property in new[]
        {
            "id", "title", "date", "time", "location", "type",
            "participants", "status", "description"
        })
        {
            Assert.True(item.TryGetProperty(property, out _), property);
        }
    }

    private async Task EnsureOfficialCalendarImported()
    {
        using var scope = _factory.Services.CreateScope();
        var importer = scope.ServiceProvider
            .GetRequiredService<RealCalendarImportService>();
        await importer.ImportAsync(backupConfirmed: true);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new()
    {
        AllowAutoRedirect = false
    });

    private static async Task WebsiteLogin(
        HttpClient client,
        string email,
        string password)
    {
        var html = await client.GetStringAsync("/Account/Login");
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] =
                    WebUtility.HtmlDecode(match.Groups[1].Value),
                ["Email"] = email,
                ["Password"] = password,
                ["RememberMe"] = "false"
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static IConfiguration Configuration(
        Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static RealCalendarSourceEntry ValidEntry(
        string title,
        DateTime date) => new(
            date.ToString("d MMMM yyyy"),
            title,
            EventType.Practice,
            date,
            "Verified test venue",
            "Verified test description.");

    private sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public ApplicationDbContext Context { get; }

        public TestDatabase()
        {
            _connection.Open();
            Context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(_connection)
                    .Options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
