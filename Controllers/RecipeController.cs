using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;

using System.Security.Claims;

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

        // ===== RECIPE LIST (All Recipes) =====
        public async Task<IActionResult> Index()
        {
            var recipes = await _context.Recipes.ToListAsync();
            return View(recipes);
        }

        // ===== RECIPE DETAIL - Registered Users Only =====
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound();
            return View(recipe);
        }

        // ===== FREE RECIPES - Public =====
        public async Task<IActionResult> FreeRecipes()
        {
            var recipes = await _context.Recipes.Where(r => r.IsFree).ToListAsync();
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
        public async Task<IActionResult> SubmitCustomRecipe(UserRecipe userRecipe, IFormFile? ImageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            ModelState.Remove("UserId");
            ModelState.Remove("UserName");

            if (ModelState.IsValid)
            {
                userRecipe.UserId = userId;
                userRecipe.UserName = User.Identity?.Name ?? "User";
                userRecipe.SubmittedDate = DateTime.Now;
                userRecipe.Status = "Pending";

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string folder = "images/recipes/";
                    string folderPath = Path.Combine(_env.WebRootPath, folder);
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string fullPath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    userRecipe.ImagePath = "/" + folder + fileName;
                }

                _context.UserRecipes.Add(userRecipe);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your custom recipe has been submitted successfully for admin review!";
                return RedirectToAction(nameof(Index));
            }

            return View(userRecipe);
        }
    }
}