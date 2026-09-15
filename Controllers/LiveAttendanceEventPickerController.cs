using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
[Route("Attendance/Live/EventPickerData")]
public sealed class LiveAttendanceEventPickerController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LiveAttendanceEventPickerController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Events
            .AsNoTracking()
            .Where(eventItem => eventItem.Status != EventStatus.Cancelled)
            .OrderByDescending(eventItem => eventItem.Date)
            .ThenBy(eventItem => eventItem.Title)
            .Take(500)
            .Select(eventItem => new
            {
                eventItem.Id,
                eventItem.Title,
                eventItem.Date,
                eventItem.Type
            })
            .ToListAsync(cancellationToken);

        var events = rows.Select(eventItem => new
        {
            id = eventItem.Id,
            title = eventItem.Title,
            date = eventItem.Date.ToString("yyyy-MM-dd"),
            type = eventItem.Type.ToString()
        });

        return Ok(events);
    }
}
