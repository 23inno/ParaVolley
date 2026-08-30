# ParaVolley PASS 6 Production Readiness Report

**Assessment date:** 2026-08-30
**Branch:** `thapelo-android-api-integration`
**Verdict:** **READY AFTER BLOCKERS**

This report records evidence from the final production-readiness pass. It does not claim that the system is unhackable, enterprise secure, or proven at production scale. No production database data was changed, and no commit or push was made.

## 1. Architecture

ParaVolley is an ASP.NET Core MVC and JWT API application backed by EF Core/PostgreSQL, with a native Android client using Retrofit/OkHttp. The website uses an authenticated cookie with server-derived identity and role claims. The Android API uses signed JWT bearer tokens. The account authority is `AppUser`; Admin, Coach, and Player permissions are enforced server-side. The Android Player flow, event registration, attendance, announcements, and QR attendance remain intact.

## 2. Deployment environment

The intended production-like host is `https://paravolley-production.up.railway.app/`. Local validation used the Development environment and a local PostgreSQL database. The Railway CLI was not installed, and no Railway project/environment access was available, so production variables, deployment logs, backup facilities, and database migration history could not be inspected directly.

The live domain responded, but its routes and response behavior show that it is serving an older deployment rather than the current PASS 1-6 source.

## 3. Database

The local target was confirmed as a localhost PostgreSQL database without printing credentials. A custom-format backup was created before migration at:

`C:\Users\User\AppData\Local\Temp\paravolley-local-before-pass6-20260830-195356.dump`

- Backup time: 2026-08-30T17:53:57.9331417Z
- Size: 43,356 bytes
- SHA-256: `AEEB9D13A306E108D3695CFBE4D5744110821607F4FFA60ED686F7C3A5F1F12E`
- Restore method: restore with `pg_restore` into an empty PostgreSQL target after validating the target database.

The backup is outside the repository and `wwwroot`. No production backup was created or verified because Railway access was unavailable. Production migration and import must not proceed until a Railway/PostgreSQL backup and restore path are confirmed.

## 4. Applied migrations

The migration chain is chronological and model-consistent:

1. `20260810155100_InitialPostgreSql`
2. `20260810202614_AddAppUsers`
3. `20260816221532_AddEventRegistrations`
4. `20260817002314_AddUniqueAttendanceConstraint`
5. `20260817004746_AddQrAttendanceSessions`
6. `20260829212634_AddNormalizedAppUserEmail`
7. `20260830124227_AddPerformanceIndexes`

`dotnet ef migrations has-pending-model-changes --no-build` reported no pending model changes. Idempotent PostgreSQL SQL generation succeeded. The normalized-email migration detects conflicting case-insensitive emails and stops rather than deleting rows. The performance migration adds six indexes and does not transform or delete business data.

The local database update succeeded, and all seven migrations now appear in local migration history. Production migration was not performed and remains a deployment blocker.

## 5. Authentication

Website login and JWT login derive account roles from the database. Passwords use ASP.NET Core password hashing. Public registration cannot select Admin or Coach. Pending/inactive accounts are denied normal authentication. JWT-authenticated requests revalidate current account activation, role, email, and Player linkage against the database, allowing account disablement and role changes to take effect without trusting client claims.

Production startup requires a configured JWT key of at least 32 bytes, issuer, audience, and a token duration between 5 and 60 minutes. The default duration is 60 minutes. Development fallback secrets are not permitted in Production.

## 6. Authorization

Role policies and endpoint authorization cover Admin, Coach, and Player. Player-owned API operations derive identity from the validated principal rather than request-supplied Player IDs. Admin account/security operations are Admin-only. Coach management is bounded and excludes Admin/security operations and non-Practice event management. QR ownership and cross-Coach restrictions are covered by regression tests.

## 7. Public website

Local smoke tests returned HTTP 200 for `/`, `/About`, `/Contact`, `/Events`, `/Matches`, `/Announcements`, `/Sponsors`, and `/Account/Login`.

The live deployment did not pass the required public smoke test. `/About`, `/Contact`, and `/Sponsors` returned 404. Several public routes redirected to an insecure `http://` login URL. This is consistent with an older deployment and missing/incorrect forwarded-header configuration.

## 8. Admin website

Admin role behavior is covered by the 60-test local suite, including role separation and protected operations. A legitimate live Admin smoke test was not performed because the current source is not deployed and no approved live credentials/Railway environment access were available. Passwords were not copied into this report or sent to the old live deployment.

