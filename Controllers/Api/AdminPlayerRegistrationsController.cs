using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
using SportsManagementMVC.Infrastructure;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/admin/player-registrations")]
    [Authorize(
        AuthenticationSchemes =
            JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin"
    )]
    [EnableRateLimiting("sensitive")]
    public class AdminPlayerRegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminPlayerRegistrationsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<
            IEnumerable<PendingPlayerRegistrationDto>>>
            GetPendingRegistrations(
                int page = 1,
                int pageSize = Paging.DefaultApiPageSize,
                CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);
            pageSize = Paging.PageSize(pageSize, Paging.MaximumApiPageSize);
            var registrations = await _context.AppUsers
                .AsNoTracking()
                .Include(user => user.Player)
                .Where(user =>
                    user.Role == AppUserRole.Player &&
                    !user.IsActive &&
                    user.PlayerId.HasValue &&
                    user.Player != null &&
                    user.Player.Status == PlayerStatus.Inactive)
                .OrderBy(user => user.Player!.Name)
                .Select(user =>
                    new PendingPlayerRegistrationDto
                    {
                        PlayerId = user.Player!.Id,
                        AppUserId = user.Id,
                        Name = user.Player.Name,
                        Email = user.Email,
                        Phone = user.Player.Phone,
                        Position = user.Player.Position,
                        Team = user.Player.Team,
                        Age = user.Player.Age,
                        Disability = user.Player.Disability
                    })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(registrations);
        }

        [HttpPost("{playerId:int}/approve")]
        public async Task<IActionResult> Approve(
            int playerId,
            CancellationToken cancellationToken)
        {
            var user = await _context.AppUsers
                .AsNoTracking()
                .Include(appUser => appUser.Player)
                .FirstOrDefaultAsync(appUser =>
                    appUser.Role == AppUserRole.Player &&
                    appUser.PlayerId == playerId,
                    cancellationToken);

            if (user == null || user.Player == null)
            {
                return NotFound(new
                {
                    message =
                        "The pending player registration could not be found."
                });
            }

            if (user.IsActive &&
                user.Player.Status == PlayerStatus.Active)
            {
                return Conflict(new
                {
                    message =
                        "This player account has already been approved."
                });
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            var activatedAccounts = await _context.AppUsers
                .Where(appUser =>
                    appUser.Id == user.Id &&
                    !appUser.IsActive)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        appUser => appUser.IsActive,
                        true),
                    cancellationToken);

            if (activatedAccounts != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new
                {
                    message = "This player account has already been approved or changed."
                });
            }

            var activatedPlayers = await _context.Players
                .Where(player =>
                    player.Id == playerId &&
                    player.Status == PlayerStatus.Inactive)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        player => player.Status,
                        PlayerStatus.Active),
                    cancellationToken);

            if (activatedPlayers != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new
                {
                    message = "This player registration was changed by another request."
                });
            }

            await transaction.CommitAsync(cancellationToken);

            return Ok(new
            {
                message =
                    "Player registration approved successfully.",
                playerId = user.Player.Id,
                email = user.Email
            });
        }

        [HttpPost("{playerId:int}/reject")]
        public async Task<IActionResult> Reject(
            int playerId,
            CancellationToken cancellationToken)
        {
            var user = await _context.AppUsers
                .Include(appUser => appUser.Player)
                .FirstOrDefaultAsync(appUser =>
                    appUser.Role == AppUserRole.Player &&
                    appUser.PlayerId == playerId,
                    cancellationToken);

            if (user == null || user.Player == null)
            {
                return NotFound(new
                {
                    message =
                        "The pending player registration could not be found."
                });
            }

            if (user.IsActive ||
                user.Player.Status == PlayerStatus.Active)
            {
                return Conflict(new
                {
                    message =
                        "An active player account cannot be rejected through the pending-registration endpoint."
                });
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            _context.AppUsers.Remove(user);
            _context.Players.Remove(user.Player);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new
            {
                message =
                    "Player registration rejected successfully."
            });
        }
    }
}
