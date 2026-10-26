using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class RecipeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<RecipeController> _logger;

        public RecipeController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<RecipeController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // ===== RECIPE LIST (All Recipes) =====
        public async Task<IActionResult> Index()
        {
            var recipes = await _context.Recipes
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(recipes);
        }

        // ===== RECIPE DETAIL - Registered Users Only =====
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // ===== FREE RECIPES - Public =====
        public async Task<IActionResult> FreeRecipes()
        {
            var recipes = await _context.Recipes
                .AsNoTracking()
                .Where(r => r.IsFree)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            ViewData["Title"] = "Free Recipes - iCREAM";
            return View("Index", recipes);
        }

        // ===== SUBMIT CUSTOM RECIPE (GET) =====
        [Authorize]
        [HttpGet]
        public IActionResult SubmitCustomRecipe()
        {
            return View(new UserRecipe());
        }

        // ===== SUBMIT CUSTOM RECIPE (POST) =====
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitCustomRecipe(UserRecipe userRecipe, IFormFile? imageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            ModelState.Remove(nameof(UserRecipe.UserId));
            ModelState.Remove(nameof(UserRecipe.UserName));
            ModelState.Remove(nameof(UserRecipe.Status));
            ModelState.Remove(nameof(UserRecipe.PrizeMoney));
            ModelState.Remove(nameof(UserRecipe.IsCertificateIssued));
            ModelState.Remove(nameof(UserRecipe.ReviewedDate));
            ModelState.Remove(nameof(UserRecipe.AdminRemarks));
            ModelState.Remove(nameof(UserRecipe.SubmittedDate));
            ModelState.Remove(nameof(UserRecipe.ImagePath));

            if (!ModelState.IsValid)
            {
                return View(userRecipe);
            }

            string? uploadedPath = null;
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    uploadedPath = await FileUploadHelper.SaveImageAsync(imageFile, _env, "images/recipes");
                }
            }
            catch (FileUploadHelper.ValidationException ex)
            {
                ModelState.AddModelError(nameof(imageFile), ex.Message);
                return View(userRecipe);
            }

            userRecipe.Id = 0;
            userRecipe.UserId = userId;
            userRecipe.UserName = User.Identity?.Name ?? "User";
            userRecipe.SubmittedDate = DateTime.UtcNow;
            userRecipe.Status = "Pending";
            userRecipe.ImagePath = uploadedPath;
            userRecipe.ReviewedDate = null;
            userRecipe.AdminRemarks = null;
            userRecipe.PrizeMoney = null;
            userRecipe.IsCertificateIssued = false;

            _context.UserRecipes.Add(userRecipe);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} submitted recipe {RecipeId}.", userId, userRecipe.Id);
            TempData["Success"] = "Your custom recipe has been submitted successfully for admin review!";
            return RedirectToAction(nameof(Index));
        }
    }
}
