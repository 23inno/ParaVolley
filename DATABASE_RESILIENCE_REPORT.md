# ParaVolley PASS 4 Database Resilience Report

## 1. Scope and evidence limits

This review covers the ASP.NET Core 8 website/API, EF Core 8.0.10, Npgsql EF provider 8.0.10, PostgreSQL model/migrations, request queries, concurrency controls, health checks, resource use, and controlled local concurrency. It preserves PASS 1-3 authentication and security behavior.

No load was sent to Railway. Measured load results below use ASP.NET Core `TestServer` and a file-backed SQLite integration database so that repeatable concurrent application behavior can be tested without production data. They validate application/request correctness under concurrency, not Railway network latency, PostgreSQL plan capacity, or a specific Railway database connection quota.

## 2. Current architecture

- `ApplicationDbContext` is registered with `AddDbContext`, whose default lifetime is Scoped. A request receives one context; contexts are not static, singleton, or shared across requests.
- Controllers inject the scoped context directly. There is no repository/service abstraction, but no lifetime violation was found.
- Production uses `UseNpgsql(connectionString)` with the connection string supplied by configuration/environment/user secrets. No tracked production connection string is present.
- EF/Npgsql opens connections when a command needs one. Commands/readers close after completion and the request scope disposes the context. Explicit transactions use `await using`, so their connections/transactions are disposed.
- No application code manually constructs `NpgsqlConnection`, `DbConnection`, or a permanent database connection. No connection leak was found.
- Startup creates a scope, applies migrations, and seeds idempotent bootstrap/demo data. The startup context is disposed with that scope.
- The backend makes no outbound HTTP requests, so `HttpClientFactory` is not applicable. Email uses disposed `SmtpClient`/`MailMessage` instances.

## 3. Connection pooling

