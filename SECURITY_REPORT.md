# ParaVolley PASS 3 Security Report

## 1. Scope

This defensive review covers the ASP.NET Core MVC website, JWT API, EF Core/PostgreSQL access, upload paths, QR attendance workflow, configuration/secrets, and the native Android client on `thapelo-android-api-integration`. It does not claim that the system is unhackable and does not include destructive production testing, infrastructure penetration testing, or source-history rewriting.

## 2. Architecture

- `AppUser` is the authentication authority. Passwords use ASP.NET Core `IPasswordHasher<AppUser>`.
- MVC uses an authenticated cookie containing server-derived identity and role claims.
- Android/API uses issuer-, audience-, lifetime-, and signature-validated HMAC-SHA256 JWTs. Each request revalidates account activation, role, and player linkage against the database.
- Authorization policies distinguish Admin, Coach, Player, and Admin-or-Coach operations.
- EF Core LINQ targets PostgreSQL in production; tests use SQLite.
- Android uses Retrofit/OkHttp and now stores its JWT in Android Keystore-backed encrypted preferences.

## 3. Threat model

Protected assets include Admin/Coach/Player accounts, hashes, JWTs, cookies, contact/disability/attendance data, registrations, QR secrets, database/JWT deployment secrets, uploads, backups, and administrative settings.

Relevant actors are anonymous attackers, malicious Players or Coaches, compromised accounts, bots, credential-stuffing attackers, and request-tampering/IDOR attackers. Primary trust boundaries are browser-to-MVC, Android-to-API, application-to-PostgreSQL, Railway-to-environment configuration, and uploaded bytes-to-server storage.

## 4. Findings before fixes

| ID | Severity | Component and attack scenario | Impact / safe validation | Remediation | Residual risk |
|---|---|---|---|---|---|
| F-01 | HIGH | Report uploads accepted arbitrary types and used an original filename beneath public static storage. An Admin account or compromised Admin could upload active content and share a direct path. | Confirmed by code inspection; no live payload execution was attempted. | Added size, extension, content-signature checks, generated names, safe display names, and an Admin-only static-file gate. | Files remain on local/Railway ephemeral storage and should move to private object storage with malware scanning for a production service. |
| F-02 | HIGH | JSON backups containing player/contact information were written under `wwwroot/uploads/backups` with predictable timestamp names. | Anonymous static-file reachability was confirmed from middleware order and is regression-tested at the route gate. | Authentication now runs before static files and non-Admin access to backup/report paths returns 404. | Backups are not encrypted at rest and retention is not implemented. |
| F-03 | HIGH | Android stored JWT and identity data in plaintext, backup-eligible `SharedPreferences`. | Confirmed by source review; no device extraction was attempted. | Migrated to Android Keystore-backed `EncryptedSharedPreferences`, deletes the legacy plaintext store, disables backup, and excludes shared preferences from extraction/transfer rules. | A compromised/rooted device or in-process compromise can still access active credentials. Existing users must sign in once after upgrade. |
| F-04 | MEDIUM | Login, registration, general API, sensitive account actions, and QR check-in had no throttling. | Repeated invalid login was safely exercised in integration tests. | Added fixed-window, partitioned ASP.NET Core rate limits with HTTP 429 and `Retry-After`. | Distributed deployments need a shared limiter for globally consistent limits; account lockout/MFA remain future work. |
| F-05 | MEDIUM | Any authenticated Coach could revoke another Coach's QR session by changing the numeric session ID. | Safely validated by an integration test using two Coach identities. | Coach revocation now requires session ownership; Admin retains incident-management override. | Coaches can still create sessions for events they are authorized to manage, as intended. |
| F-06 | MEDIUM | Logo/photo validation trusted extensions; SVG logo upload allowed active content and Coach photos lacked a size limit. | Confirmed by source review and spoofed-image tests. | Removed SVG, capped images at 2 MB, checked JPEG/PNG/WebP signatures, and retained server-generated names. | Signature checks are deliberately lightweight, not full image decoding or malware scanning. |
| F-07 | MEDIUM | JWT startup accepted a weak signing key and token lifetime was controller-clamped as high as eight hours. | Confirmed by configuration path review. | Production startup now requires at least 32 key bytes and a configured lifetime of 5-60 minutes; the existing default is 60 minutes. | Secret rotation and key versioning are operational responsibilities. |
| F-08 | MEDIUM | MVC cookie security attributes were partly implicit and production security headers were absent. | Confirmed by source review; headers are now integration-tested. | Explicit HttpOnly, SameSite=Lax, production Secure, CSP, `nosniff`, referrer policy, permissions policy, HSTS, and clickjacking protection were added. | CSP permits inline script/style because current Razor views use them. Nonces/hashes are a future improvement. |
| F-09 | MEDIUM | Public registration/login fields and total request bodies allowed unnecessarily large input. | Oversized registration fields are regression-tested. | Added DTO limits, 12 MB request/multipart caps, and 10 MB report limits. | Several legacy MVC domain fields still need comprehensive database-aligned maximum lengths. |
| F-10 | LOW | `Reports/Edit` binds attachment metadata fields. | The action explicitly copies only title/type/status/date, so tampered file paths were not exploitable. | Existing explicit assignment retained; generated paths are used for new files. | Replace remaining domain-model form binding with dedicated view models over time. |
| F-11 | INFORMATIONAL | No CORS policy is enabled. | Source review. Native Android does not require browser CORS. | No change; deliberately did not broaden origins. | Add an allow-list only if a separate browser frontend is deployed. |

