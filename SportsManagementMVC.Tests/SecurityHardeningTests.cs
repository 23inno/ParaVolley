using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using Xunit;

namespace SportsManagementMVC.Tests;

public sealed class SecurityHardeningTests
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    private readonly AuthenticationWebApplicationFactory _factory;

    public SecurityHardeningTests(AuthenticationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task WebsiteLogin_DoesNotFollowExternalReturnUrl()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryToken(
            client,
            "/Account/Login?returnUrl=https%3A%2F%2Fattacker.example");

        var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = AuthenticationWebApplicationFactory.AdminEmail,
                ["Password"] = AuthenticationWebApplicationFactory.Password,
                ["ReturnUrl"] = "https://attacker.example"
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/AdminDashboard", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CriticalMvcWrite_WithoutAntiforgeryToken_IsRejected()
    {
        using var client = CreateClient();
        await WebsiteLogin(client);

        var response = await client.PostAsync(
            "/Settings/ToggleAppUserActive?id=999",
            new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_RejectsOversizedSecurityRelevantInput()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register/player",
            new
            {
                name = "Input Limit Test",
                position = new string('P', 101),
                team = "Tests",
                age = 24,
                email = $"limits-{Guid.NewGuid():N}@test.local",
                phone = "+27 82 100 3000",
                disability = "",
                password = AuthenticationWebApplicationFactory.Password,
                role = "Admin",
                isActive = true,
                playerId = 999999
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RepeatedLoginAbuse_EventuallyReturns429()
    {
        using var isolatedFactory = new AuthenticationWebApplicationFactory();
        using var client = isolatedFactory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        HttpStatusCode? finalStatus = null;
        for (var attempt = 0; attempt < 21; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email = AuthenticationWebApplicationFactory.PlayerEmail,
                    password = "definitely-wrong"
                });
            finalStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, finalStatus);
    }

    [Fact]
    public async Task Anonymous_CannotReadPrivateUploadedFiles()
    {
        using var client = CreateClient();
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/uploads/backups/predictable.json")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/uploads/reports/guessed.pdf")).StatusCode);
    }

    [Fact]
    public async Task PlayerProfile_IgnoresInjectedOtherPlayerId()
    {
        int ownPlayerId;
        int otherPlayerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            ownPlayerId = context.Players.Single(item =>
                item.Email == AuthenticationWebApplicationFactory.PlayerEmail).Id;
            var other = new Player
            {
                Name = "Private Other Player",
                Position = "Setter",
                Team = "Other",
                Status = PlayerStatus.Active,
                Age = 25,
                Email = $"private-{Guid.NewGuid():N}@test.local",
                Phone = "+27 82 999 1111",
                Disability = "Private classification"
            };
            context.Players.Add(other);
            context.SaveChanges();
            otherPlayerId = other.Id;
        }

        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);
        var response = await client.GetAsync(
            $"/api/player/me?playerId={otherPlayerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ownPlayerId, body.RootElement.GetProperty("id").GetInt32());
        Assert.DoesNotContain(
            "Private classification",
            await response.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Coach_CannotRevokeAnotherCoachsQrSession()
    {
        int sessionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var owner = new AppUser
            {
                Email = $"owner-{Guid.NewGuid():N}@test.local",
                NormalizedEmail = $"owner-{Guid.NewGuid():N}@test.local",
                PasswordHash = "unused",
                Role = AppUserRole.Coach,
                IsActive = true
            };
            var eventItem = new Event
            {
                Title = "QR ownership test",
                Date = DateTime.Today,
                Time = "10:00",
                Location = "Tests",
                Type = EventType.Practice
            };
            context.AddRange(owner, eventItem);
            context.SaveChanges();

            var session = new QrAttendanceSession
            {
                EventId = eventItem.Id,
                TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                CreatedByAppUserId = owner.Id
            };
            context.QrAttendanceSessions.Add(session);
            context.SaveChanges();
            sessionId = session.Id;
        }

        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.CoachEmail);
        var response = await client.PostAsync(
            $"/api/qr-attendance/sessions/{sessionId}/revoke",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task QrCheckIn_RejectsExpiredRevokedAndDuplicateSessions(
        bool expired,
        bool revoked,
        bool duplicate)
    {
        var scenario = CreateQrScenario(expired, revoked, duplicate);
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);

        var response = await client.PostAsJsonAsync(
            "/api/qr-attendance/check-in",
            new { token = scenario.Token });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task QrCheckIn_IgnoresInjectedPlayerIdAndUsesJwtOwner()
    {
        var scenario = CreateQrScenario(false, false, false);
        int otherPlayerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var other = new Player
            {
                Name = "Other Player",
                Position = "Setter",
                Team = "Tests",
                Status = PlayerStatus.Active,
                Age = 23,
                Email = $"other-{Guid.NewGuid():N}@test.local",
                Phone = "+27 82 999 0000"
            };
            context.Players.Add(other);
            context.SaveChanges();
            otherPlayerId = other.Id;
        }

        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);
        var response = await client.PostAsJsonAsync(
            "/api/qr-attendance/check-in",
            new
            {
                token = scenario.Token,
                playerId = otherPlayerId
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var attendance = verifyContext.Attendances.Single(item =>
            item.EventId == scenario.EventId);
        Assert.Equal(scenario.PlayerId, attendance.PlayerId);
        Assert.NotEqual(otherPlayerId, attendance.PlayerId);
    }

    [Fact]
    public async Task ImageUpload_RejectsSpoofedContentAndOversize()
    {
        var spoofed = FormFile("not an image"u8.ToArray(), "avatar.jpg");
        Assert.False(await UploadSecurity.IsSafeImageAsync(spoofed));

        var oversized = FormFile(
            new byte[(int)UploadSecurity.ImageMaxBytes + 1],
            "avatar.png");
        Assert.False(await UploadSecurity.IsSafeImageAsync(oversized));
    }

    [Fact]
    public async Task ReportUpload_RejectsExecutableExtensionAndRemovesPathTraversal()
    {
        var dangerous = FormFile("<script>alert(1)</script>"u8.ToArray(), "../../report.html");
        Assert.False(await UploadSecurity.IsSafeReportAsync(dangerous));

        var pdf = FormFile("%PDF-1.7 test"u8.ToArray(), "../../quarterly.pdf");
        Assert.True(await UploadSecurity.IsSafeReportAsync(pdf));
        Assert.DoesNotContain("..", UploadSecurity.GeneratedFileName(pdf));
        Assert.Equal(
            "quarterly.pdf",
            UploadSecurity.SafeOriginalFileName(pdf.FileName));
    }

    private HttpClient CreateClient() => _factory.CreateClient(new()
    {
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> CreateAuthenticatedApiClient(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
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

    private static async Task WebsiteLogin(HttpClient client)
    {
        var token = await GetAntiforgeryToken(client, "/Account/Login");
        var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = AuthenticationWebApplicationFactory.AdminEmail,
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

    private static FormFile FormFile(byte[] content, string fileName)
    {
        return new FormFile(
            new MemoryStream(content),
            0,
            content.Length,
            "file",
            fileName);
    }

    private QrScenario CreateQrScenario(
        bool expired,
        bool revoked,
        bool duplicate)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var player = context.Players.Single(item =>
            item.Email == AuthenticationWebApplicationFactory.PlayerEmail);
        var eventItem = new Event
        {
            Title = $"QR check-in {Guid.NewGuid():N}",
            Date = DateTime.Today,
            Time = "11:00",
            Location = "Tests",
            Type = EventType.Practice,
            Status = EventStatus.Upcoming
        };
        context.Events.Add(eventItem);
        context.SaveChanges();
        context.EventRegistrations.Add(new EventRegistration
        {
            PlayerId = player.Id,
            EventId = eventItem.Id,
            Status = EventRegistrationStatus.Registered,
            RegisteredAtUtc = DateTime.UtcNow
        });
        context.QrAttendanceSessions.Add(new QrAttendanceSession
        {
            EventId = eventItem.Id,
            TokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAtUtc = expired
                ? DateTime.UtcNow.AddSeconds(-1)
                : DateTime.UtcNow.AddMinutes(10),
            IsRevoked = revoked
        });
        if (duplicate)
        {
            context.Attendances.Add(new Attendance
            {
                PlayerId = player.Id,
                EventId = eventItem.Id,
                Date = eventItem.Date,
                Status = AttendanceStatus.Present
            });
        }

        context.SaveChanges();
        return new QrScenario(token, eventItem.Id, player.Id);
    }

    private sealed record QrScenario(string Token, int EventId, int PlayerId);
}
