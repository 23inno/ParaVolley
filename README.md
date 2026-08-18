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


---

# ParaVolley Team Development Guide

ParaVolley is a group project consisting of a web application, backend/API, and Android mobile application.

To protect the working project while allowing every group member to contribute, all team members must follow the Git and GitHub workflow described below.

## Team Members and Responsibilities

### Thapelo
Responsibilities:
- Backend development
- REST API development
- Database integration
- Android/API integration
- Mobile application development
- Website development when required
- Reviewing backend changes

### Lerato
Role: Project Manager and Lead Backend Developer

Responsibilities:
- Backend development
- REST API development
- Backend review
- Project coordination
- Mobile application contributions
- Website contributions
- Access across the project

### Kamohelo (Zani)
Responsibilities:
- Website frontend development
- UI/UX development
- Mobile application development
- Mobile frontend features
- General frontend improvements

### Tumelo
Responsibilities:
- Website frontend development
- UI/UX development
- Mobile application development
- Mobile frontend features
- Reviewing and understanding the overall project

---

# Project Areas

## Backend

The backend includes areas such as:

- `Controllers/Api/`
- `Data/`
- `Dtos/`
- `Models/`
- `Migrations/`

Primary backend developers:

- Thapelo
- Lerato

Backend changes should normally be handled by Thapelo and Lerato.

If another team member believes a backend change is required, the change should first be discussed with Thapelo or Lerato.

This helps prevent frontend or mobile changes from accidentally breaking the API, authentication, database, or existing integrations.

---

## Website

Important website/frontend areas include:

- `Views/`
- `wwwroot/`

Website development may be performed by:

- Thapelo
- Lerato
- Kamohelo
- Tumelo

Kamohelo and Tumelo will primarily focus on frontend and UI/UX work.

---

## Android Mobile Application

The Android project is located in:

`mobile/`

All team members may contribute to the mobile application:

- Thapelo
- Lerato
- Kamohelo
- Tumelo

This includes:

- UI improvements
- New screens
- Navigation
- Dashboard features
- Event features
- Match features
- Attendance features
- QR features
- API integration
- Bug fixes

Mobile developers should avoid changing backend code while implementing mobile features unless the backend change has been discussed with Thapelo or Lerato.

---

# Git and GitHub Workflow

## Important Rule

Do not develop directly on the shared `mobile-api` branch.

`mobile-api` is the project's main integration branch.

Each developer should create a separate branch for the feature they are working on.

The normal workflow is:

`mobile-api -> feature branch -> changes -> commit -> push -> Pull Request -> review -> merge`

---

## Before Starting Work

Always get the latest version of the project first:

```bash
git checkout mobile-api
git pull origin mobile-api
