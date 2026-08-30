using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;
using SportsManagementMVC.Security;

namespace SportsManagementMVC.Controllers
{
    public class SponsorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SponsorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Sponsors.AsNoTracking()
                .OrderBy(s => s.Tier).ThenBy(s => s.Name).ToListAsync());
        }

        // GET: Sponsors/Create
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult Create()
        {
            if (IsAjaxRequest())
            {
                return PartialView("_CreatePartial", new Sponsor());
            }
            return View(new Sponsor());
        }

        // POST: Sponsors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Create([Bind("Name,Tier")] Sponsor sponsor)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return PartialView("_CreatePartial", sponsor);
                }
                return View(sponsor);
            }

            _context.Add(sponsor);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sponsor \"{sponsor.Name}\" was added.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction("Index", "Announcements");
        }

        // GET: Sponsors/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_DetailsPartial", sponsor);
            }
            return View(sponsor);
        }

        // GET: Sponsors/Edit/5
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null) return NotFound();

            if (IsAjaxRequest())
            {
                return PartialView("_EditPartial", sponsor);
            }
            return View(sponsor);
        }

        // POST: Sponsors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Tier")] Sponsor input)
        {
            if (id != input.Id) return NotFound();

            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null) return NotFound();

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return PartialView("_EditPartial", input);
                }
                return View(input);
            }

            sponsor.Name = input.Name;
            sponsor.Tier = input.Tier;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sponsor \"{sponsor.Name}\" was updated.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction("Index", "Announcements");
        }

        // POST: Sponsors/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id)
        {
            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor != null)
            {
                _context.Sponsors.Remove(sponsor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Sponsor was removed.";
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction("Index", "Announcements");
        }
    }
}
