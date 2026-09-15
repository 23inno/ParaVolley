using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
[Route("Attendance/Records")]
public sealed class AttendanceRecordsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceRecordsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? year,
        int? month,
        int? day,
        CancellationToken cancellationToken = default)
    {
        if (month.HasValue && !year.HasValue)
        {
            return RedirectToAction(nameof(Index));
        }

        if (day.HasValue && (!year.HasValue || !month.HasValue))
        {
            return RedirectToAction(
                nameof(Index),
                new { year, month });
        }

        if (month is < 1 or > 12)
        {
            return RedirectToAction(nameof(Index), new { year });
        }

        if (year is < 1900 or > 2200)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new AttendanceRecordsViewModel
        {
            SelectedYear = year,
            SelectedMonth = month,
            SelectedDay = day
        };

        var attendance = _context.Attendances.AsNoTracking();

        if (!year.HasValue)
        {
            var groups = await attendance
                .GroupBy(record => record.Date.Year)
                .Select(group => new
                {
                    Value = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(group => group.Value)
                .ToListAsync(cancellationToken);

            model.Years = groups
                .Select(group => new AttendanceArchiveGroup
                {
                    Value = group.Value,
                    Label = group.Value.ToString(),
                    RecordCount = group.Count
                })
                .ToList();

            return View(model);
        }

        if (!month.HasValue)
        {
            var groups = await attendance
                .Where(record => record.Date.Year == year.Value)
                .GroupBy(record => record.Date.Month)
                .Select(group => new
                {
                    Value = group.Key,
                    Count = group.Count()
                })
                .OrderBy(group => group.Value)
                .ToListAsync(cancellationToken);

            model.Months = groups
                .Select(group => new AttendanceArchiveGroup
                {
                    Value = group.Value,
                    Label = new DateTime(
                        year.Value,
                        group.Value,
                        1).ToString("MMMM"),
                    RecordCount = group.Count
                })
                .ToList();

            return View(model);
        }

        if (!day.HasValue)
        {
            var groups = await attendance
                .Where(record =>
                    record.Date.Year == year.Value &&
                    record.Date.Month == month.Value)
                .GroupBy(record => record.Date.Day)
                .Select(group => new
                {
                    Value = group.Key,
                    Count = group.Count()
                })
                .OrderBy(group => group.Value)
                .ToListAsync(cancellationToken);

            model.Days = groups
                .Select(group =>
                {
                    var date = new DateTime(
                        year.Value,
                        month.Value,
                        group.Value);

                    return new AttendanceArchiveGroup
                    {
                        Value = group.Value,
                        Label = date.ToString("dddd, d MMMM"),
                        RecordCount = group.Count
                    };
                })
                .ToList();

            return View(model);
        }

        DateTime selectedDate;
        try
        {
            selectedDate = new DateTime(
                year.Value,
                month.Value,
                day.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return RedirectToAction(
                nameof(Index),
                new { year, month });
        }

        var nextDate = selectedDate.AddDays(1);

        model.Records = await attendance
            .Include(record => record.Player)
            .Include(record => record.Event)
            .Where(record =>
                record.Date >= selectedDate &&
                record.Date < nextDate)
            .OrderBy(record => record.Event != null
                ? record.Event.Title
                : string.Empty)
            .ThenBy(record => record.Player != null
                ? record.Player.Name
                : string.Empty)
            .ThenBy(record => record.Id)
            .ToListAsync(cancellationToken);

        return View(model);
    }
}
