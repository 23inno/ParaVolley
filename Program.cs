using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportsManagementMVC.Data;
using SportsManagementMVC.Health;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key was not found.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not found.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not found.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

var accessTokenMinutes =
    builder.Configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;

if (accessTokenMinutes is < 5 or > 60)
{
    throw new InvalidOperationException(
        "JWT access token lifetime must be between 5 and 60 minutes.");
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 12 * 1024 * 1024;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 12 * 1024 * 1024;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.CommandTimeout(30)));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<RealCalendarImportService>();

builder.Services.AddScoped<
    IPasswordHasher<AppUser>,
    Microsoft.AspNetCore.Identity.PasswordHasher<AppUser>>();

builder.Services.AddScoped<AppUserPrincipalValidator>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment() ||
            builder.Environment.IsEnvironment("Testing")
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        options.Events.OnValidatePrincipal = async context =>
        {
            var validator = context.HttpContext.RequestServices
                .GetRequiredService<AppUserPrincipalValidator>();

            if (!await validator.IsCurrentAsync(context.Principal))
            {
                context.RejectPrincipal();
            }
        };
    })
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var validator = context.HttpContext.RequestServices
                        .GetRequiredService<AppUserPrincipalValidator>();

                    if (!await validator.IsCurrentAsync(context.Principal))
                    {
                        context.Fail("The account is no longer authorized.");
                    }
                }
            };
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(nameof(AppUserRole.Admin)));

    options.AddPolicy(
        AuthorizationPolicies.CoachOnly,
        policy => policy.RequireRole(nameof(AppUserRole.Coach)));

    options.AddPolicy(
        AuthorizationPolicies.PlayerOnly,
        policy => policy.RequireRole(nameof(AppUserRole.Player)));

    options.AddPolicy(
        AuthorizationPolicies.AdminOrCoach,
        policy => policy.RequireRole(
            nameof(AppUserRole.Admin),
            nameof(AppUserRole.Coach)));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => context.Request.Path.StartsWithSegments("/api")
            ? RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
            : RateLimitPartition.GetNoLimiter("non-api"));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString();
        }

        if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { message = "Too many requests. Please try again later." },
                cancellationToken);
        }
    };

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("registration", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    options.AddPolicy("contact", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("sensitive", context =>
            RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("qr-check-in", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("general-api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

if (!app.Configuration.GetValue<bool>("SkipDatabaseInitialization"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var appUserPasswordHasher = scope.ServiceProvider
        .GetRequiredService<IPasswordHasher<AppUser>>();

    context.Database.Migrate();

    DbInitializer.Seed(context, builder.Configuration);

    AppUserSeeder.Seed(
        context,
        appUserPasswordHasher,
        builder.Configuration,
        app.Environment);

    if (app.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>(
            "SeedData:DryRunRealCalendar"))
    {
        var importer = scope.ServiceProvider
            .GetRequiredService<RealCalendarImportService>();
        var dryRun = await importer.DryRunAsync();
        app.Logger.LogInformation(
            "Real calendar dry run: {Created} creatable, {Duplicates} duplicates, " +
            "{Rejected} rejected, {Confirmation} require confirmation.",
            dryRun.Created,
            dryRun.SkippedDuplicate,
            dryRun.RejectedInvalid,
            dryRun.RequiresConfirmation);
    }

    if (app.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>(
            "SeedData:ImportRealCalendar"))
    {
        var importer = scope.ServiceProvider
            .GetRequiredService<RealCalendarImportService>();
        var import = await importer.ImportAsync(
            builder.Configuration.GetValue<bool>(
                "SeedData:RealCalendarBackupConfirmed"));
        app.Logger.LogInformation(
            "Real calendar import: {Created} created, {Duplicates} duplicates, " +
            "{Rejected} rejected, {Confirmation} require confirmation.",
            import.Created,
            import.SkippedDuplicate,
            import.RejectedInvalid,
            import.RequiresConfirmation);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] =
            "camera=(), geolocation=(), microphone=()";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
            "form-action 'self'; img-src 'self' data: blob:; " +
            "font-src 'self' data: https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "connect-src 'self'";
        return Task.CompletedTask;
    });

    await next();
});

app.Use(async (context, next) =>
{
    var isPrivateUpload =
        context.Request.Path.StartsWithSegments("/uploads/reports") ||
        context.Request.Path.StartsWithSegments("/uploads/backups");

    if (isPrivateUpload &&
        !(context.User.Identity?.IsAuthenticated == true &&
          context.User.IsInRole(nameof(AppUserRole.Admin))))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteMinimalHealthResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteMinimalHealthResponse
}).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static Task WriteMinimalHealthResponse(
    HttpContext context,
    Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status ==
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
                ? "Healthy"
                : "Unhealthy"
    });
}

public partial class Program
{
}
