using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Models.Api;
using SportsManagementMVC.Security;

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
        public async Task<ActionResult<PlayerProfileResponse>> GetMyProfile(
            CancellationToken cancellationToken = default)
        {
            if (!TryGetPlayerId(out var playerId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid player account."
                });
            }

            var player = await _db.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == playerId,
                    cancellationToken);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            return Ok(await BuildProfileResponseAsync(
                player,
                cancellationToken));
        }

        [HttpPut("me")]
        public async Task<ActionResult<PlayerProfileResponse>> UpdateMyProfile(
            UpdatePlayerProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetPlayerId(out var playerId) ||
                !int.TryParse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var appUserId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid player account."
                });
            }

            var normalizedEmail = request.Email
                .Trim()
                .ToLowerInvariant();
            var phone = request.Phone.Trim();

            var player = await _db.Players
                .FirstOrDefaultAsync(
                    p => p.Id == playerId,
                    cancellationToken);

            var appUser = await _db.AppUsers
                .FirstOrDefaultAsync(
                    user =>
                        user.Id == appUserId &&
                        user.PlayerId == playerId &&
                        user.Role == AppUserRole.Player,
                    cancellationToken);

            if (player == null || appUser == null)
            {
                return Unauthorized(new
                {
                    message = "The linked player account could not be found."
                });
            }

            var emailUsedByAnotherAccount = await _db.AppUsers
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.Id != appUser.Id &&
                        user.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            var emailUsedByAnotherPlayer = await _db.Players
                .AsNoTracking()
                .AnyAsync(
                    other =>
                        other.Id != player.Id &&
                        other.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (emailUsedByAnotherAccount || emailUsedByAnotherPlayer)
            {
                return Conflict(new
                {
                    message = "That email address is already in use."
                });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                player.Age = request.Age;
                player.Email = normalizedEmail;
                player.Phone = phone;

                // The player's authentication account uses the same email.
                // Updating both records makes the edited email the next login email.
                appUser.Email = normalizedEmail;
                appUser.NormalizedEmail = normalizedEmail;

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Conflict(new
                {
                    message = "The profile could not be saved because the email address is already in use."
                });
            }

            return Ok(await BuildProfileResponseAsync(
                player,
                cancellationToken));
        }

        [HttpGet("me/photo")]
        public async Task<IActionResult> GetMyProfilePhoto(
            CancellationToken cancellationToken = default)
        {
            if (!TryGetPlayerId(out var playerId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid player account."
                });
            }

            var photo = await _db.PlayerProfilePhotos
                .AsNoTracking()
                .Where(item => item.PlayerId == playerId)
                .Select(item => new
                {
                    item.Data,
                    item.ContentType
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (photo == null)
            {
                return NotFound(new
                {
                    message = "No profile photo has been uploaded yet."
                });
            }

            return File(photo.Data, photo.ContentType);
        }

        [HttpPost("me/photo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadSecurity.ImageMaxBytes + 64 * 1024)]
        public async Task<ActionResult<PlayerProfileResponse>> UploadMyProfilePhoto(
            IFormFile photo,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetPlayerId(out var playerId))
            {
                return Unauthorized(new
                {
                    message = "The access token does not contain a valid player account."
                });
            }

            if (photo == null ||
                !await UploadSecurity.IsSafeImageAsync(
                    photo,
                    UploadSecurity.ImageMaxBytes))
            {
                return BadRequest(new
                {
                    message = "Choose a JPG, PNG, or WEBP image smaller than 2 MB."
                });
            }

            var player = await _db.Players
                .FirstOrDefaultAsync(
                    p => p.Id == playerId,
                    cancellationToken);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "The player profile could not be found."
                });
            }

            await using var stream = new MemoryStream();
            await photo.CopyToAsync(stream, cancellationToken);

            var extension = Path
                .GetExtension(photo.FileName)
                .ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var existing = await _db.PlayerProfilePhotos
                .FirstOrDefaultAsync(
                    item => item.PlayerId == playerId,
                    cancellationToken);

            if (existing == null)
            {
                existing = new PlayerProfilePhoto
                {
                    PlayerId = playerId
                };
                _db.PlayerProfilePhotos.Add(existing);
            }

            existing.Data = stream.ToArray();
            existing.ContentType = contentType;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(await BuildProfileResponseAsync(
                player,
                cancellationToken));
        }

        private bool TryGetPlayerId(out int playerId)
        {
            return int.TryParse(
                User.FindFirstValue("playerId"),
                out playerId);
        }

        private async Task<PlayerProfileResponse> BuildProfileResponseAsync(
            Player player,
            CancellationToken cancellationToken)
        {
            var hasProfilePhoto = await _db.PlayerProfilePhotos
                .AsNoTracking()
                .AnyAsync(
                    item => item.PlayerId == player.Id,
                    cancellationToken);

            return new PlayerProfileResponse
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
                Disability = player.Disability,
                HasProfilePhoto = hasProfilePhoto
            };
        }
    }
}
