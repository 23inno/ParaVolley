using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class ContactMessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactMessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ContactMessages
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var messages = await _context.ContactMessages
                .AsNoTracking()
                .OrderBy(m => m.IsRead)
                .ThenByDescending(m => m.SubmittedAtUtc)
                .ToListAsync(cancellationToken);

            return View(messages);
        }

        // GET: ContactMessages/Details/5
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(
                    m => m.Id == id,
                    cancellationToken);

            if (message == null)
            {
                return NotFound();
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return View(message);
        }
    }
}