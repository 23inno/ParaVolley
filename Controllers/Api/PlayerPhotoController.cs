using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Models.Api;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/player/me/photo")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Player"
    )]
    public class PlayerPhotoController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PlayerPhotoController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpDelete]
        public async Task<ActionResult<PlayerProfileResponse>> DeleteMyProfilePhoto(
            CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(User.FindFirstValue("playerId"), out var playerId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid player account."
                });
            }

            var player = await _db.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == playerId,
                    cancellationToken);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            var photo = await _db.PlayerProfilePhotos
                .FirstOrDefaultAsync(
                    item => item.PlayerId == playerId,
                    cancellationToken);

            if (photo != null)
            {
                _db.PlayerProfilePhotos.Remove(photo);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var profileDetails = await _db.PlayerProfileDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    details => details.PlayerId == playerId,
                    cancellationToken);

            var earliestRegistration = await _db.EventRegistrations
                .AsNoTracking()
                .Where(item => item.PlayerId == playerId)
                .Select(item => (DateTime?)item.RegisteredAtUtc)
                .MinAsync(cancellationToken);

            var earliestAttendance = await _db.Attendances
                .AsNoTracking()
                .Where(item => item.PlayerId == playerId)
                .Select(item => (DateTime?)item.Date)
                .MinAsync(cancellationToken);

            var joinedDate = profileDetails?.JoinedDate
                ?? (earliestRegistration ?? earliestAttendance ?? DateTime.UtcNow).Date;

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
                EmergencyContactName = profileDetails?.EmergencyContactName ?? string.Empty,
                EmergencyContactPhone = profileDetails?.EmergencyContactPhone ?? string.Empty,
                JoinedDate = joinedDate.ToString("yyyy-MM-dd"),
                Disability = player.Disability,
                HasProfilePhoto = false
            });
        }
    }
}
