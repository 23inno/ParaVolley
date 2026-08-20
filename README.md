# ParaVolley Mpumalanga — Sports Management System

ParaVolley is a group project consisting of an **ASP.NET Core MVC website**, a **REST API/backend**, a **PostgreSQL database**, and an **Android mobile application**.

The current project no longer uses the original in-memory database setup. The backend now uses **PostgreSQL with Entity Framework Core/Npgsql**, supports **cookie authentication for the MVC website**, **JWT authentication for the mobile API**, and includes Android API integration for the main player flows.

## Current Tech Stack

### Website / Backend
- ASP.NET Core MVC — .NET 8
- Razor `.cshtml` views
- Entity Framework Core 8
- PostgreSQL
- Npgsql Entity Framework Core provider
- Cookie authentication for the MVC website
- JWT Bearer authentication for the REST API
- .NET User Secrets for development credentials and secrets

### Android
- Kotlin
- Jetpack Compose
- Retrofit
- Gson
- Navigation Compose
- Gradle 9.3.1 wrapper

## Project Structure

```text
SportsManagementMVC/
├── Controllers/           # MVC controllers
│   └── Api/               # Mobile/API controllers
├── Data/                  # DbContext, seeding, supporting services
├── Dtos/                  # API request/response DTOs
├── Migrations/            # EF Core PostgreSQL migrations
├── Models/                # Domain/entity models
├── Views/                 # Razor MVC views
├── wwwroot/               # Website CSS/JS/static files
├── mobile/                # Android application
├── Program.cs             # Application startup/configuration
├── SportsManagementMVC.csproj
├── SportsManagementMVC.sln
├── README.md
└── TEAM_SETUP.md          # Full local setup/reproducibility guide
```

## Important: Team Setup

For the full local setup instructions, use:

```text
TEAM_SETUP.md
```

That guide explains:

- PostgreSQL setup
- .NET User Secrets
- JWT configuration
- seeded development accounts
- database migrations
- running the website/backend
- Android build setup
- Android emulator/device testing
- branch workflow
- security rules

## Quick Website / Backend Setup

Install:

- .NET 8 SDK
- PostgreSQL
- Git

Then clone/pull the repository and configure local development secrets.

The project uses the User Secrets ID:

```text
sports-management-mvc-secrets
```

