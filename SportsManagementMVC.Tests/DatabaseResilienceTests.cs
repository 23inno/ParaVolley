using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using Xunit;
using Xunit.Abstractions;

namespace SportsManagementMVC.Tests;

public sealed class DatabaseResilienceTests
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    private readonly AuthenticationWebApplicationFactory _factory;

    public DatabaseResilienceTests(AuthenticationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoints_ExposeOnlyMinimalStatus()
    {
        using var client = CreateClient();

        foreach (var path in new[] { "/health/live", "/health/ready" })
        {
            var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"status\":\"Healthy\"}", body);
            Assert.DoesNotContain("Data Source", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ApiListPaging_PreservesArrayContract_AndCapsPageSize()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Announcements.AddRange(Enumerable.Range(1, 205).Select(index =>
                new Announcement
                {
                    Title = $"Paged announcement {index}",
                    Excerpt = "Paging test",
                    Content = "Paging test",
                    Author = "Tests",
                    Date = DateTime.Today.AddDays(-index),
                    Category = AnnouncementCategory.News
                }));
            await context.SaveChangesAsync();
        }

        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);
        var first = await client.GetFromJsonAsync<JsonElement>(
            "/api/announcements?page=1&pageSize=999");
        var second = await client.GetFromJsonAsync<JsonElement>(
            "/api/announcements?page=2&pageSize=200");

        Assert.Equal(JsonValueKind.Array, first.ValueKind);
        Assert.Equal(200, first.GetArrayLength());
        Assert.True(second.GetArrayLength() >= 5);
    }

    [Fact]
    public async Task SimultaneousDuplicatePlayerRegistration_CreatesOneAccount()
    {
        var email = $"concurrent-{Guid.NewGuid():N}@test.local";
        using var client = CreateClient();
        var payload = new
        {
            name = "Concurrent Registration",
            position = "Setter",
            team = "Tests",
            age = 25,
            email,
            phone = "+27 82 123 4567",
            disability = "",
            password = AuthenticationWebApplicationFactory.Password
        };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/register/player", payload),
            client.PostAsJsonAsync("/api/auth/register/player", payload));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await context.AppUsers.CountAsync(user =>
            user.NormalizedEmail == email));
        Assert.Equal(1, await context.Players.CountAsync(player =>
            player.Email == email));
    }

    [Fact]
    public async Task SimultaneousDuplicateEventRegistration_CreatesOneRecord()
    {
        var scenario = await CreateEventScenario();
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);

        var responses = await Task.WhenAll(
            client.PostAsync($"/api/events/{scenario.EventId}/register", null),
            client.PostAsync($"/api/events/{scenario.EventId}/register", null));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertSingleEventRecord(scenario, registrations: true);
    }

    [Fact]
    public async Task SimultaneousDuplicateAttendance_CreatesOneRecord()
    {
        var scenario = await CreateEventScenario();
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.CoachEmail);
        var payload = new
        {
            playerId = scenario.PlayerId,
            eventId = scenario.EventId,
            status = "Present"
        };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/attendance", payload),
            client.PostAsJsonAsync("/api/attendance", payload));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertSingleEventRecord(scenario, registrations: false);
    }

    [Fact]
    public async Task SimultaneousDuplicateQrCheckIn_CreatesOneAttendance()
    {
        var scenario = await CreateEventScenario(addRegistration: true, addQrSession: true);
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);
        var payload = new { token = scenario.Token };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/qr-attendance/check-in", payload),
            client.PostAsJsonAsync("/api/qr-attendance/check-in", payload));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertSingleEventRecord(scenario, registrations: false);
    }

    [Fact]
    public async Task SimultaneousDuplicateCoachProvisioning_CreatesOneAccount()
    {
        var email = $"coach-concurrent-{Guid.NewGuid():N}@test.local";
        using var first = CreateClient();
        using var second = CreateClient();
        await WebsiteLogin(first);
        await WebsiteLogin(second);
        var firstToken = await GetAntiforgeryToken(first, "/Settings/RolesUsers");
        var secondToken = await GetAntiforgeryToken(second, "/Settings/RolesUsers");

        Task<HttpResponseMessage> Provision(HttpClient client, string token) =>
            client.PostAsync("/Settings/ProvisionCoach", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["email"] = email,
                    ["password"] = AuthenticationWebApplicationFactory.Password
                }));

        var responses = await Task.WhenAll(
            Provision(first, firstToken),
            Provision(second, secondToken));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await context.AppUsers.CountAsync(user =>
            user.NormalizedEmail == email && user.Role == AppUserRole.Coach));
    }

    [Fact]
    public async Task AdminReportAndDashboards_RenderWithServerAggregates()
    {
        using var admin = CreateClient();
        await WebsiteLogin(admin);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.GetAsync("/Home/AdminDashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.GetAsync("/Reports")).StatusCode);

        using var coach = CreateClient();
        await WebsiteLogin(
            coach,
            AuthenticationWebApplicationFactory.CoachEmail);
        Assert.Equal(HttpStatusCode.OK,
            (await coach.GetAsync("/Home/CoachDashboard")).StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new()
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> CreateAuthenticatedApiClient(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = AuthenticationWebApplicationFactory.Password
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("token").GetString());
        return client;
    }

    private async Task<EventScenario> CreateEventScenario(
        bool addRegistration = false,
        bool addQrSession = false)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var playerId = await context.Players
            .Where(player => player.Email == AuthenticationWebApplicationFactory.PlayerEmail)
            .Select(player => player.Id)
            .SingleAsync();
        var eventItem = new Event
        {
            Title = $"Concurrency {Guid.NewGuid():N}",
            Date = DateTime.Today,
            Time = "12:00",
            Location = "Tests",
            Type = EventType.Practice,
            Status = EventStatus.Upcoming
        };
        context.Events.Add(eventItem);
        await context.SaveChangesAsync();

        if (addRegistration)
        {
            context.EventRegistrations.Add(new EventRegistration
            {
                PlayerId = playerId,
                EventId = eventItem.Id,
                Status = EventRegistrationStatus.Registered,
                RegisteredAtUtc = DateTime.UtcNow
            });
        }

        if (addQrSession)
        {
            context.QrAttendanceSessions.Add(new QrAttendanceSession
            {
                EventId = eventItem.Id,
                TokenHash = Convert.ToHexString(SHA256.HashData(
                    Encoding.UTF8.GetBytes(token))),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
            });
        }

        await context.SaveChangesAsync();
        return new EventScenario(playerId, eventItem.Id, token);
    }

    private async Task AssertSingleEventRecord(
        EventScenario scenario,
        bool registrations)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var count = registrations
            ? await context.EventRegistrations.CountAsync(item =>
                item.PlayerId == scenario.PlayerId && item.EventId == scenario.EventId)
            : await context.Attendances.CountAsync(item =>
                item.PlayerId == scenario.PlayerId && item.EventId == scenario.EventId);
        Assert.Equal(1, count);
    }

    private static async Task WebsiteLogin(
        HttpClient client,
        string email = AuthenticationWebApplicationFactory.AdminEmail)
    {
        var token = await GetAntiforgeryToken(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = email,
                ["Password"] = AuthenticationWebApplicationFactory.Password
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryToken(
        HttpClient client,
        string path)
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The antiforgery token was not rendered.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private sealed record EventScenario(int PlayerId, int EventId, string Token);
}

public sealed class LocalLoadProbeTests
{
    private readonly ITestOutputHelper _output;

    public LocalLoadProbeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "LoadProbe")]
    public async Task ControlledLocalConcurrencyProbe_HasNoServerFailures()
    {
        using var factory = new AuthenticationWebApplicationFactory();
        using var publicClient = factory.CreateClient(new() { AllowAutoRedirect = false });
        using var playerClient = factory.CreateClient(new() { AllowAutoRedirect = false });
        var login = await playerClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = AuthenticationWebApplicationFactory.PlayerEmail,
            password = AuthenticationWebApplicationFactory.Password
        });
        login.EnsureSuccessStatusCode();
        using (var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync()))
        {
            playerClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    body.RootElement.GetProperty("token").GetString());
        }

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
        var publicResult = await RunScenario(
            "public-20",
            publicClient,
            20,
            4,
            ["/", "/Events", "/Matches", "/Announcements"]);
        var authenticatedResult = await RunScenario(
            "authenticated-20",
            playerClient,
            20,
            4,
            ["/api/player/me", "/api/player/dashboard", "/api/events", "/api/player/attendance"]);
        var burstResult = await RunScenario(
            "public-burst-50",
            publicClient,
            50,
            1,
            ["/"]);
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        foreach (var result in new[] { publicResult, authenticatedResult, burstResult })
        {
            _output.WriteLine(result.ToString());
            Assert.Equal(0, result.ServerErrors);
            Assert.Equal(0, result.OtherFailures);
        }
        _output.WriteLine($"managed-memory-delta-bytes={memoryAfter - memoryBefore}");

        var loginResponses = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ =>
            publicClient.PostAsJsonAsync("/api/auth/login", new
            {
                email = AuthenticationWebApplicationFactory.PlayerEmail,
                password = "invalid-password"
            })));
        var throttled = loginResponses.Count(response =>
            response.StatusCode == HttpStatusCode.TooManyRequests);
        _output.WriteLine($"login-probe total=25 throttled={throttled}");
        Assert.True(throttled > 0);
        Assert.Equal(HttpStatusCode.OK,
            (await publicClient.GetAsync("/health/ready")).StatusCode);
    }

    private static async Task<LoadResult> RunScenario(
        string name,
        HttpClient client,
        int users,
        int requestsPerUser,
        string[] paths)
    {
        var samples = new ConcurrentBag<(HttpStatusCode Status, double Milliseconds)>();
        var overall = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, users).Select(user => Task.Run(async () =>
        {
            for (var request = 0; request < requestsPerUser; request++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await client.GetAsync(paths[(user + request) % paths.Length]);
                stopwatch.Stop();
                samples.Add((response.StatusCode, stopwatch.Elapsed.TotalMilliseconds));
            }
        }));
        await Task.WhenAll(tasks);
        overall.Stop();

        var ordered = samples.Select(sample => sample.Milliseconds).OrderBy(value => value).ToArray();
        double Percentile(double percentile) =>
            ordered[Math.Clamp((int)Math.Ceiling(ordered.Length * percentile) - 1, 0, ordered.Length - 1)];
        return new LoadResult(
            name,
            samples.Count,
            samples.Count(sample => (int)sample.Status is >= 200 and < 400),
            samples.Count(sample => sample.Status == HttpStatusCode.TooManyRequests),
            samples.Count(sample => (int)sample.Status >= 500),
            samples.Count(sample => (int)sample.Status is >= 400 and < 500 &&
                sample.Status != HttpStatusCode.TooManyRequests),
            ordered.Average(),
            Percentile(0.5),
            Percentile(0.95),
            ordered[^1],
            samples.Count / overall.Elapsed.TotalSeconds);
    }

    private sealed record LoadResult(
        string Name,
        int Total,
        int Successful,
        int Throttled,
        int ServerErrors,
        int OtherFailures,
        double AverageMs,
        double MedianMs,
        double P95Ms,
        double MaximumMs,
        double RequestsPerSecond)
    {
        public override string ToString() =>
            $"{Name} total={Total} success={Successful} 429={Throttled} " +
            $"5xx={ServerErrors} other-failures={OtherFailures} " +
            $"avg-ms={AverageMs:F2} median-ms={MedianMs:F2} p95-ms={P95Ms:F2} " +
            $"max-ms={MaximumMs:F2} rps={RequestsPerSecond:F2}";
    }
}
