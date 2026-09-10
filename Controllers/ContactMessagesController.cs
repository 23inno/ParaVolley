using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Infrastructure;
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
        // GET: ContactMessages
        public async Task<IActionResult> Index(
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Paging.Page(page);

            var query = _context.ContactMessages
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var unreadCount = await query
                .CountAsync(
                    message => !message.IsRead,
                    cancellationToken);

            var readCount = totalCount - unreadCount;

            var pageCount = Paging.PageCount(
                totalCount,
                Paging.DefaultPageSize);

            page = Math.Min(page, pageCount);

            ViewBag.UnreadCount = unreadCount;
            ViewBag.ReadCount = readCount;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = pageCount;

            var messages = await query
                .OrderBy(message => message.IsRead)
                .ThenByDescending(message => message.SubmittedAtUtc)
                .Skip((page - 1) * Paging.DefaultPageSize)
                .Take(Paging.DefaultPageSize)
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