## 5. Authorization, IDOR, injection, XSS, and CSRF results

- Every API controller is JWT-scoped or explicitly anonymous for login/Player registration. Player-owned profile, dashboard, attendance history, event registration, cancellation, and QR check-in derive ownership from JWT claims and database revalidation rather than request-supplied player IDs.
- Coach cannot access Settings, account provisioning, player deletion/export, Reports, Coaches administration, or Admin registration approval. Admin-only and negative-role tests cover these boundaries.
- EF Core LINQ is used for application queries. No string-concatenated SQL, `FromSqlRaw`, or `ExecuteSqlRaw` injection surface was found.
- Razor's default encoding is used. `Html.Raw` occurrences serialize server-created chart objects as JSON; no intentional user-supplied HTML rendering was found. SVG upload was removed to eliminate the clearest stored active-content path.
- State-changing MVC actions inspected use `[ValidateAntiForgeryToken]`; JWT Bearer API actions correctly do not use cookie-CSRF tokens. A critical Admin write without a token is integration-tested to return 400.
- Website login does not honor an external `returnUrl`; role destinations are server-derived and an external redirect attempt is tested.

## 6. QR and registration security

QR tokens use 32 cryptographically random bytes, are stored only as SHA-256 hashes, expire after 15 minutes, support revocation, require event registration, and rely on a unique attendance database constraint. Player identity comes only from the validated JWT. Tests cover expired, revoked, duplicate, injected-player-ID, and cross-Coach revocation cases.

Public Player registration uses an explicit DTO, ignores role/activation/player-ID over-posting, creates only inactive Player accounts, normalizes email, and relies on a unique normalized-email database index for race resistance. Admin approval remains required.

## 7. Exact rate limits

- Login: 20 requests/IP/minute.
- Player registration: 5 requests/IP/hour.
- Sensitive account/Admin and QR session management: 30 requests/authenticated account/minute (IP fallback).
- QR check-in: 60 requests/authenticated account/minute (IP fallback), allowing normal team throughput.
- General API: 120 requests/authenticated account or anonymous IP/minute.

These in-memory limits protect a single application instance. Use a distributed gateway/limiter if Railway runs multiple replicas.

## 8. Tests

The pre-existing suite covers authentication authority, inactive accounts, role injection, stale/disabled JWTs, Player/Coach/Admin authorization, duplicate email handling, public/private website separation, and private Player-field minimization. PASS 3 adds real middleware/action tests for rate limiting, headers, CSRF, external return URLs, oversized registration input, private uploads, QR ownership/expiry/revocation/duplicate/ownership injection, and upload content/path validation.

## 9. Secret and dependency review

No committed private key, production JWT value, Railway credential, or password-bearing production connection string was found in the tracked working tree. Documentation contains placeholders only. User Secrets and Railway environment variables remain the intended secret stores. If a secret was ever committed outside the current tree, rotate it and audit history separately.

The first NuGet audit found high-severity GHSA-2m69-gcr7-jv3q in the test-only transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.6 package. The test project now pins the compatible 2.1.13 bundle; the repeat audit reports no known vulnerable packages in either project. Production uses PostgreSQL and was not affected by the SQLite test dependency. Android dependencies were reviewed without a broad upgrade; the added AndroidX Security Crypto library is scoped to encrypted session storage.

## 10. Residual risks and production recommendations

1. Configure Railway with a strong unique JWT key, exact issuer/audience, `Jwt__AccessTokenMinutes=60`, PostgreSQL TLS as supported, and `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; verify the platform strips client-supplied forwarded headers and that HTTPS redirect/secure cookies work in staging. Prefer explicit trusted proxy ranges if Railway provides stable ranges.
2. Restrict `AllowedHosts` to production hostnames rather than `*`.
3. Set `PARAVOLLEY_RELEASE_API_BASE_URL` to the final HTTPS Railway URL; the checked-in invalid placeholder intentionally prevents accidental production traffic.
4. Move reports/backups to private durable object storage with authorization, encryption, retention, antivirus scanning, and backups. Railway's local filesystem is ephemeral.
5. Add MFA for Admins, breached-password checks, security-event/audit logging without PII/secrets, and a shared/distributed rate limiter before higher-risk public use.
6. Replace CSP inline allowances with nonces/hashes and finish dedicated MVC input view models/maximum lengths.
7. Perform staging DAST and dependency scanning in CI; this source audit is not a substitute for infrastructure testing.

No database migration is required by PASS 3. No production data is modified or deleted. No commit or push is performed.
