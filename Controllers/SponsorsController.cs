using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers
{
    [Authorize]
    public class SponsorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SponsorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // GET: Sponsors/Create
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
