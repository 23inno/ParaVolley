using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using Xunit;

namespace SportsManagementMVC.Tests;

public sealed class AuthenticationAuthorizationTests
    : IClassFixture<AuthenticationWebApplicationFactory>
{
    private readonly AuthenticationWebApplicationFactory _factory;

    public AuthenticationAuthorizationTests(
        AuthenticationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Coach")]
    public async Task PublicRegistration_IgnoresInjectedRole_AndCreatesPlayer(
        string injectedRole)
    {
        var email = $"role-{injectedRole.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register/player",
            new
            {
                name = "Role Safety Test",
                position = "Setter",
                team = "Tests",
                age = 24,
                email,
                phone = "+27 82 100 1000",
                disability = "",
                password = AuthenticationWebApplicationFactory.Password,
                role = injectedRole
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var user = context.AppUsers.Single(item =>
            item.NormalizedEmail == email);

        Assert.Equal(AppUserRole.Player, user.Role);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task PendingPlayer_CannotLogIn()
    {
        using var client = CreateClient();
        var response = await ApiLogin(
            client,
            AuthenticationWebApplicationFactory.PendingEmail,
            AuthenticationWebApplicationFactory.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AndroidCompatiblePlayerLogin_Succeeds_WithEmailPasswordPayload()
    {
        using var client = CreateClient();
        var response = await ApiLogin(
            client,
            AuthenticationWebApplicationFactory.PlayerEmail,
            AuthenticationWebApplicationFactory.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Player", body.RootElement
            .GetProperty("user")
            .GetProperty("role")
            .GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task WrongPassword_Fails()
    {
        using var client = CreateClient();
        var response = await ApiLogin(
            client,
            AuthenticationWebApplicationFactory.PlayerEmail,
            "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WebsiteLogin_UsesAppUser_AndServerDerivedAdminRole()
    {
        using var client = CreateClient();
        var login = await WebsiteLogin(
            client,
            AuthenticationWebApplicationFactory.AdminEmail,
            AuthenticationWebApplicationFactory.Password);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var settings = await client.GetAsync("/Settings/RolesUsers");
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
    }

    [Fact]
    public async Task WebsitePlayer_IsRedirectedToSafeDestination_AndCannotOpenSettings()
    {
        using var client = CreateClient();
        var login = await WebsiteLogin(
            client,
            AuthenticationWebApplicationFactory.PlayerEmail,
            AuthenticationWebApplicationFactory.Password);

        Assert.Equal("/Account/PlayerAccess", login.Headers.Location?.OriginalString);

        var settings = await client.GetAsync("/Settings/RolesUsers");
        Assert.Equal(HttpStatusCode.Redirect, settings.StatusCode);
        Assert.Equal(
            "/Account/AccessDenied",
            settings.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Anonymous_CannotAccessAdminEndpoint()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(
            "/api/admin/player-registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Player_CannotAccessAdminOrCoachManagementEndpoints()
    {
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.PlayerEmail);

        var adminResponse = await client.GetAsync(
            "/api/admin/player-registrations");
        var coachResponse = await client.PostAsJsonAsync(
            "/api/events",
            new { title = "Forbidden" });

        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, coachResponse.StatusCode);
    }

    [Fact]
    public async Task CoachLogin_RemainsCompatible_ButCoachCannotAccessAdminOperations()
    {
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.CoachEmail);
        var response = await client.GetAsync(
            "/api/admin/player-registrations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var websiteClient = CreateClient();
        await WebsiteLogin(
            websiteClient,
            AuthenticationWebApplicationFactory.CoachEmail,
            AuthenticationWebApplicationFactory.Password);
        var settings = await websiteClient.GetAsync("/Settings/RolesUsers");
        Assert.Equal(HttpStatusCode.Redirect, settings.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAccessAdminOperation()
    {
        using var client = await CreateAuthenticatedApiClient(
            AuthenticationWebApplicationFactory.AdminEmail);
        var response = await client.GetAsync(
            "/api/admin/player-registrations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminProvisioning_AlwaysCreatesCoach_ServerSide()
    {
        var email = $"provisioned-{Guid.NewGuid():N}@test.local";
        using var client = CreateClient();
        await WebsiteLogin(
            client,
            AuthenticationWebApplicationFactory.AdminEmail,
            AuthenticationWebApplicationFactory.Password);

        var token = await GetAntiforgeryToken(client, "/Settings/RolesUsers");
        var response = await client.PostAsync(
            "/Settings/ProvisionCoach",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["email"] = email,
                ["password"] = AuthenticationWebApplicationFactory.Password,
                ["role"] = "Admin"
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var user = context.AppUsers.Single(item =>
            item.NormalizedEmail == email);
        Assert.Equal(AppUserRole.Coach, user.Role);
    }

    [Fact]
    public async Task CaseVariantDuplicateEmail_IsRejected()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register/player",
            new
            {
                name = "Duplicate Test",
                position = "Setter",
                team = "Tests",
                age = 24,
                email = AuthenticationWebApplicationFactory.PlayerEmail.ToUpperInvariant(),
                phone = "+27 82 100 2000",
                disability = "",
                password = AuthenticationWebApplicationFactory.Password
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DisabledAccount_JwtStopsWorkingImmediately()
    {
        var email = $"disabled-{Guid.NewGuid():N}@test.local";
        AddUser(email, AppUserRole.Admin);

        using var client = await CreateAuthenticatedApiClient(email);
        SetAccount(email, isActive: false, role: AppUserRole.Admin);

        var response = await client.GetAsync(
            "/api/admin/player-registrations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangedRole_InvalidatesPreviouslyIssuedJwt()
    {
        var email = $"role-change-{Guid.NewGuid():N}@test.local";
        AddUser(email, AppUserRole.Coach);

        using var client = await CreateAuthenticatedApiClient(email);
        SetAccount(email, isActive: true, role: AppUserRole.Player);

        var response = await client.PostAsJsonAsync(
            "/api/events",
            new { title = "Stale role" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Events")]
    [InlineData("/Announcements")]
    [InlineData("/Matches")]
    [InlineData("/Sponsors")]
    public async Task Anonymous_CanAccessPublicWebsite(string path)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Players")]
    [InlineData("/Attendance")]
    [InlineData("/Settings/Profile")]
    public async Task Anonymous_CannotAccessManagementWebsite(string path)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Coach_HasCoachDashboardTrainingAndAttendance_ButNotAdminOperations()
    {
        using var client = CreateClient();
        var login = await WebsiteLogin(client,
            AuthenticationWebApplicationFactory.CoachEmail,
            AuthenticationWebApplicationFactory.Password);
        Assert.Equal("/Home/CoachDashboard", login.Headers.Location?.OriginalString);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Home/CoachDashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Events/Create")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Attendance/Create")).StatusCode);

        foreach (var path in new[] { "/Settings/Profile", "/Settings/RolesUsers", "/Coaches", "/Reports", "/Players/Delete/1" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/AccessDenied", response.Headers.Location?.AbsolutePath);
        }
    }

    [Fact]
    public async Task Admin_RetainsManagementDashboardAndFunctions()
    {
        using var client = CreateClient();
        var login = await WebsiteLogin(client,
            AuthenticationWebApplicationFactory.AdminEmail,
            AuthenticationWebApplicationFactory.Password);
        Assert.Equal("/Home/AdminDashboard", login.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Home/AdminDashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Players/Create")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Settings/Profile")).StatusCode);
    }

    [Fact]
    public async Task CoachPlayerViews_DoNotExposePrivatePlayerFields()
    {
        using var client = CreateClient();
        await WebsiteLogin(client,
            AuthenticationWebApplicationFactory.CoachEmail,
            AuthenticationWebApplicationFactory.Password);

        var html = await client.GetStringAsync("/Players");
        Assert.DoesNotContain(AuthenticationWebApplicationFactory.PlayerEmail, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("+27 82 000 0001", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Disability Classification", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicPages_DoNotExposePrivateManagementData()
    {
        using var client = CreateClient();
        var pages = new List<string>();
        foreach (var path in new[] { "/", "/Events", "/Announcements" })
        {
            pages.Add(await client.GetStringAsync(path));
        }
        var html = string.Join('\n', pages);

        Assert.DoesNotContain(AuthenticationWebApplicationFactory.PlayerEmail, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Attendance Management", html, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<HttpClient> CreateAuthenticatedApiClient(string email)
    {
        var client = CreateClient();
        var response = await ApiLogin(
            client,
            email,
            AuthenticationWebApplicationFactory.Password);
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                body.RootElement.GetProperty("token").GetString());
        return client;
    }

    private static Task<HttpResponseMessage> ApiLogin(
        HttpClient client,
        string email,
        string password)
    {
        return client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });
    }

    private static async Task<HttpResponseMessage> WebsiteLogin(
        HttpClient client,
        string email,
        string password)
    {
        var token = await GetAntiforgeryToken(client, "/Account/Login");
        return await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = email,
                ["Password"] = password,
                ["RememberMe"] = "false"
            }));
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

    private void AddUser(string email, AppUserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<AppUser>>();
        var user = new AppUser
        {
            Email = email,
            NormalizedEmail = email,
            Role = role,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(
            user,
            AuthenticationWebApplicationFactory.Password);
        context.AppUsers.Add(user);
        context.SaveChanges();
    }

    private void SetAccount(
        string email,
        bool isActive,
        AppUserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var user = context.AppUsers.Single(item =>
            item.NormalizedEmail == email);
        user.IsActive = isActive;
        user.Role = role;
        context.SaveChanges();
    }
}
