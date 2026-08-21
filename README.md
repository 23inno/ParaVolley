# ParaVolley Mpumalanga — Sports Management System

ParaVolley Mpumalanga is a group sports management system designed to support the administration of ParaVolley players, events, matches, attendance, announcements and team activities.

The project consists of:

- an **ASP.NET Core MVC web application**
- an **ASP.NET Core REST API**
- a **PostgreSQL database**
- an **Android mobile application**
- QR-based attendance functionality

The website and REST API are provided by the same ASP.NET Core backend and share the same PostgreSQL database.

The Android application communicates with the backend through the REST API.

---

## Technology Stack

### Website / Backend

- ASP.NET Core MVC — .NET 8
- ASP.NET Core Web API
- Razor `.cshtml` views
- Entity Framework Core 8
- PostgreSQL
- Npgsql Entity Framework Core provider
- Cookie authentication for the MVC website
- JWT Bearer authentication for the mobile API
- ASP.NET Core password hashing
- .NET User Secrets for local development secrets
- Docker support for deployment

### Android

- Kotlin
- Jetpack Compose
- Retrofit
- Gson
- Navigation Compose
- CameraX
- Google ML Kit barcode/QR scanning
- Gradle

---

## System Architecture

The project follows the following high-level architecture:

```text
                 ┌─────────────────────┐
                 │    MVC Website      │
                 │ Admin / Management  │
                 └──────────┬──────────┘
                            │
                            │
                 ┌──────────▼──────────┐
                 │ ASP.NET Core Backend│
                 │   MVC + REST API    │
                 └──────────┬──────────┘
                            │
                    Entity Framework Core
                            │
                 ┌──────────▼──────────┐
                 │     PostgreSQL      │
                 │      Database       │
                 └─────────────────────┘
                            ▲
                            │
                      REST API / JWT
                            │
                 ┌──────────┴──────────┐
                 │   Android Mobile    │
                 │    Application      │
                 └─────────────────────┘
```

---

## Project Structure

```text
SportsManagementMVC/
├── Controllers/            # MVC controllers
│   └── Api/                # REST/mobile API controllers
├── Data/                   # DbContext, seeders and supporting services
├── Dtos/                   # API request/response DTOs
├── Migrations/             # EF Core PostgreSQL migrations
├── Models/                 # Domain/entity models
├── Views/                  # Razor MVC views
├── wwwroot/                # Website CSS, JavaScript and static files
├── mobile/                 # Android application
├── Dockerfile              # Deployment container configuration
├── Program.cs              # Application startup/configuration
├── SportsManagementMVC.csproj
├── SportsManagementMVC.sln
├── README.md
└── TEAM_SETUP.md           # Detailed team setup/reproducibility guide
```

---

## Team Setup

For complete local installation and setup instructions, see:

```text
TEAM_SETUP.md
```

The guide covers:

- PostgreSQL setup
- .NET User Secrets
- JWT configuration
- seeded development accounts
- Entity Framework migrations
- running the website/backend
- Android build setup
- emulator and physical-device testing
- Git workflow
- security requirements

---

## Website / Backend Setup

### Requirements

Install:

- .NET 8 SDK
- PostgreSQL
- Git

Clone the repository and configure the required local secrets before running the application.

The project uses the User Secrets ID:

```text
sports-management-mvc-secrets
```

