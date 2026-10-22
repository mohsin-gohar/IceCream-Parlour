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
    public class ProductsController : Controller
    {
        private const string DefaultImage = "/img/product-1.jpg";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Products";
            var recipes = await _context.Recipes
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(recipes);
        }

        // ===== CREATE (GET) =====
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Product";
            return View(new Recipe());
        }

        // ===== CREATE (POST) =====
        [HttpPost]
        public async Task<IActionResult> Create(Recipe recipe, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Recipe.ImagePath));
            ModelState.Remove(nameof(Recipe.CreatedDate));

            if (!ModelState.IsValid)
            {
                return View(recipe);
            }

            try
            {
                recipe.ImagePath = imageFile != null && imageFile.Length > 0
                    ? await FileUploadHelper.SaveImageAsync(imageFile, _environment, "images/recipes")
                    : DefaultImage;
            }
            catch (FileUploadHelper.ValidationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
                return View(recipe);
            }

            recipe.Id = 0;
            recipe.CreatedDate = DateTime.UtcNow;

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Recipe {RecipeId} created.", recipe.Id);
            TempData["Success"] = "Product added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await _context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Edit Product";
            return View(recipe);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Recipe recipe, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Recipe.ImagePath));
            ModelState.Remove(nameof(Recipe.CreatedDate));

            if (id != recipe.Id)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(recipe);
            }

            var existing = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id);
            if (existing == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            existing.Name = recipe.Name;
            existing.Category = recipe.Category;
            existing.Ingredients = recipe.Ingredients;
            existing.Procedure = recipe.Procedure;
            existing.IsFree = recipe.IsFree;
            existing.Price = recipe.Price;

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var newPath = await FileUploadHelper.SaveImageAsync(imageFile, _environment, "images/recipes");
                    FileUploadHelper.DeleteIfExists(_environment, existing.ImagePath);
                    existing.ImagePath = newPath;
                }
                catch (FileUploadHelper.ValidationException ex)
                {
                    ModelState.AddModelError("imageFile", ex.Message);
                    return View(recipe);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(recipe.ImagePath, DefaultImage, StringComparison.OrdinalIgnoreCase))
            {
                FileUploadHelper.DeleteIfExists(_environment, recipe.ImagePath);
            }

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Recipe {RecipeId} deleted.", id);
            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
