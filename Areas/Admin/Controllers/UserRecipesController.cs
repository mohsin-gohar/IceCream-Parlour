using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public class UserRecipesController : Controller
    {
        private static readonly string[] AllowedStatuses = { "Pending", "Approved", "Rejected", "Selected" };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserRecipesController> _logger;

        public UserRecipesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<UserRecipesController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // ===== LIST USER RECIPES =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "User Recipes";
            var userRecipes = await _context.UserRecipes
                .AsNoTracking()
                .OrderByDescending(ur => ur.SubmittedDate)
                .ToListAsync();

            return View(userRecipes);
        }

        // ===== DETAILS & REVIEW =====
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.UserRecipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Review Recipe";
            return View(recipe);
        }

        // ===== UPDATE STATUS / AWARD PRIZE =====
        [HttpPost]
        public async Task<IActionResult> Review(int id, string? status, decimal? prizeMoney, bool isCertificateIssued, string? adminRemarks)
        {
            var recipe = await _context.UserRecipes.FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status))
            {
                TempData["Error"] = "Please choose a valid review status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (prizeMoney is < 0)
            {
                TempData["Error"] = "Prize money cannot be negative.";
                return RedirectToAction(nameof(Details), new { id });
            }

            recipe.Status = status;
            recipe.PrizeMoney = prizeMoney;
            recipe.IsCertificateIssued = isCertificateIssued;
            recipe.AdminRemarks = adminRemarks?.Trim();
            recipe.ReviewedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User recipe {RecipeId} reviewed with status {Status}.", id, status);
            TempData["Success"] = $"Recipe status updated to {status} successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.UserRecipes.FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                TempData["Error"] = "Recipe not found.";
                return RedirectToAction(nameof(Index));
            }

            FileUploadHelper.DeleteIfExists(_environment, recipe.ImagePath);
            _context.UserRecipes.Remove(recipe);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User recipe deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
