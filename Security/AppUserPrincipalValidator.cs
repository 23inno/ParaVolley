using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;

namespace SportsManagementMVC.Security
{
    public sealed class AppUserPrincipalValidator
    {
        private readonly ApplicationDbContext _context;

        public AppUserPrincipalValidator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsCurrentAsync(
            ClaimsPrincipal? principal,
            CancellationToken cancellationToken = default)
        {
            var idValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idValue, out var appUserId))
            {
                return false;
            }

            var storedUser = await _context.AppUsers
                .AsNoTracking()
                .Where(user => user.Id == appUserId)
                .Select(user => new
                {
                    user.IsActive,
                    user.Email,
                    user.Role,
                    user.PlayerId
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (storedUser == null || !storedUser.IsActive)
            {
                return false;
            }

            var claimedRole = principal?.FindFirstValue(ClaimTypes.Role);
            var claimedEmail = principal?.FindFirstValue(ClaimTypes.Email);
            var claimedPlayerId = principal?.FindFirstValue("playerId");

            if (!string.Equals(
                    claimedRole,
                    storedUser.Role.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    claimedEmail,
                    storedUser.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return storedUser.PlayerId.HasValue
                ? claimedPlayerId == storedUser.PlayerId.Value.ToString()
                : string.IsNullOrEmpty(claimedPlayerId);
        }
    }
}
