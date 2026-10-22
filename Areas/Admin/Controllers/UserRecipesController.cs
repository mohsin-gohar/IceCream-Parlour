using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public class UserRecipesController : Controller
    {
        private static readonly string[] AllowedStatuses = { "Pending", "Selected", "Rejected" };

        private readonly ApplicationDbContext _context;

        public UserRecipesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== LIST USER RECIPES =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "User Recipes";
            var userRecipes = await _context.UserRecipes.AsNoTracking()
                .OrderByDescending(ur => ur.SubmittedDate)
                .ToListAsync();
            return View(userRecipes);
        }

        // ===== DETAILS & REVIEW =====
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.UserRecipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null) return NotFound();

            ViewData["Title"] = "Review Recipe";
            return View(recipe);
        }

        // ===== UPDATE STATUS / AWARD PRIZE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, string status, decimal? prizeMoney, bool isCertificateIssued, string? adminRemarks)
        {
            if (!AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Invalid review status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var recipe = await _context.UserRecipes.FindAsync(id);
            if (recipe == null) return NotFound();

            recipe.Status = status;
            recipe.PrizeMoney = prizeMoney is >= 0 ? prizeMoney : null;
            recipe.IsCertificateIssued = isCertificateIssued;
            recipe.AdminRemarks = string.IsNullOrWhiteSpace(adminRemarks) ? null : adminRemarks.Trim();
            recipe.ReviewedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Recipe status updated to {status} successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.UserRecipes.FindAsync(id);
            if (recipe != null)
            {
                _context.UserRecipes.Remove(recipe);
                await _context.SaveChangesAsync();
                TempData["Success"] = "User recipe deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
