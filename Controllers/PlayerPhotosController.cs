using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOrCoach)]
    [Route("Players/ProfilePhoto")]
    public class PlayerPhotosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlayerPhotosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(
            int id,
            CancellationToken cancellationToken = default)
        {
            var photo = await _context.PlayerProfilePhotos
                .AsNoTracking()
                .Where(item => item.PlayerId == id)
                .Select(item => new
                {
                    item.Data,
                    item.ContentType
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (photo == null)
            {
                return NotFound();
            }

            return File(photo.Data, photo.ContentType);
        }
    }
}