## 9. Coach website

Coach role behavior, denial from Admin operations, sanitized Player access, and Practice-scoped event management are covered locally. A legitimate live Coach smoke test was not performed for the same deployment and credential constraints.

## 10. Player Android

The existing Player API contract and Android flow were preserved. Local backend tests cover successful mobile Player login, disabled/pending denial, role injection, Player ownership, dashboard, registrations, attendance, announcements, and QR behavior. Live Player API and physical-device flows were not executed because the live backend is outdated and no Android device was connected.

## 11. Security controls

PASS 3 controls remain present: server-derived roles, short-lived validated JWTs, database principal revalidation, CSRF protection for cookie writes, external return-URL rejection, security headers, private upload gates, upload size/type/signature checks, QR token hashing/expiry/revocation, rate limiting, and Android encrypted token storage. No security control was weakened in PASS 6.

## 12. Rate limiting

Configured limits are:

- Login: 20 requests per IP per minute.
- Player registration: 5 requests per IP per hour.
- Sensitive account/Admin and QR session management: 30 requests per authenticated account per minute, with IP fallback.
- QR check-in: 60 requests per authenticated account per minute, with IP fallback.
- General API: 120 requests per authenticated account or anonymous IP per minute.

The local controlled login probe produced expected HTTP 429 responses while readiness remained healthy. No production load test was attempted. Because the limiter is in-memory per instance, a shared/gateway limiter is recommended before multi-instance deployment.

## 13. Upload security

Report/logo/photo uploads use generated storage names, explicit size caps, extension and content-signature checks, and protected access for sensitive report/backup locations. SVG upload is disabled. Existing local/Railway filesystem storage is still a limitation: private object storage, malware scanning, retention, and encrypted backups are recommended for higher-risk use.

## 14. JWT security

JWT signature, issuer, audience, lifetime, activation, role, and Player linkage are validated. Tokens are not accepted from request-supplied role fields. Android stores the token using Android Keystore-backed encrypted preferences, removes the legacy plaintext preference store, excludes preferences from backup/transfer, and does not intentionally log JWTs. Operational key rotation/versioning remains future work.

## 15. Cookie security

MVC cookies are HttpOnly and SameSite=Lax, and are Secure in Production when proxy/HTTPS configuration is correct. Local current-source responses included the expected security headers. The old live login response did not issue a Secure cookie and therefore failed production validation. Railway must enable forwarded headers and deploy the current build before cookie security can be accepted.

## 16. Health checks

Local `/health/live` and `/health/ready` both returned HTTP 200 with minimal healthy JSON. Readiness verifies database connectivity and does not expose connection details.

Both live health routes returned 404, confirming that the current health-enabled source is not deployed. Railway probes cannot be considered configured or healthy yet.

## 17. Database resilience

EF contexts are scoped, PostgreSQL connections use Npgsql pooling, queries are asynchronous and bounded, key lists are paginated, and hot lookup/filter paths have explicit indexes. Unique constraints and conflict classification protect concurrent Player registration, Coach provisioning, attendance, event registration, and QR check-in. Startup migration remains acceptable for a controlled single-instance academic deployment; multi-instance deployment should use one controlled migration job before application replicas start.

## 18. Load evidence

The controlled local probe completed:

- 80/80 public requests with 20 workers, no 5xx.
- 80/80 authenticated requests with 20 workers, no 5xx.
- 50/50 public burst requests, no 5xx.
- 25 small invalid-login attempts, including 6 expected HTTP 429 responses, with readiness remaining healthy.

This is useful local evidence, not a Railway capacity guarantee. Production database latency, service quotas, CPU, storage I/O, and realistic data volume were not measured.

## 19. Real-data import

The `RealCalendarImportService` remains guarded, duplicate-aware, transactional, and non-overwriting. A local Development dry run against the migrated database reported:

- Creatable: 8
- Skipped duplicate: 0
- Rejected: 0
- Requires confirmation: 11

No records were imported locally or in production during PASS 6. Production import must wait for a verified backup, deployment of the current code, a production dry run, and review of its counts.

## 20. Pending client data

Eleven of nineteen calendar activities remain pending because they have month-only/TBD dates, missing venues, or wording requiring confirmation. No dates or venues were invented. `Middleburg` remains exactly as supplied by the client. Existing production rows cannot be safely classified as demo or real from names alone, so no cleanup was attempted.

## 21. Railway configuration