Required local configuration includes:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Jwt:Issuer
Jwt:Audience
SeedUsers:PlayerPassword
SeedUsers:CoachPassword
SeedUsers:AdminPassword
```

Example PostgreSQL connection string:

```text
Host=localhost;Port=5432;Database=paravolley_dev;Username=postgres;Password=YOUR_LOCAL_PASSWORD
```

Never commit database passwords, JWT signing keys or User Secret values to GitHub.

---

## Build and Run the Website / Backend

From the root project directory:

```powershell
dotnet restore
dotnet build
dotnet run
```

A successful startup will display an address similar to:

```text
Now listening on: http://localhost:5080
```

Open the displayed URL in a browser.

The MVC website and REST API run from the same ASP.NET Core application.

The application runs:

```csharp
Database.Migrate()
```

during startup, allowing existing Entity Framework Core migrations to be applied automatically when a valid PostgreSQL connection is available.

---

## Database

The project uses **PostgreSQL** as its persistent relational database.

Entity Framework Core with the Npgsql provider is used for database access and migrations.

The database stores system information including:

- players
- application users
- events
- event registrations
- matches
- attendance
- announcements
- QR attendance sessions

The project no longer relies on the original in-memory database configuration for its active backend.

---

## Authentication and Authorization

ParaVolley uses two authentication mechanisms.

### MVC Website

The MVC website uses **cookie authentication**.

### Mobile API

The REST API uses **JWT Bearer authentication**.

The API supports the following roles:

- Admin
- Coach
- Player

Protected endpoints use role-based authorization to restrict access to appropriate functionality.

Passwords are stored using ASP.NET Core password hashing rather than plain text.

---

## Seeded Development Accounts

The development seeder provides the following test account emails:

| Role | Email |
|---|---|
| Player | `john.doe@email.com` |
| Coach | `john.smith@paravolley.com` |
| Admin | `admin@paravolley.com` |

Passwords are **not stored in the repository**.

They are supplied through .NET User Secrets during local development or secure environment variables when deployed.

These accounts are intended for development and testing.

---

## Main Backend / API Capabilities

The current backend includes:

- JWT authentication
- player account registration
- administrator approval/rejection of player registrations
- player profile
- player dashboard
- events
- event registration and cancellation
- attendance
- announcements
- matches
- QR attendance sessions
- QR session expiry/revocation
- player QR attendance check-in
- role-based authorization

---

## Player Dashboard

The Android application retrieves player dashboard information from the real backend.

Dashboard information includes relevant data such as:

- player information
- upcoming events
- registered events
- attendance statistics
- announcements
- recent match information

The active Android screens no longer depend on the original fake player repository for these core flows.

---

## Events and Registrations

Players can:

- view available events
- view event details
- register for events
- cancel registrations
- retrieve their existing registrations

Registration data is persisted in PostgreSQL.

---

## Attendance

Attendance information is stored in PostgreSQL.

Players can retrieve their own attendance history through the mobile API.

Admin/Coach functionality supports attendance management.

The project also supports QR-based attendance.

---

## QR Attendance

The QR attendance workflow connects the backend and Android application.

Typical workflow:

```text
Coach/Admin creates attendance session
        ↓
Backend generates QR attendance token/session
        ↓
Player scans QR code
        ↓
Android submits token to backend
        ↓
Backend validates session
        ↓
Attendance recorded
        ↓
Player attendance history updated
```

The backend handles:

- invalid QR sessions
- expired sessions
- revoked sessions
- duplicate attendance attempts

The Android application uses:

- CameraX
- Google ML Kit QR detection
- backend QR check-in
- manual-token fallback for testing

---

## Android Mobile Application

The Android project is located in:

```text
mobile/
```

The current application includes:

- player login
- player registration
- JWT session persistence
- session expiry handling
- dashboard
- profile
- events
- event registration/cancellation
- attendance history
- announcements
- QR camera scanning
- backend QR check-in
- logout
- loading/error handling

The Android UI has also been integrated with the final API-connected screens and ParaVolley visual theme.

---

## Build the Android Application

From the repository root:

```powershell
cd mobile
.\gradlew.bat assembleDebug
```

Expected result:

```text
BUILD SUCCESSFUL
```

Additional verification can be performed using:

```powershell
.\gradlew.bat testDebugUnitTest
.\gradlew.bat lintDebug
```

A successful build confirms that the Android source compiles.

Physical-device testing is still recommended for runtime behaviour and camera functionality.

---

## Android Development API Address

When using the standard Android emulator, the backend running on the Windows development PC can normally be accessed through:

```text
http://10.0.2.2:5080/
```

If the backend runs on another port, the debug API URL can be overridden at build time.

Example:

```powershell
.\gradlew.bat assembleDebug -PPARAVOLLEY_API_BASE_URL=http://10.0.2.2:YOUR_PORT/
```

A physical Android device cannot use `10.0.2.2` to access the development PC.

For USB debugging with Android Debug Bridge:

```powershell
adb reverse tcp:5080 tcp:5080
```

The application can then be built using an appropriate local API address.

Do not commit personal development-machine IP addresses.

Deployed/release applications should use HTTPS.

---

## Website Features

The MVC website provides the management side of the ParaVolley system.

Current areas include:

- Dashboard
- Players
- Events
- Matches
- Attendance
- News / Announcements
- Reports
- Coaches
- Settings
- authenticated account information
- Logout

The shared sidebar supports expanded and collapsed states and responsive layouts.

The navigation and account/logout section have been adjusted so the authenticated user's information and logout functionality remain accessible.

---

## Responsive Website

The shared website layout has been checked at representative viewport sizes including:

- 1920 × 1080 desktop
- 1366 × 768 laptop
- 820 × 1180 tablet
- 390 × 844 mobile

Additional final cross-browser and device testing should still be completed before submission.

---

## Deployment

A Docker configuration is included:

```text
Dockerfile
```

This allows the ASP.NET Core application to be built and hosted in a container-based deployment environment.

Production configuration must be supplied securely through environment variables rather than committed configuration files.

Typical deployment configuration includes:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Jwt__Issuer
Jwt__Audience
SeedUsers__PlayerPassword
SeedUsers__CoachPassword
SeedUsers__AdminPassword
ASPNETCORE_ENVIRONMENT
ASPNETCORE_URLS
```