Required development configuration includes:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Jwt:Issuer
Jwt:Audience
SeedUsers:PlayerPassword
SeedUsers:CoachPassword
SeedUsers:AdminPassword
```

Example connection string format:

```text
Host=localhost;Port=5432;Database=paravolley_dev;Username=postgres;Password=YOUR_LOCAL_PASSWORD
```

Do **not** commit PostgreSQL passwords, JWT keys or User Secret values to GitHub.

## Build and Run the Website / Backend

From the root project folder:

```powershell
dotnet restore
dotnet build
dotnet run
```

The terminal will print the local URL, for example:

```text
Now listening on: http://localhost:5080
```

The MVC website and REST API run from the same ASP.NET Core application.

The application calls `Database.Migrate()` at startup, so existing EF Core migrations are applied automatically when the PostgreSQL connection is valid.

## Seeded Development Accounts

The current development seeder uses these emails:

| Role | Email |
|---|---|
| Player | `john.doe@email.com` |
| Coach | `john.smith@paravolley.com` |
| Admin | `admin@paravolley.com` |

Passwords are not stored in the repository. They are read from .NET User Secrets.

## Main API Capabilities

The current backend includes the following mobile/API areas:

- Authentication / JWT login
- Player registration
- Admin approval/rejection of player accounts
- Player profile
- Player dashboard
- Events
- Event registration/cancellation
- Attendance
- Announcements
- Matches
- QR attendance sessions and player check-in

## Android Mobile Application

The Android project is located at:

```text
mobile/
```

The current mobile integration includes:

- real API login
- configurable debug/release API addresses
- JWT session persistence, expiry checks and automatic 401 logout
- player account registration with administrator approval
- player profile
- player dashboard
- events
- event registration/cancellation
- attendance history
- announcements
- CameraX + ML Kit QR scanning and attendance check-in
- manual QR token fallback for development and devices without camera access

The old `FakePlayerRepository` is no longer used by the active Android screens.

### Build Android without an emulator

From the repository root:

```powershell
cd mobile
.\gradlew.bat assembleDebug
```

Expected result:

```text
BUILD SUCCESSFUL
```

This confirms the Android source compiles, but real emulator/device testing is still required for runtime behaviour and camera operation.

## Android Development API Address

The standard Android emulator uses:

```text
http://10.0.2.2:5080/
```

This points from the emulator to the backend running on the same Windows PC.

If the ASP.NET backend starts on another port, override the debug URL at build time:

```powershell
.\gradlew.bat assembleDebug -PPARAVOLLEY_API_BASE_URL=http://10.0.2.2:YOUR_PORT/
```

The trailing `/` is normalized by the Gradle configuration.

A physical Android device cannot use `10.0.2.2` to reach the development PC. For a USB-connected device with Android Debug Bridge enabled, run:

```powershell
adb reverse tcp:5080 tcp:5080
cd mobile
.\gradlew.bat assembleDebug -PPARAVOLLEY_API_BASE_URL=http://127.0.0.1:5080/
```

Keep the backend running on the PC while testing. As an optional alternative, temporarily supply the PC's LAN address through `PARAVOLLEY_API_BASE_URL`; do not commit a personal IP address. Use HTTPS for deployed/release builds.

## Current Verified Status

At the latest integration checkpoint:

- ASP.NET backend: **build successful, 0 warnings, 0 errors**
- Android project: **`assembleDebug` successful**
- Player login/API authentication: tested
- Player profile: tested
- Player dashboard: tested
- Events: tested
- Player registrations: tested
- Player attendance: tested
- Announcements: tested
- Coach login: tested
- QR attendance end-to-end backend flow: tested successfully

The QR workflow was verified through a fresh test event:

```text
Coach creates event
→ Player registers
→ Coach creates QR session
→ Player submits QR token
→ Backend validates session
→ Attendance saved as Present
→ Player attendance endpoint returns the record
```

## Remaining Work Before Final Submission

The main outstanding work is:

- Kamohelo to complete/polish the Android UI/UX
- Tumelo to run and verify the MVC website locally, including responsiveness and website functionality
- a teammate with a reliable Android Studio emulator or physical Android device to perform full Android runtime testing
- physical QR camera scanning to be runtime-tested on a real device
- regression testing after UI/runtime fixes
- final PR review and merge coordinated by Lerato

## Team Responsibilities

### Thapelo — Backend & Android API Integration
- Backend/API development
- PostgreSQL/database integration
- Android/API integration
- Maintains working API contracts
- Supports confirmed backend/integration bugs

### Kamohelo — Android UI/UX
- Android frontend/UI polish
- Main working areas: `screens/`, `components/`, `ui/theme/`
- Avoid changing `network/` or backend code unless a confirmed bug requires it
- Run `assembleDebug` before pushing changes

### Tumelo — Website
- Run the MVC website locally using `TEAM_SETUP.md`
- Complete/check website UI, responsiveness, navigation, validation and functionality
- Commit genuine frontend fixes on Tumelo's branch
- Report backend/database issues to Lerato/Thapelo before changing working backend code

### Lerato — Project Manager / Lead Backend
- Coordinate final project work
- Review backend/API requirements
- Coordinate branch reviews and merges
- Ensure at least one teammate other than Thapelo can reproduce/run the website locally
- Coordinate unresolved backend issues
- Confirm final testing evidence and submission readiness

### Assigned Android Tester
- Run the real Android app on emulator/device
- Test login, dashboard, profile, events, registration/cancellation, attendance, announcements, navigation and QR camera scanning
- Capture screenshots and Logcat for failures
- Commit genuine runtime/device fixes on their own branch/account

## Git Workflow

Do not develop directly on the shared `mobile-api` branch.

Use:

```text
mobile-api
→ feature branch
→ changes
→ commit
→ push
→ Pull Request
→ review
→ merge
```

Example:

```powershell
git checkout mobile-api
git pull origin mobile-api
git checkout -b your-name-feature
```

Current Android/backend integration work is on:

```text
thapelo-android-api-integration
```

## Security Rules

Do not commit or post publicly:

- PostgreSQL passwords
- JWT signing keys
- User Secret values
- raw JWT tokens
- production/client credentials
- `local.properties`
- `.idea/`

Use development/test accounts when testing the application.

## Final Testing

Before submission, the team should complete one final regression pass covering:

- website startup and responsiveness
- website database-backed functionality
- Android installation and startup
- player login
- dashboard
- profile
- events
- registration/cancellation
- attendance
- announcements
- navigation
- QR camera scanning/check-in
- error handling

For detailed setup and testing instructions, see **`TEAM_SETUP.md`**.
