using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserRecipesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserRecipesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== LIST USER RECIPES =====
        public async Task<IActionResult> Index()
        {
            var userRecipes = await _context.UserRecipes
                .OrderByDescending(ur => ur.SubmittedDate)
                .ToListAsync();
            return View(userRecipes);
        }

        // ===== DETAILS & REVIEW =====
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.UserRecipes.FindAsync(id);
            if (recipe == null) return NotFound();
            return View(recipe);
        }

        // ===== UPDATE STATUS / AWARD PRIZE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, string status, decimal? prizeMoney, bool isCertificateIssued, string? adminRemarks)
        {
            var recipe = await _context.UserRecipes.FindAsync(id);
            if (recipe == null) return NotFound();

            recipe.Status = status;
            recipe.PrizeMoney = prizeMoney;
            recipe.IsCertificateIssued = isCertificateIssued;
            recipe.AdminRemarks = adminRemarks;
            recipe.ReviewedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Recipe status updated to {status} successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
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
