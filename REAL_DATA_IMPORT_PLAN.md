# ParaVolley PASS 5 Real Data Import Plan

## 1. Source and scope

The only real business source used is the client-supplied **2025/26 ParaVolley Mpumalanga Calendar of Activities**, containing 19 activities between February and October 2026. No roster, score, attendance, sponsor, Coach, contact, time, or participant-count data was supplied, so none is fabricated.

This pass does not connect to or mutate Railway. No local real-data import was run. The implementation provides a read-only dry run and a backup-gated transactional Development-only import path.

## 2. Current seeders and classification

### A. Technical/bootstrap data retained

- `AppUserSeeder` remains authoritative for authentication bootstrap:
  - Development-only Player account when a Development password is configured.
  - Development-only Coach account when a Development password is configured.
  - Admin bootstrap only in Development or when `SeedUsers:BootstrapAdminEnabled=true`; an explicit password remains mandatory.
- EF Core migrations and authentication/security records are untouched.
- Integration-test accounts in `AuthenticationWebApplicationFactory` remain isolated test-only records.

### B. Development/test-only data

- The complete legacy `DbInitializer` sample dataset is now gated by `SeedData:EnableDemoData`.
- The tracked production default is `false`; `appsettings.Development.json` explicitly enables it for local Development.
- Tests create their own isolated SQLite records and do not depend on production startup seeding.

### C. Fake/demo business data found

The legacy initializer contains invented:

- eight Players and eight Coaches;
- five Events (`Provincial Championship`, `Training Session`, `Regional Finals`, `Team Building Workshop`, `Inter-Provincial Match`);
- eight Matches, including invented opponents and scores;
- four Announcements, including an invented result/MVP;
- six Attendance records;
- five Sponsors;
- six Reports;
- legacy `UserProfile`/`SystemUser` display records;
- sample organisation, notification, appearance and backup-history records.

These no longer appear automatically in production. The source remains available behind the explicit Development demo flag.

### D. Existing possibly-real business data

Existing database rows cannot be classified safely from source code alone. The schema has no seed-source/import-source marker. Titles that resemble the legacy seed are not sufficient proof that a production row is disposable because an administrator may have edited or recreated it.

Therefore no current Event, Player, Coach, Match, Attendance, Announcement, Sponsor, Report, AppUser or settings row is automatically deleted or overwritten.

## 3. Parsed calendar and Event mapping

The existing `EventType` values are `Tournament`, `Practice`, `Match`, and `Workshop`; no enum/schema change is required.

| # | Activity | Source date | Mapping | Import disposition |
|---:|---|---|---|---|
| 1 | Women's National Team Training Camp | 20–22 Feb 2026 | Practice | Exact and confirmed; creatable |
| 2 | Local Community Clubs Training | March 2026 | Practice | Exact date/location confirmation required |
| 3 | District Training Camp — Nkangala launch | TBD | Practice | Date/location confirmation required |
| 4 | National Sitting Volleyball Training Camp | 20–22 Mar 2026 | Practice | Exact and confirmed; creatable |
| 5 | Coaching Clinics and Referee Courses | TBD | Workshop | Date/location confirmation required |
| 6 | Development Programmes rollout | TBD | Practice | Date/location confirmation required |
| 7 | District Training Camp — Ehlanzeni | 18–20 Mar 2026 | Practice | Venue confirmation required |
| 8 | Local Community Clubs Training Sessions | April 2026 | Practice | Exact date/location confirmation required |
| 9 | National Sitting Volleyball Training Camp | 17–19 Apr 2026 | Practice | Exact and confirmed; creatable |
| 10 | District Training Camp — Gert Sibande launch | TBD | Practice | Date/location confirmation required |
| 11 | Women's National Team — SA Sitting Volleyball Championships | 23–26 Apr 2026 | Tournament | Exact and confirmed; creatable |
| 12 | Local Community Clubs Training Sessions | May 2026 | Practice | Exact date/location confirmation required |
| 13 | District Tournament | 16 May 2026 | Tournament | Venue confirmation required |
| 14 | Preparation for National Competition / Training Camp | 30 May 2026 | Practice | Venue/source wording confirmation required |
| 15 | Local Community Clubs Training Sessions | June 2026 | Practice | Exact date confirmation required |
| 16 | District Trials | 27 Jun 2026 | Practice | Exact and confirmed; creatable |
| 17 | Provincial Championships | 11 Jul 2026 | Tournament | Exact and confirmed; creatable |
| 18 | Provincial Team Selection / Training Camp | 8 Aug 2026 | Practice | Exact and confirmed; creatable |
| 19 | SA Sitting Volleyball Championships | 3–6 Oct 2026 | Tournament | Exact and confirmed; creatable |