The tracked configuration does not disable or override Npgsql pooling. Npgsql pooling therefore defaults to enabled, minimum pool size 0 and maximum pool size 100. These are driver defaults, not a claim that the Railway database plan permits 100 connections. See the [official Npgsql connection-string parameter documentation](https://www.npgsql.org/doc/connection-string-parameters).

A scoped EF context is not a permanent physical connection. The Npgsql data source leases a pooled connection for database work and returns it when the operation/context/transaction closes. Twenty requests therefore do not imply twenty permanently occupied connections, although twenty simultaneously executing database commands can require multiple leases.

No arbitrary pool enlargement was added. Before setting `Maximum Pool Size`, obtain the actual Railway/PostgreSQL connection limit and reserve capacity for migrations, operations and other clients. A multi-replica deployment must budget the pool per replica.

## 4. Async database access and cancellation

Request paths were already mostly asynchronous. Remaining synchronous EF existence checks in Player, Coach, Event, Match and Announcement edit conflict paths were converted to `AnyAsync`. Attendance dropdown queries are now asynchronous and bounded. Cancellation tokens were added to the highest-traffic public pages, dashboards, report aggregation, list APIs, attendance pages, health checks and backup streaming.

Synchronous seeding remains startup-only and does not reduce concurrent request throughput.

## 5. Query and N+1 findings

- No loop-issued EF query/N+1 pattern was found in the audited request paths.
- Read-only queries now use `AsNoTracking` more consistently.
- API/MVC list queries apply ordering, filtering and paging in SQL rather than after loading a whole table.
- The Admin dashboard action now uses 8 bounded SQL queries instead of 9; Player totals/active totals are one grouped aggregate. Including cookie revalidation, a normal Admin dashboard HTTP request performs 9 database queries.
- The Coach dashboard action now uses 5 bounded SQL queries instead of 6; attendance totals/present totals are one aggregate. Including cookie revalidation, a normal Coach dashboard HTTP request performs 6 database queries.
- The Player dashboard replaces loading all attendance rows with one grouped count query. Its action uses 6 bounded queries; JWT revalidation adds one indexed AppUser query.
- Report analytics no longer load complete Player, Match, Event, Attendance and Report tables into managed memory. Aggregates execute in SQL, report rows are paged, team groups are capped at 100 and attendance summaries at 50.
- The Attendance dashboard still performs rich in-memory chart calculations, but its input is explicitly capped to the most recent 5,000 attendance rows. A future analytics/reporting store is preferable if history substantially exceeds that window.
- Includes retained for attendance/event registration map only the required single-reference relationships; no large collection graph or Cartesian explosion was found.

## 6. Pagination and data growth

- MVC Players, Coaches, Events, Matches, Announcements, Reports and raw Attendance Records use 25-row server-side pages with filter-preserving Previous/Next navigation.
- Android-compatible array endpoints for events, matches, announcements, attendance history, event registrations and pending registrations retain the same JSON array shape. They accept optional `page` and `pageSize`, default to 100 and cap page size at 200.
- Public homepage sections remain intentionally capped (3 upcoming events, 3 announcements and 50 sponsors).
- QR session creation/revocation has no unbounded list endpoint.
- Export actions intentionally export the selected full dataset. They remain Admin-controlled operational actions and are a future streaming-export candidate for very large datasets.

API clients should adopt paging before datasets exceed the compatibility default. A future versioned API can return page metadata in a wrapper without breaking the current Android contract.

## 7. Index audit and migration

Existing useful indexes were preserved:

- unique `AppUser.NormalizedEmail` for login/provisioning/registration;
- unique `(PlayerId, EventId)` for event registration and attendance;
- unique QR `TokenHash`;
- foreign-key indexes for Event registrations, attendance and QR ownership/event joins;
- primary-key indexes used for AppUser JWT validation and entity lookup.

Migration `20260830124227_AddPerformanceIndexes` adds:

| Index | Query supported | Benefit | Tradeoff |
|---|---|---|---|
| `Players(Status)` | active/inactive dashboard/report counts | avoids full scans as Players grow | small write/storage overhead |
| `Events(Status, Date)` | upcoming-event filters ordered by date | filter/order support | Event writes update one extra index |
| `Matches(Status, Date)` | scheduled/completed dashboard queries | filter/order support | Match writes update one extra index |
| `Announcements(IsPinned, Date)` | pinned/recent ordering | improves recent-news reads | Announcement writes update one extra index |
| `Reports(Status, Date)` | report status filter/date ordering | improves report library reads | Report writes update one extra index |
| `Attendances(Date)` | recent attendance history/trends | range and recency support | Attendance inserts update one extra index |

Generated PostgreSQL SQL was validated. It contains only six `CREATE INDEX` statements plus migration history; no business rows are deleted or transformed. Index creation can briefly consume I/O/locks, so apply during a controlled deployment window.

## 8. Concurrency and data integrity

- Public Player registration uses a transaction and the unique normalized AppUser email constraint. Concurrent identical requests are tested: one returns 201, one 409, and exactly one Player/AppUser remains.
- Event registration and attendance rely on unique `(PlayerId, EventId)` constraints. Check-then-insert races are converted to safe 409 responses only for verified unique violations; unrelated database errors are no longer mislabeled as duplicates.
- Concurrent event registration, Coach attendance recording and QR check-in each produce one valid record plus one conflict in tests.
- Concurrent Coach provisioning relies on unique normalized email; two simultaneous requests create exactly one Coach account.
- Admin approval now uses conditional database updates inside a transaction. Only an inactive AppUser and inactive Player can transition; a competing request receives a conflict rather than silently repeating the transition.
- Admin rejection now deletes the pending linked records in one transaction/SaveChanges instead of two partially committable writes.
- No broad table locks were introduced.

PostgreSQL constraints, not application prechecks, are the final authority for duplicate prevention.

## 9. JWT database revalidation cost

Every authenticated JWT request performs exactly one AppUser projection by primary key in `AppUserPrincipalValidator`. It returns only activation, email, role and PlayerId. This indexed point lookup preserves immediate disable/role-change enforcement.

Normal API actions do not repeat the AppUser lookup. Redundant active-AppUser checks were removed from QR create/revoke because successful JWT validation already performs the same current-request check. Player-owned actions still query Player/event/business records as required. No long-lived authorization cache was added.

For representative reads:

- `/api/player/me`: 1 JWT AppUser lookup + 1 Player lookup;
- `/api/events`: 1 JWT AppUser lookup + 1 paged Event query;
- `/api/player/attendance`: 1 JWT AppUser lookup + 1 paged Attendance/Event query;
- Player dashboard: 1 JWT AppUser lookup + 6 bounded action queries.

This overhead is acceptable for the expected workload. The next optimization, if measurements later justify it, should be short-lived request-local reuse—not authorization caching that delays account disablement.

## 10. Report, memory and resource behavior

- Reports use SQL aggregates and bounded result lists.
- Report and backup downloads use `PhysicalFile` range-enabled streaming instead of `ReadAllBytesAsync`.
- Backup JSON is serialized asynchronously to a file from EF async streams rather than constructing all core tables plus one giant JSON string in memory.
- Upload streams remain disposed and PASS 3 size/content limits remain intact.
- No undisposed application stream or manual database connection was found.
- Announcement email fan-out remains sequential SMTP work in the request and may become slow with a large subscriber list; a durable background queue is a future scaling improvement.

## 11. Timeout and transient resilience

Npgsql command timeout is explicitly 30 seconds. This is a failure bound, not a substitute for query optimization.

Automatic `EnableRetryOnFailure` was evaluated but deliberately not enabled globally in this pass. Several endpoints perform non-idempotent inserts and explicit transactions; automatic replay can create ambiguous outcomes if the database committed but the acknowledgement was lost. Unique constraints protect several flows, but not every business creation action has an idempotency key. Safe future retries require execution-strategy-wrapped transactions and idempotency/verification for all replayable writes.

The application therefore fails visibly on an unhandled transient database outage instead of silently duplicating non-idempotent operations. Health readiness reports database loss to the platform.

## 12. Health checks

- `GET /health/live`: application process/routing liveness only.
- `GET /health/ready`: executes `Database.CanConnectAsync` and is healthy only when the database is reachable.
- Responses contain only `{"status":"Healthy"}` or `{"status":"Unhealthy"}`. Connection strings, database host, credentials, exception details and stack traces are not returned.

Railway should use `/health/live` for liveness and `/health/ready` for readiness if separate probe configuration is supported.

## 13. Startup migrations

Automatic `Database.Migrate()` remains practical for the current academic single-instance deployment. The startup scope is correctly disposed. Multiple replicas starting simultaneously can contend for migration DDL/migration-history work. Before multi-instance production scaling, move migrations to one controlled release/deployment step and start application replicas only after it succeeds.

## 14. Caching decision

No cache was added. Public homepage queries were already small and bounded, and the local 20-user probe did not justify cache invalidation complexity. Authorization/private data is never cached. Short TTL caching for public events, results, announcements and sponsors remains an optional future optimization after production telemetry shows repeated-read pressure.

## 15. Reproducible local load methodology

`LocalLoadProbeTests.ControlledLocalConcurrencyProbe_HasNoServerFailures` creates an isolated application host and file-backed test database, then runs:

1. 20 concurrent workers, each requesting `/`, `/Events`, `/Matches`, and `/Announcements` (80 requests).
2. 20 concurrent authenticated workers, each requesting `/api/player/me`, `/api/player/dashboard`, `/api/events`, and `/api/player/attendance` (80 requests). JWT database revalidation remains enabled.
3. A 50-concurrent-request public homepage burst.
4. Twenty-five small invalid login attempts to confirm throttling, followed by a readiness check.

The probe records status classes, average/median/p95/max request latency, throughput, and managed-memory delta. It does not bypass PASS 3 limits.

## 16. Actual measured results

Measured locally on 2026-08-30:

| Scenario | Total | Success | 429 | 5xx | Other failures | Avg | Median | p95 | Max | Requests/s |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Public 20 workers | 80 | 80 | 0 | 0 | 0 | 256.32 ms | 66.00 ms | 865.95 ms | 871.25 ms | 76.37 |
| Authenticated 20 workers | 80 | 80 | 0 | 0 | 0 | 108.29 ms | 47.23 ms | 351.56 ms | 352.70 ms | 178.67 |
| Public burst 50 | 50 | 50 | 0 | 0 | 0 | 81.69 ms | 82.40 ms | 117.55 ms | 125.28 ms | 387.48 |

Observed post-GC managed-memory delta across all three scenarios was +2,835,168 bytes. This single-run value is not a leak diagnosis. No application crash, HTTP 5xx, database connection error or pool-exhaustion symptom occurred.

Login probe: 25 invalid attempts produced 6 expected HTTP 429 responses; the application and database readiness endpoint remained healthy afterward.

## 17. Twenty-user conclusion

The evidence does not support the concern that approximately 20 concurrent users will inherently overload or crash this application. All 160 representative public/authenticated requests completed successfully under two 20-worker scenarios, with no 5xx or application crash, while JWT database revalidation was active. The architecture uses scoped contexts, async I/O and driver pooling rather than one permanent connection per user.

This is not a guarantee for every Railway plan or arbitrarily expensive dataset. Production PostgreSQL latency, connection quota, CPU, storage I/O and data volume were not measured locally.

## 18. Fifty-request burst conclusion

The controlled 50-request public burst completed 50/50 successfully, with zero 429, zero 5xx and no crash. Average latency was 81.69 ms and p95 was 117.55 ms in the local test environment.

## 19. Remaining scaling limits

1. The unknown Railway/PostgreSQL connection quota is the first infrastructure limit to verify before adding replicas or setting pool sizes.
2. Admin report/dashboard round trips and the 5,000-row Attendance analytics window are the clearest database/application hotspots as data grows.
3. Full CSV exports, manual full-data backup duration and SMTP subscriber fan-out remain operationally unbounded/long-running tasks even though memory use was reduced.
4. In-memory rate limiting is per application instance; multiple replicas require a gateway/distributed limiter.
5. Local TestServer/SQLite measurements do not replace PostgreSQL staging load tests, `EXPLAIN ANALYZE`, connection-pool telemetry or Railway memory/CPU metrics.
6. Automatic migrations should move out of application startup before multi-instance operation.

## 20. Railway considerations

- No mandatory new connection-string setting is introduced. Pooling should remain enabled.
- Do not set an arbitrary maximum pool size. First obtain the plan's actual connection allowance, divide it across replicas and reserve headroom.
- Configure liveness/readiness probes to the new endpoints.
- Apply the non-destructive index migration during controlled deployment and observe migration duration.
- Continue required PASS 3 forwarded-header, HTTPS, JWT secret and release URL configuration.
- Collect PostgreSQL active/waiting connection counts, slow queries, command timeouts, application CPU/memory and p95 latency before the next scaling change.

## 21. Verification summary

- `dotnet build`: succeeded with 0 warnings and 0 errors.
- Complete tests: 50 passed, 0 failed, 0 skipped.
- Concurrency tests: duplicate Player registration, event registration, attendance, QR check-in and Coach provisioning passed.
- Controlled load probe: passed; measurements above.
- PostgreSQL migration SQL generated successfully and inspected as non-destructive.
- Android contracts and files were not changed; no Android rebuild is required for PASS 4.
- No production data was modified. No commit or push was performed.
