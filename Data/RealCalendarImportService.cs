using System.Data;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data;

public enum CalendarImportDisposition
{
    Created,
    SkippedDuplicate,
    RejectedInvalid,
    RequiresConfirmation
}

public sealed record RealCalendarSourceEntry(
    string SourceDate,
    string Title,
    EventType Type,
    DateTime? StartDate,
    string? Location,
    string Description,
    bool VenueRequiresConfirmation = false);

public sealed record CalendarImportItem(
    RealCalendarSourceEntry Source,
    CalendarImportDisposition Disposition,
    string Reason);

public sealed class RealCalendarImportResult
{
    public required bool IsDryRun { get; init; }
    public required IReadOnlyList<CalendarImportItem> Items { get; init; }

    public int Created => Items.Count(item =>
        item.Disposition == CalendarImportDisposition.Created);
    public int SkippedDuplicate => Items.Count(item =>
        item.Disposition == CalendarImportDisposition.SkippedDuplicate);
    public int RejectedInvalid => Items.Count(item =>
        item.Disposition == CalendarImportDisposition.RejectedInvalid);
    public int RequiresConfirmation => Items.Count(item =>
        item.Disposition == CalendarImportDisposition.RequiresConfirmation);
}

public sealed class RealCalendarImportService
{
    private const int MaximumDescriptionLength = 4000;
    private readonly ApplicationDbContext _context;

    public RealCalendarImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public static IReadOnlyList<RealCalendarSourceEntry> OfficialCalendar { get; } =
    [
        new(
            "20–22 February 2026",
            "Women's National Team Training Camp",
            EventType.Practice,
            new DateTime(2026, 2, 20),
            "Limpopo – Makhado CPDC Indoor Sports Hall",
            "Source date range: 20–22 February 2026."),
        new(
            "March 2026 — exact date not supplied",
            "Local Community Clubs Training",
            EventType.Practice,
            null,
            null,
            "Formation phase. Recruit and select potential players from Ehlanzeni, " +
            "Gert Sibande and Nkangala District. Community outreach to promote " +
            "ParaVolley in communities. The source references Ligwalagwala FM, " +
            "Radio Bushbuckridge and Emalahleni FM; these are not treated as venues."),
        new(
            "TBD",
            "District Training Camp (3 days) — Nkangala launch",
            EventType.Practice,
            null,
            null,
            "Launching of the Nkangala Community local club."),
        new(
            "20–22 March 2026",
            "National Sitting Volleyball Training Camp",
            EventType.Practice,
            new DateTime(2026, 3, 20),
            "Polokwane – Ngoako Ramatlhodi Indoor Sports Complex",
            "Source date range: 20–22 March 2026."),
        new(
            "TBD",
            "Coaching Clinics and Referee Courses",
            EventType.Workshop,
            null,
            null,
            "Exact date and venue were not supplied."),
        new(
            "TBD",
            "Development Programmes rollout to community clubs",
            EventType.Practice,
            null,
            null,
            "Exact date and venue were not supplied."),
        new(
            "18–20 March 2026",
            "District Training Camp (3 days) — Ehlanzeni",
            EventType.Practice,
            new DateTime(2026, 3, 18),
            "To be confirmed",
            "District: Ehlanzeni. Source date range: 18–20 March 2026. " +
            "Venue is to be confirmed.",
            true),
        new(
            "April 2026 — exact date not supplied",
            "Local Community Clubs Training Sessions — April",
            EventType.Practice,
            null,
            null,
            "Practice by community clubs per district. Community outreach."),
        new(
            "17–19 April 2026",
            "National Sitting Volleyball Training Camp",
            EventType.Practice,
            new DateTime(2026, 4, 17),
            "Polokwane – Ngoako Ramatlhodi Indoor Sports Complex",
            "Source date range: 17–19 April 2026."),
        new(
            "TBD",
            "District Training Camp (3 days) — Gert Sibande launch",
            EventType.Practice,
            null,
            null,
            "Launching at the Gert Sibande District."),
        new(
            "23–26 April 2026",
            "Women's National Team — South Africa Sitting Volleyball Championships",
            EventType.Tournament,
            new DateTime(2026, 4, 23),
            "Bulawayo, Zimbabwe",
            "Source date range: 23–26 April 2026."),
        new(
            "May 2026 — exact date not supplied",
            "Local Community Clubs Training Sessions — May",
            EventType.Practice,
            null,
            null,
            "Practice/training session. Community outreach to promote ParaVolley " +
            "in communities."),
        new(
            "16 May 2026",
            "District Tournament",
            EventType.Tournament,
            new DateTime(2026, 5, 16),
            null,
            "Inter-district tournament – Ehlanzeni District."),
        new(
            "30 May 2026",
            "Preparation for National Competition / Training Camp",
            EventType.Practice,
            new DateTime(2026, 5, 30),
            null,
            "Source wording: Preps For National Competition, Middleburg/Host/ " +
            "training Camp - view the venue and National team pre-Championship " +
            "training session – Nkangala District."),
        new(
            "June 2026 — exact date not supplied",
            "Local Community Clubs Training Sessions — June",
            EventType.Practice,
            null,
            "Bushbuckridge, Nkomazi",
            "Community outreach to promote ParaVolley in communities."),
        new(
            "27 June 2026",
            "District Trials",
            EventType.Practice,
            new DateTime(2026, 6, 27),
            "Ehlanzeni District",
            "District trials. Mapped to Practice because the source describes a " +
            "selection/development activity, not a scored tournament."),
        new(
            "11 July 2026",
            "Provincial Championships",
            EventType.Tournament,
            new DateTime(2026, 7, 11),
            "Middleburg",
            "Mpumalanga Championships in Middleburg. Source spelling preserved."),
        new(
            "8 August 2026",
            "Provincial Team Selection / Training Camp (3 days)",
            EventType.Practice,
            new DateTime(2026, 8, 8),
            "Nkangala District",
            "Training camp in preparation for National Championships."),
        new(
            "3–6 October 2026",
            "South Africa Sitting Volleyball Championships",
            EventType.Tournament,
            new DateTime(2026, 10, 3),
            "Middleburg",
            "Participate in National Championships. Mpumalanga hosting. " +
            "Source date range: 3–6 October 2026. Source spelling preserved.")
    ];

