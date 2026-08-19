# ParaVolley Team Setup Guide

This guide explains how any team member can clone the ParaVolley repository and run the ASP.NET Core website/backend locally, and how to verify the Android project builds. It is intended for development/testing only.

## 1. Repository and branch workflow

Repository: `23inno/ParaVolley`

Main integration branch: `mobile-api`

Current Android/backend integration branch: `thapelo-android-api-integration`

Do not develop directly on `mobile-api`. Create your own branch from the latest approved integration branch or from `mobile-api`, depending on the task assigned by Lerato.

Typical workflow:

```powershell
git clone https://github.com/23inno/ParaVolley.git
cd ParaVolley
git checkout mobile-api
git pull origin mobile-api
git checkout -b your-name-feature
```

If you are specifically testing Thapelo's Android/API integration before it is merged:

```powershell
git checkout thapelo-android-api-integration
git pull origin thapelo-android-api-integration
```

## 2. Required software for website/backend

Install:

- .NET 8 SDK
- PostgreSQL
- Git
- Visual Studio / VS Code (either is fine)

The backend project targets `.NET 8` and uses PostgreSQL through Npgsql/Entity Framework Core.

## 3. PostgreSQL local database

Each developer should use their own local PostgreSQL database. Do not share a personal PostgreSQL password in the repository.

Create a local database, for example:

```text
Database: paravolley_dev
Host: localhost
Port: 5432
Username: postgres (or your own PostgreSQL user)
Password: your local PostgreSQL password
```

Example connection string format:

```text
Host=localhost;Port=5432;Database=paravolley_dev;Username=postgres;Password=YOUR_LOCAL_PASSWORD
```

## 4. Configure .NET User Secrets

The project uses User Secrets. The project UserSecretsId is:

```text
sports-management-mvc-secrets
```

From the root `SportsManagementMVC` project folder, run these commands and replace only the example values with your own local development values:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=paravolley_dev;Username=postgres;Password=YOUR_LOCAL_POSTGRES_PASSWORD"

dotnet user-secrets set "Jwt:Key" "USE-A-LONG-DEVELOPMENT-ONLY-JWT-KEY-AT-LEAST-32-CHARACTERS"
dotnet user-secrets set "Jwt:Issuer" "ParaVolley"
dotnet user-secrets set "Jwt:Audience" "ParaVolleyMobile"

dotnet user-secrets set "SeedUsers:PlayerPassword" "CHOOSE-A-DEVELOPMENT-PLAYER-PASSWORD"
dotnet user-secrets set "SeedUsers:CoachPassword" "CHOOSE-A-DEVELOPMENT-COACH-PASSWORD"
dotnet user-secrets set "SeedUsers:AdminPassword" "CHOOSE-A-DEVELOPMENT-ADMIN-PASSWORD"
```

Important:

- Do not commit these passwords or JWT keys to GitHub.
- The passwords may differ from one developer PC to another.
- If the team wants identical test passwords, share them privately outside GitHub.

You can confirm which keys exist without posting their values publicly:

```powershell
dotnet user-secrets list
```

## 5. Seeded development account emails

The application seeder currently creates/maintains these development accounts:

| Role | Email |
|---|---|
| Player | `john.doe@email.com` |
| Coach | `john.smith@paravolley.com` |
| Admin | `admin@paravolley.com` |

The passwords come from each developer's User Secrets.

## 6. Restore, build and create/update database

From the root project folder:

```powershell
dotnet restore
dotnet build
```

The application calls `Database.Migrate()` on startup, so existing EF Core migrations are automatically applied when the application starts with a valid database connection.

You may also apply them manually if required:

```powershell
dotnet ef database update
```

If `dotnet ef` is not available:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
```

Then run:

```powershell
dotnet ef database update
```

## 7. Run the website/backend

From the root project folder:

```powershell
dotnet run
```

The terminal will print the local address, for example:

```text
Now listening on: http://localhost:5080
```

Use the exact address shown on that PC.

The MVC website and REST API are hosted by the same ASP.NET Core application.

## 8. Website test responsibility - Tumelo

Tumelo should pull the latest approved code, follow this setup guide, run the ASP.NET MVC website locally, and verify:

- Website launches successfully
- Login works
- Dashboard/navigation works
- Player, coach, event, match, announcement and attendance pages required by the project work correctly
- Forms validate correctly
- Data saves/loads from PostgreSQL as expected
- Website is responsive at desktop/tablet/mobile browser widths
- No obvious broken links, missing pages or layout problems