Current `Program.cs` expects these production variables without exposing their values:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenMinutes` (recommended/current default: 60)
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
- `AllowedHosts=paravolley-production.up.railway.app`
- Railway-provided `PORT`
- `SeedData__EnableDemoData` absent or false
- `SeedUsers__BootstrapAdminEnabled` normally false after initial bootstrap

Production `AllowedHosts` is now restricted to the actual Railway hostname, while Development permits local hosts. These live variable values and production seeding behavior could not be verified without Railway access. The live insecure redirects strongly indicate that forwarded headers and/or the current deployment are not in effect.

## 22. Android release configuration

`PARAVOLLEY_RELEASE_API_BASE_URL` is configured as `https://paravolley-production.up.railway.app/`. The release APK contains that HTTPS URL and does not contain the invalid fallback URL. Debug-only localhost/`10.0.2.2` support remains isolated to debug configuration. Release cleartext traffic is disabled; debug cleartext remains available for local development.

The generated release artifact is `app-release-unsigned.apk`, 32,399,866 bytes. It is intentionally unsigned because no legitimate release keystore/signing configuration was available. A real protected signing key and release signing process are required before installation/distribution.

## 23. Smoke-test results

- Backend restore/build: succeeded, 0 warnings and 0 errors.
- Backend tests: 60 passed, 0 failed, 0 skipped.
- NuGet vulnerable-package audit: no known vulnerable packages in application or test project.
- Local migration/startup/health/public routes: passed.
- Android `assembleDebug`, `testDebugUnitTest`, `lintDebug`, and `assembleRelease`: passed.
- Android unit tests: 1 passed, 0 failed, 0 skipped.
- Android lint: 0 errors, 24 warnings, plus informational findings. Warnings are primarily deprecations around the currently required encrypted-preferences APIs; they did not fail the build.
- Connected devices: none. Physical phone and live QR smoke tests were not performed.
- Live Railway: failed current-source readiness checks; live health routes were 404, several expected public routes were 404, redirects used HTTP, and expected security headers were absent.

## 24. Known limitations

1. The current PASS 1-6 source has not been deployed to Railway.
2. Railway variables, logs, database backup, and migration history were not accessible.
3. The release APK is unsigned.
4. No physical Android device was connected.
5. No live Admin, Coach, Player, QR, or safe production rate-limit smoke test was performed.
6. File uploads use local/ephemeral storage rather than private durable object storage.
7. Rate limiting is instance-local.
8. Admin MFA, breached-password checks, centralized security audit logging, and JWT key rotation/versioning remain future improvements.
9. CSP currently permits inline styles/scripts used by existing Razor pages.

## 25. Residual risks

The highest immediate risk is deploying or continuing to expose the old Railway build: it lacks the current health routes and expected PASS 3 response protections, and it redirects over HTTP. Applying production migrations/import without a verified backup would also be unsafe. A signed APK must not be produced with an improvised or committed key. Live authorization conclusions cannot be drawn until the current source is deployed and legitimate test accounts are used.

Required unblock sequence:

1. Obtain Railway project access and verify/create a restorable PostgreSQL backup.
2. Verify all required environment variables, forwarded-header support, demo seeding off, and Admin bootstrap off.
3. Deploy the current branch through the approved Railway process.
4. Confirm production migrations, `/health/live`, `/health/ready`, HTTPS redirects, Secure cookies, and security headers.
5. Run minimal legitimate Admin/Coach/Player authorization smoke tests.
6. Run the production calendar dry run; import only the eight verified records after counts and backup are approved.
7. Configure legitimate Android release signing, rebuild, and perform physical-device/live API and QR tests using designated test records.

## 26. Git commit/push state

The working tree contains the reviewed PASS 1-6 source and reports plus expected untracked source/test/migration files. Generated `.NET` outputs, Gradle/Android build directories, generated APKs, local backups, uploads, user secrets, and credentials are excluded and were not staged.

`git diff --check` reported no whitespace errors; it emitted only expected LF-to-CRLF conversion warnings on Windows. The final secret scan found no production private key, JWT token, credentialed PostgreSQL URI, or literal production configuration secret. Literal credentials are limited to isolated test fixtures, and `TEAM_SETUP.md` contains clearly marked example placeholders.

No commit was created and nothing was pushed because critical live deployment, production backup/migration, role smoke, signing, and physical-device validation did not succeed. The branch remains `thapelo-android-api-integration`.

## Final verdict

**READY AFTER BLOCKERS**