    public Task<RealCalendarImportResult> DryRunAsync(
        CancellationToken cancellationToken = default) =>
        AnalyzeAsync(OfficialCalendar, true, cancellationToken);

    public async Task<RealCalendarImportResult> ImportAsync(
        bool backupConfirmed,
        CancellationToken cancellationToken = default) =>
        await ImportAsync(OfficialCalendar, backupConfirmed, cancellationToken);

    public async Task<RealCalendarImportResult> ImportAsync(
        IReadOnlyList<RealCalendarSourceEntry> entries,
        bool backupConfirmed,
        CancellationToken cancellationToken = default)
    {
        if (!backupConfirmed)
        {
            throw new InvalidOperationException(
                "A database backup must be confirmed before importing real calendar data.");
        }

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var result = await AnalyzeAsync(entries, false, cancellationToken);
            var events = result.Items
                .Where(item =>
                    item.Disposition == CalendarImportDisposition.Created)
                .Select(item => MapEvent(item.Source))
                .ToList();

            _context.Events.AddRange(events);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<RealCalendarImportResult> AnalyzeAsync(
        IReadOnlyList<RealCalendarSourceEntry> entries,
        bool isDryRun,
        CancellationToken cancellationToken)
    {
        var items = new List<CalendarImportItem>(entries.Count);
        var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var validation = Validate(entry);
            if (validation != null)
            {
                items.Add(new CalendarImportItem(
                    entry,
                    validation.Value.Disposition,
                    validation.Value.Reason));
                continue;
            }

            var key = DuplicateKey(entry);
            if (!sourceKeys.Add(key) ||
                await ExistsAsync(entry, cancellationToken))
            {
                items.Add(new CalendarImportItem(
                    entry,
                    CalendarImportDisposition.SkippedDuplicate,
                    "An event with the same title, start date, type and location already exists."));
                continue;
            }

            items.Add(new CalendarImportItem(
                entry,
                CalendarImportDisposition.Created,
                isDryRun
                    ? "Validated and would be created; the dry run did not mutate the database."
                    : "Created from the verified calendar source."));
        }

        return new RealCalendarImportResult
        {
            IsDryRun = isDryRun,
            Items = items
        };
    }

    private static (CalendarImportDisposition Disposition, string Reason)?
        Validate(RealCalendarSourceEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Title) || entry.Title.Length > 150)
        {
            return (CalendarImportDisposition.RejectedInvalid,
                "The title is required and must not exceed 150 characters.");
        }

        if (!Enum.IsDefined(entry.Type))
        {
            return (CalendarImportDisposition.RejectedInvalid,
                "The event type is not supported by the current Event model.");
        }

        if (string.IsNullOrWhiteSpace(entry.Description) ||
            entry.Description.Length > MaximumDescriptionLength)
        {
            return (CalendarImportDisposition.RejectedInvalid,
                $"The description is required and must not exceed {MaximumDescriptionLength} characters.");
        }

        if (entry.StartDate == null)
        {
            return (CalendarImportDisposition.RequiresConfirmation,
                "An exact start date was not supplied; no date was invented.");
        }

        if (entry.VenueRequiresConfirmation ||
            string.IsNullOrWhiteSpace(entry.Location))
        {
            return (CalendarImportDisposition.RequiresConfirmation,
                "The current Event model requires a confirmed location; no venue was invented.");
        }

        return null;
    }

    private async Task<bool> ExistsAsync(
        RealCalendarSourceEntry entry,
        CancellationToken cancellationToken)
    {
        var title = entry.Title.Trim().ToLower();
        var location = entry.Location!.Trim().ToLower();
        return await _context.Events.AsNoTracking().AnyAsync(
            eventItem =>
                eventItem.Title.ToLower() == title &&
                eventItem.Date == entry.StartDate!.Value.Date &&
                eventItem.Type == entry.Type &&
                eventItem.Location.ToLower() == location,
            cancellationToken);
    }

    private static string DuplicateKey(RealCalendarSourceEntry entry) =>
        $"{entry.Title.Trim()}|{entry.StartDate:yyyy-MM-dd}|{entry.Type}|" +
        entry.Location!.Trim();

    private static Event MapEvent(RealCalendarSourceEntry entry)
    {
        var date = entry.StartDate!.Value.Date;
        return new Event
        {
            Title = entry.Title.Trim(),
            Date = date,
            Time = "Not supplied",
            Location = entry.Location!.Trim(),
            Type = entry.Type,
            Participants = 0,
            Status = date >= DateTime.Today
                ? EventStatus.Upcoming
                : EventStatus.Completed,
            Description = $"{entry.Description.Trim()} Source date: {entry.SourceDate}. " +
                "No event time, participant count, score or attendance was supplied."
        };
    }
}
