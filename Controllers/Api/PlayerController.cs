using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models.Api;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/player")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Player"
    )]
    public class PlayerController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PlayerController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("me")]
        public async Task<ActionResult<PlayerProfileResponse>> GetMyProfile()
        {
            var playerIdValue = User.FindFirstValue("playerId");

            if (!int.TryParse(playerIdValue, out var playerId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid player account."
                });
            }

            var player = await _db.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            return Ok(new PlayerProfileResponse
            {
                Id = player.Id,
                Name = player.Name,
                Position = player.Position,
                Team = player.Team,
                Status = player.Status.ToString(),
                Age = player.Age,
                Matches = player.Matches,
                Email = player.Email,
                Phone = player.Phone,
                Disability = player.Disability
            });
        }
    }
}