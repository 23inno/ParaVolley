using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;
using SportsManagementMVC.Services;

namespace SportsManagementMVC.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
[Route("Attendance/Inline")]
public sealed class AttendanceInlineController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceNotificationService _attendanceNotifications;

    public AttendanceInlineController(
        ApplicationDbContext context,
        AttendanceNotificationService attendanceNotifications)
    {
        _context = context;
        _attendanceNotifications = attendanceNotifications;
    }

    [HttpGet("Form")]
    public async Task<IActionResult> Form(
        CancellationToken cancellationToken = default)
    {
        await PopulateDropdownsAsync(cancellationToken);

        return PartialView(
            "~/Views/Attendance/_RecordAttendanceCard.cshtml",
            new Attendance
            {
                Date = DateTime.Today,
                Status = AttendanceStatus.Present
            });
    }

    [HttpPost("Record")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(
        [Bind("PlayerId,EventId,Date,Status")] Attendance attendance,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Please select a player, event, date and attendance status.";

            return Redirect("/Attendance#record-attendance-card");
        }

        var playerExists = await _context.Players
            .AsNoTracking()
            .AnyAsync(
                player => player.Id == attendance.PlayerId,
                cancellationToken);

        var eventExists = await _context.Events
            .AsNoTracking()
            .AnyAsync(
                eventItem => eventItem.Id == attendance.EventId,
                cancellationToken);

        if (!playerExists || !eventExists)
        {
            TempData["Error"] =
                "The selected player or event could not be found.";

            return Redirect("/Attendance#record-attendance-card");
        }

        var before = await _attendanceNotifications.GetSnapshotAsync(
            attendance.PlayerId,
            cancellationToken);

        attendance.EntryMethod = AttendanceEntryMethod.Manual;
        _context.Attendances.Add(attendance);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (DatabaseConflictClassifier.IsUniqueViolation(
                exception,
                _context.Database))
        {
            TempData["Error"] =
                "Attendance has already been recorded for this player and event.";

            return Redirect("/Attendance#record-attendance-card");
        }

        await _attendanceNotifications.NotifyIfCrossedBelowAsync(
            attendance.PlayerId,
            before,
            cancellationToken);

        TempData["Success"] =
            "Attendance record was added successfully.";

        return Redirect("/Attendance#record-attendance-card");
    }

    private async Task PopulateDropdownsAsync(
        CancellationToken cancellationToken)
    {
        ViewBag.PlayerId = new SelectList(
            await _context.Players
                .AsNoTracking()
                .OrderBy(player => player.Name)
                .Take(500)
                .ToListAsync(cancellationToken),
            "Id",
            "Name");

        ViewBag.EventId = new SelectList(
            await _context.Events
                .AsNoTracking()
                .OrderByDescending(eventItem => eventItem.Date)
                .ThenBy(eventItem => eventItem.Title)
                .Take(250)
                .ToListAsync(cancellationToken),
            "Id",
            "Title");
    }
}
