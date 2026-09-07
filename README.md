# ParaVolley Mpumalanga – Sports Management System

ParaVolley Mpumalanga is a group project developed to make it easier to manage players, events, matches, attendance, announcements and other information for ParaVolley Mpumalanga.

The final system consists of:

- an ASP.NET Core MVC website
- a REST API
- a PostgreSQL database
- an Android mobile application

The website and API are part of the same ASP.NET Core application. The system uses PostgreSQL for persistent data storage, while the Android application communicates with the backend through the REST API.

## Live Website

The web application has been deployed to Railway and is available at:

https://paravolley-production.up.railway.app/

The deployed version uses a PostgreSQL database hosted on Railway.

## Technologies Used

### Website and Backend

- ASP.NET Core MVC (.NET 8)
- Razor views
- Entity Framework Core
- PostgreSQL
- Npgsql
- REST API
- Cookie authentication for the website
- JWT authentication for the Android application

### Android Application

- Kotlin
- Jetpack Compose
- Retrofit
- Gson
- Navigation Compose
- CameraX
- ML Kit QR scanning

### Deployment

- GitHub for source control
- Railway for the ASP.NET Core website/API
- Railway PostgreSQL for the production database

## Project Structure

```text
SportsManagementMVC/
├── Controllers/        # MVC controllers
│   └── Api/            # REST API controllers
├── Data/               # Database context and seeders
├── Dtos/               # API request and response models
├── Migrations/         # Entity Framework Core migrations
├── Models/             # Application models/entities
├── Views/              # Razor website views
├── wwwroot/            # Website CSS, JavaScript and static files
├── mobile/             # Android application
├── Program.cs
├── SportsManagementMVC.csproj
├── SportsManagementMVC.sln
├── README.md
└── TEAM_SETUP.md
```

## Main Features

The system includes functionality for:

- player management
- coach management
- events
- event registration and cancellation
- matches
- attendance
- announcements/news
- reports
- system settings
- player registration and approval
- player dashboard and profile
- QR attendance

The Android application allows players to log in and access the main player features from their phones.

## Authentication

The project uses two forms of authentication.

The MVC website uses cookie authentication, while the Android application uses JWT Bearer authentication when communicating with the REST API.

Development/test accounts are seeded for the Player, Coach and Admin roles.

The seeded email addresses are:

| Role | Email |
| --- | --- |
| Player | `john.doe@email.com` |
| Coach | `john.smith@paravolley.com` |
| Admin | `admin@paravolley.com` |

Passwords are not stored in the repository.

For local development they are supplied using .NET User Secrets. For the deployed Railway version they are supplied using Railway environment variables.

## Database

The application uses PostgreSQL instead of the original in-memory database setup.

Entity Framework Core and the Npgsql provider are used to communicate with PostgreSQL.

EF Core migrations are used to create and update the database structure.

Some of the main persisted data includes:

- players
- coaches
- users
- events
- event registrations
- matches
- attendance
- announcements
- QR attendance sessions

The development environment uses a local PostgreSQL database, while the deployed application uses PostgreSQL hosted on Railway.

## Running the Website Locally

Requirements:

- .NET 8 SDK
- PostgreSQL
- Git

Clone the repository and configure the required User Secrets.

The application expects configuration for:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Jwt:Issuer
Jwt:Audience
SeedUsers:PlayerPassword
SeedUsers:CoachPassword
SeedUsers:AdminPassword
```

Do not commit these values to GitHub.

From the project folder run:

```powershell
dotnet restore
dotnet build
dotnet run
```

The local development server normally runs at:

```text
http://localhost:5080
```

The exact URL can also be checked in the terminal after starting the application.

## Android Application

The Android project is located inside:

```text
mobile/
```

The Android application uses the ParaVolley REST API instead of relying on fake player data.

The implemented mobile functionality includes:

- player login
- player registration
- session persistence
- player dashboard
- profile
- events
- event registration and cancellation
- attendance history
- announcements
- QR attendance scanning
- logout

## Building the Android App

From the `mobile` directory:

```powershell
.\gradlew.bat assembleDebug
```

A successful build produces the debug APK at:

```text
mobile/app/build/outputs/apk/debug/app-debug.apk
```

For live beta testing, the Android application can be built against the deployed Railway API:

```powershell
.\gradlew.bat assembleDebug -PPARAVOLLEY_API_BASE_URL=https://paravolley-production.up.railway.app/
```

## Deployment

The ASP.NET Core website and REST API are deployed together on Railway.

The Android application does not need to be hosted as a separate web service. It communicates with the deployed API over HTTPS.

## Environment Setup

Development and debugging are done locally using ASP.NET Core, PostgreSQL and Android Studio. The live beta environment uses Railway ASP.NET Core hosting, Railway PostgreSQL, HTTPS and Railway environment variables for sensitive configuration.

## Security

Sensitive information should never be committed to the repository. This includes PostgreSQL passwords, JWT signing keys, seeded account passwords, production credentials, raw JWT tokens and local development secrets. Local development secrets are managed using .NET User Secrets and production configuration is stored using Railway environment variables.

## Team

The project is a group project, with different members contributing to the website, Android application, backend, database and project management.

## Current Status

ParaVolley Mpumalanga is currently in a deployed beta/testing stage. The website and API are publicly accessible through Railway and the PostgreSQL production database is connected. The Android beta communicates with the deployed API and continues to undergo real-device testing and user-interface refinement.

The public website frontend is also being refined through reviewed team contributions before changes are incorporated into the deployment branch. Further regression testing, mobile UI refinement and operational checks will continue before the project is treated as a final public release.
