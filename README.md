# ParaVolley Mpumalanga — Sports Management System (ASP.NET Core MVC)

A full **ASP.NET Core MVC** rebuild of the original Figma Make React app, using the
classic **Model / View / Controller** structure with `.cshtml` Razor views.

## Tech stack

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core — **In-Memory** database provider (no SQL Server install required — data resets each time you stop the app, and is re-seeded automatically)
- Cookie-based authentication (login required to view any page except the login screen)
- Bootstrap 5 + Bootstrap Icons (via CDN)

## Project structure

```
SportsManagementMVC/
├── Controllers/        # HomeController, PlayersController, CoachesController,
│                          EventsController, MatchesController, AnnouncementsController,
│                          AttendanceController, ReportsController, SettingsController,
│                          AccountController (login/logout)
├── Models/              # Player, Coach, Event, Match, Announcement, Attendance,
│                          plus view models (Dashboard, Reports, Settings, Login)
├── Data/                # ApplicationDbContext (EF Core) + DbInitializer (seed data)
├── Views/                # .cshtml Razor views, one folder per controller
│   └── Shared/           # _Layout.cshtml (sidebar + topbar), Error.cshtml
├── wwwroot/              # site.css, site.js
├── Program.cs            # app startup, DB + auth configuration
└── SportsManagementMVC.sln
```

## How to run (Visual Studio 2022 / 2026)

1. Double-click `SportsManagementMVC.sln` to open the solution in Visual Studio.
2. Visual Studio will restore NuGet packages automatically (EF Core, EF Core InMemory).
   If it doesn't, right-click the solution in **Solution Explorer** → **Restore NuGet Packages**.
3. Press **F5** (or the green ▶ **Run** button) to build and launch.
4. Your browser will open to the login page automatically.

### Demo login

```
Email:    admin@paravolley.com
Password: Admin123!
```

## Features covered (for the assignment write-up)

- **Models**: `Player`, `Coach`, `Event`, `Match`, `Announcement`, `Attendance`, each with
  data annotations for validation (`[Required]`, `[EmailAddress]`, `[Range]`, etc.)
- **Controllers**: full CRUD (Create, Read, Update, Delete) for Players, Coaches, Events,
  Matches, and Announcements, plus a dedicated Attendance controller (with foreign-key
  dropdowns to Player/Event) and a Reports controller that aggregates data with LINQ.
- **Views**: Razor `.cshtml` views using Tag Helpers (`asp-for`, `asp-action`,
  `asp-validation-for`) for strongly-typed forms, plus partials for shared layout and
  client-side validation scripts.
- **Entity Framework Core**: `DbContext` with `DbSet<T>` for each entity, relationships
  between `Attendance` → `Player`/`Event` via foreign keys, and `Include()`/LINQ queries
  in the controllers.
- **Authentication**: cookie-based login using `ClaimsPrincipal`, with `[Authorize]`
  attributes protecting all controllers except `AccountController`.

## Switching to a real SQL Server database (optional)

Right now the app uses `UseInMemoryDatabase(...)` in `Program.cs` so it runs instantly
with zero setup. If your assignment requires a persistent database instead:

1. Add the SQL Server EF Core package:
   ```
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   ```
2. In `Program.cs`, replace:
   ```csharp
   options.UseInMemoryDatabase("SportsManagementDb")
   ```
   with:
   ```csharp
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
   ```
3. Add a connection string to `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SportsManagementDb;Trusted_Connection=True;"
   }
   ```
4. In the **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console):
   ```
   Add-Migration InitialCreate
   Update-Database
   ```

Let me know if you'd like help doing this migration.
