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

        public RecipeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ===== RECIPE LIST =====
        public async Task<IActionResult> Index()
        {
            var recipes = await _context.Recipes.AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();
            return View(recipes);
        }

        // ===== RECIPE DETAIL - free recipes are public, premium ones need an account =====
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null) return NotFound();

            if (!recipe.IsFree && User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            return View(recipe);
        }

        // ===== FREE RECIPES - Public =====
        public async Task<IActionResult> FreeRecipes()
        {
            var recipes = await _context.Recipes.AsNoTracking()
                .Where(r => r.IsFree)
                .OrderBy(r => r.Name)
                .ToListAsync();
            return View(recipes);
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
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> SubmitCustomRecipe(UserRecipe userRecipe, IFormFile? ImageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            ModelState.Remove(nameof(userRecipe.UserId));
            ModelState.Remove(nameof(userRecipe.UserName));

            if (ImageFile is { Length: > 0 })
            {
                var (path, error) = await FileStorage.SaveImageAsync(ImageFile, _env, "recipes");
                if (path == null)
                {
                    ModelState.AddModelError(nameof(ImageFile), error ?? "Invalid image.");
                    return View(userRecipe);
                }

                userRecipe.ImagePath = path;
            }

            if (ModelState.IsValid)
            {
                userRecipe.UserId = userId;
                userRecipe.UserName = User.Identity?.Name ?? "User";
                userRecipe.SubmittedDate = DateTime.Now;
                userRecipe.Status = "Pending";
                userRecipe.PrizeMoney = null;
                userRecipe.IsCertificateIssued = false;

                _context.UserRecipes.Add(userRecipe);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your custom recipe has been submitted successfully for admin review!";
                return RedirectToAction(nameof(Index));
            }

            return View(userRecipe);
        }
    }
}
