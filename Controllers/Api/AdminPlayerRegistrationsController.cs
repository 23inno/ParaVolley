using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Dtos;
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
            GetPendingRegistrations()
        {
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
                .ToListAsync();

            return Ok(registrations);
        }

        [HttpPost("{playerId:int}/approve")]
        public async Task<IActionResult> Approve(
            int playerId)
        {
            var user = await _context.AppUsers
                .Include(appUser => appUser.Player)
                .FirstOrDefaultAsync(appUser =>
                    appUser.Role == AppUserRole.Player &&
                    appUser.PlayerId == playerId);

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

            user.IsActive = true;
            user.Player.Status = PlayerStatus.Active;

            await _context.SaveChangesAsync();

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
            int playerId)
        {
            var user = await _context.AppUsers
                .Include(appUser => appUser.Player)
                .FirstOrDefaultAsync(appUser =>
                    appUser.Role == AppUserRole.Player &&
                    appUser.PlayerId == playerId);

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

            var player = user.Player;

            _context.AppUsers.Remove(user);

            await _context.SaveChangesAsync();

            _context.Players.Remove(player);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Player registration rejected successfully."
            });
        }
    }
}