If Tumelo finds a frontend problem, fix it on Tumelo's branch and commit/push it. If a problem appears to be in the backend/API/database, report it to Lerato and Thapelo before changing working backend code.

## 9. Android project requirements

Android project location:

```text
mobile/
```

Current build setup includes:

- Gradle 9.3.1 wrapper
- Android Gradle Plugin 9.1.1
- Kotlin 2.2.10
- compile/target SDK 36
- Java/JDK 21 works with the current setup
- Retrofit + Gson for API networking

Android Studio's bundled JDK can normally be used. On Windows it is commonly located at:

```text
C:\Program Files\Android\Android Studio\jbr
```

If Gradle in VS Code/PowerShell cannot find Java for the current terminal session:

```powershell
$env:JAVA_HOME="C:\Program Files\Android\Android Studio\jbr"
$env:Path="$env:JAVA_HOMEin;$env:Path"
```

## 10. Android SDK local.properties

`local.properties` is machine-specific and should not be shared/committed.

Example Windows content:

```text
sdk.dir=C:\Users\YOUR_WINDOWS_USER\AppData\Local\Android\Sdk
```

## 11. Build Android without emulator

From the repository root:

```powershell
cd mobile
.\gradlew.bat assembleDebug
```

Expected result:

```text
BUILD SUCCESSFUL
```

This verifies the Android source compiles, but it does not replace emulator/device runtime testing.

## 12. Android runtime test responsibility

A teammate with a reliable Android Studio emulator or physical Android device should test:

1. Player login
2. Dashboard
3. Profile
4. Events list
5. Event registration
6. Registration cancellation
7. Attendance history
8. Announcements/notifications
9. Navigation/back behaviour
10. QR attendance camera scanning
11. Successful QR check-in
12. Invalid/expired/duplicate QR behaviour

For each bug, report:

```text
Screen/feature:
Steps performed:
Expected result:
Actual result:
Screenshot:
Logcat error:
Device/emulator + Android version:
```

Any genuine fix should be made on that team member's own branch and committed/pushed under their GitHub account.

## 13. Android API address

The Android emulator uses:

```text
http://10.0.2.2:5080/
```

`10.0.2.2` means the Windows host PC from the standard Android emulator. Therefore the ASP.NET backend must also be running on that same test PC on port 5080 for this development configuration.

If the backend starts on a different port, update the development base URL accordingly before testing.

A physical Android phone cannot use `10.0.2.2` to reach the developer PC. For physical-device testing, the tester must use an appropriate LAN-accessible backend address or a deployed HTTPS backend and may need firewall/network configuration.

## 14. Team responsibilities

### Thapelo - Backend & Android API Integration

- Maintains backend/API and Android integration already completed
- Supports confirmed integration/backend fixes
- Protects the working API contracts during final integration

### Kamohelo - Android UI/UX

- Completes Android UI/frontend polish
- Primarily edits `screens/`, `components/` and `ui/theme/`
- Avoids changing `network/` unless there is a confirmed integration bug
- Runs `assembleDebug` before pushing changes

### Tumelo - Website

- Runs website locally using this guide
- Completes website UI/responsiveness/validation/functionality checks
- Commits frontend fixes on Tumelo's branch

### Lerato - Project Manager / Lead Backend

- Coordinates final work and branch merges
- Reviews backend/API requirements
- Ensures at least one teammate other than Thapelo can reproduce/run the website locally
- Coordinates unresolved backend bugs
- Confirms testing evidence and final submission readiness

### Assigned Android Tester

- Runs real Android runtime/device tests
- Tests QR camera scanning
- Records screenshots and Logcat for failures
- Commits genuine runtime/device fixes on their own branch

## 15. Security / files that must not be shared in Git

Do not commit/share publicly:

- PostgreSQL passwords
- JWT signing keys
- User Secret values
- `local.properties`
- `.idea/`
- private production/client credentials
- raw JWT tokens

## 16. Important README note

The older repository README contains outdated information describing an EF Core In-Memory database and a hard-coded demo login. The current project actually uses PostgreSQL/Npgsql, EF Core migrations, cookie + JWT authentication and User Secrets. Team members should use this `TEAM_SETUP.md` guide as the current setup source until the old README is updated.