A hosted PostgreSQL database should be used for the deployed application.

Do not commit production credentials to GitHub.

---

## Current Verified Status

At the latest integration checkpoint:

| Component | Status |
|---|---|
| ASP.NET Core build | Successful |
| Backend warnings | 0 |
| Backend errors | 0 |
| PostgreSQL connection | Working |
| EF Core migrations | Up to date |
| JWT authentication | Working |
| Player profile API | Tested |
| Player dashboard API | Tested |
| Events API | Tested |
| Event registrations | Tested |
| Attendance API | Tested |
| Announcements API | Tested |
| Coach authentication | Tested |
| QR backend workflow | Tested |
| Android `assembleDebug` | Successful |
| Android unit tests | Successful |
| Android lint | Successful |
| Website responsive sidebar | Verified |

---

## Final Verification Before Submission

The implementation is substantially complete.

Final submission verification should include:

- website startup
- website login/logout
- responsive website navigation
- database-backed website functionality
- Android installation/startup
- player login
- player registration
- dashboard
- profile
- events
- event registration/cancellation
- attendance
- announcements
- session persistence
- logout
- QR camera permission
- physical QR scanning
- backend QR attendance check-in
- error handling
- deployed website verification, if deployment is required

Physical-device QR scanning should be verified on the actual Android device used for testing or demonstration.

---

## Team Responsibilities

### Thapelo — Backend & Android API Integration

Responsible for:

- ASP.NET Core backend/API development
- PostgreSQL integration
- Entity Framework migrations
- authentication/API security
- Android/backend integration
- QR attendance backend integration
- API contracts
- integration troubleshooting
- deployment preparation

### Kamohelo — Android UI/UX

Contributed to:

- Android frontend/UI
- screen layouts
- components
- theme and visual styling
- mobile user experience

Selected UI work was integrated into the final API-connected Android implementation without replacing newer backend integration.

### Tumelo — Website

Responsible for:

- MVC website frontend
- website UI
- responsive design
- navigation
- validation
- website functionality
- frontend testing

### Lerato — Project Manager / Lead Backend

Responsible for:

- project coordination
- backend/API requirement review
- branch/merge coordination
- final testing coordination
- submission readiness
- technical/project oversight

---

## Git Workflow

Feature/integration work should be performed on dedicated branches rather than directly modifying a shared stable branch.

Typical workflow:

```text
shared branch
    ↓
feature/integration branch
    ↓
changes
    ↓
commit
    ↓
push
    ↓
Pull Request / review
    ↓
merge
```

The final backend/Android integration work has been developed on:

```text
thapelo-android-api-integration
```

Safety branches were also used before major integration work.

Do not force-push shared branches unless the team has explicitly agreed to it.

---

## Security Rules

Never commit or publicly share:

- PostgreSQL passwords
- JWT signing keys
- User Secret values
- raw JWT tokens
- production/client credentials
- private API credentials
- `local.properties`
- personal development-machine configuration

Use development/test accounts for testing.

Production secrets should be configured using the hosting platform's secure environment-variable/secrets system.

---

## Documentation

For detailed local installation, configuration and testing instructions, see:

```text
TEAM_SETUP.md
```

---

## Project Status

**Final integration / testing and deployment stage.**

The core backend, PostgreSQL database integration, MVC application, Android API integration and QR attendance workflow have been implemented.

Remaining work is primarily final runtime/device verification, deployment verification and submission evidence rather than major feature development.
This message is private and confidential. If you have received this message in error, please notify the sender immediately and delete the original message from your machine. For the full disclaimer, visit STADIO Disclaimer