`District Trials` maps to `Practice` because the source describes selection/development and provides no scored tournament. Coaching/referee courses map to the existing `Workshop` type. Championships and the District Tournament map to `Tournament`. No calendar row maps to `Match`, because the source supplies no fixture opponents or scores.

There are 11 exact-date entries. Eight also have a sufficiently confirmed required location and can be represented safely by the current Event model. Four entries are month-only and four are TBD. Three exact-date entries still need venue/source confirmation. All 11 uncertain rows remain pending and receive no invented date or venue.

For safe exact rows, the single `Event.Date` stores the source start date and the complete source range remains in `Description`. `Time` is the explicit text `Not supplied`; participants are zero. Past dates are not labelled Upcoming. This is calendar status only and does not create a score, winner or attendance claim.

The spelling **Middleburg** is preserved exactly as supplied.

## 4. Validation, duplicates and no-overwrite behavior

Dry run validates all 19 entries without calling `SaveChanges` and reports:

- Created/creatable;
- Skipped duplicate;
- Rejected/invalid;
- Requires confirmation.

Validation checks title presence/150-character model limit, supported EventType, description presence/4,000-character import limit, exact start date, and the required confirmed location.

The deterministic duplicate key is normalized:

`Title + start date + EventType + Location`

The database is queried for this same meaningful identity. Duplicates are skipped; existing descriptions/status/participants are never updated. A serializable transaction encloses the final duplicate recheck and all inserts. Any save failure rolls the whole import back.

The import refuses to run unless backup confirmation is supplied. Production startup cannot run it: startup dry-run/import flags are additionally restricted to the Development environment.

## 5. Backup and cleanup strategy

Before any future import or cleanup against a real database:

1. Create and verify a database backup in the existing private backup location.
2. Retain the backup; do not place it under a public static path.
3. Run the dry run and archive its counts/list.
4. Review pending confirmations and duplicate results.
5. Enable the Development/local import only with explicit backup confirmation, or implement a separately approved controlled production release operation later.

No destructive cleanup is implemented. Because old demo rows have no trustworthy source marker, cleanup must stop at a proposed manual review. The known legacy seed identities listed in section 2 may be compared against a backup and current values, but there must be explicit approval before deleting individual records. Broad table deletion is prohibited. AppUsers and technical bootstrap accounts are outside any business-data cleanup.

Future seed/imported data should receive an immutable source identifier in a dedicated migration if automated cleanup provenance becomes necessary. That schema change is not required for the safe import implemented here.

## 6. Public website, roles and Android

- Public/Admin/Coach dashboard Upcoming queries now require both `Status=Upcoming` and `Date >= today`, preventing past calendar entries with stale status from appearing as upcoming.
- Admin retains normal Event management.
- Coach authorization is unchanged: imported Practice events are editable; imported Tournament events are forbidden.
- The Android `/api/events` response remains a JSON array with the existing fields and optional paging. No Android file or API DTO changed.

## 7. Configuration and operating procedure

Tracked defaults:

- `SeedData:EnableDemoData=false` in production/default configuration.
- `SeedData:EnableDemoData=true` in Development only.
- `SeedData:DryRunRealCalendar=false`.
- `SeedData:ImportRealCalendar=false`.
- `SeedData:RealCalendarBackupConfirmed=false`.

For a reviewed **local Development database only**, first take a backup, then temporarily enable dry run. Review the log counts. Only after approval enable import plus backup confirmation. Return both switches to false afterward. Do not use these flags on Railway; the application also refuses startup import outside Development.

## 8. Records actually imported

None. Tests imported into isolated temporary SQLite databases only. No local persistent database and no Railway database was mutated.

## 9. Records intentionally not imported

- All four month-only rows.
- All four TBD rows.
- Ehlanzeni District Training Camp until its venue is confirmed.
- District Tournament until its venue is confirmed.
- Preparation for National Competition / Training Camp until the source's venue wording is confirmed.
- All Matches/results, Players, Coaches, AppUsers, Attendance, Sponsors, Reports and Announcements, because the calendar supplies none of that verified data.

## 10. Outstanding client confirmations

1. Exact March date and confirmed venue for Local Community Clubs Training.
2. Date and venue for the Nkangala District Training Camp/club launch.
3. Date and venue for Coaching Clinics and Referee Courses.
4. Date and venue for Development Programmes rollout.
5. Venue for the 18–20 March Ehlanzeni District Training Camp.
6. Exact April date and confirmed venue for community-club sessions.
7. Date and venue for the Gert Sibande District Training Camp/launch.
8. Exact May date and confirmed venue for community-club sessions.
9. Venue for the 16 May District Tournament.
10. Confirmed venue interpretation for the 30 May preparation/training-camp source wording.
11. Exact June date for Bushbuckridge/Nkomazi community-club sessions.
