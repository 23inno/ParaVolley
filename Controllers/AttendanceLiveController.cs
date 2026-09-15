using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
[Route("Attendance/Live")]
public sealed class AttendanceLiveController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceLiveController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("Recent")]
    public async Task<IActionResult> Recent(
        CancellationToken cancellationToken = default)
    {
        var records = await _context.Attendances
            .AsNoTracking()
            .Include(item => item.Player)
            .Include(item => item.Event)
            .OrderByDescending(item => item.Id)
            .Take(12)
            .ToListAsync(cancellationToken);

        return Json(records.Select(item => new
        {
            item.Id,
            PlayerName = item.Player?.Name ?? "Unknown player",
            EventTitle = item.Event?.Title ?? "Unknown event",
            Date = item.Date.ToString("yyyy-MM-dd"),
            DateLabel = item.Date.ToString("dd MMM yyyy"),
            Status = item.Status.ToString()
        }));
    }

    [HttpGet("Records")]
    public async Task<IActionResult> Records(
        string? search,
        int? eventId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Include(item => item.Player)
            .Include(item => item.Event)
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(item => item.EventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();

            query = query.Where(item =>
                (item.Player != null &&
                 item.Player.Name.ToLower().Contains(normalized)) ||
                (item.Event != null &&
                 item.Event.Title.ToLower().Contains(normalized)));
        }

        var records = await query
            .OrderByDescending(item => item.Date)
            .ThenBy(item => item.Player != null ? item.Player.Name : string.Empty)
            .ThenByDescending(item => item.Id)
            .Take(1000)
            .ToListAsync(cancellationToken);

        return Json(records.Select(item => new
        {
            item.Id,
            PlayerName = item.Player?.Name ?? "Unknown player",
            EventTitle = item.Event?.Title ?? "Unknown event",
            Date = item.Date.ToString("yyyy-MM-dd"),
            DateLabel = item.Date.ToString("dddd, dd MMMM yyyy"),
            Status = item.Status.ToString()
        }));
    }
}
