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
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Products";
            var recipes = await _context.Recipes.AsNoTracking()
                .OrderBy(r => r.Name)
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
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Create(Recipe recipe, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(recipe);
            }

            if (ImageFile is { Length: > 0 })
            {
                var (path, error) = await FileStorage.SaveImageAsync(ImageFile, _webHostEnvironment, "recipes");
                if (path == null)
                {
                    ModelState.AddModelError(nameof(ImageFile), error ?? "Invalid image.");
                    return View(recipe);
                }

                recipe.ImagePath = path;
            }
            else
            {
                recipe.ImagePath = "/img/product-1.jpg";
            }

            recipe.CreatedDate = DateTime.Now;

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
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
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Edit(int id, Recipe recipe, IFormFile? ImageFile)
        {
            if (id != recipe.Id)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(recipe);
            }

            var existing = await _context.Recipes.FindAsync(id);
            if (existing == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            existing.Name = recipe.Name.Trim();
            existing.Category = recipe.Category.Trim();
            existing.Ingredients = recipe.Ingredients;
            existing.Procedure = recipe.Procedure;
            existing.IsFree = recipe.IsFree;
            existing.Price = recipe.Price;

            if (ImageFile is { Length: > 0 })
            {
                var (path, error) = await FileStorage.SaveImageAsync(ImageFile, _webHostEnvironment, "recipes");
                if (path == null)
                {
                    ModelState.AddModelError(nameof(ImageFile), error ?? "Invalid image.");
                    return View(recipe);
                }

                FileStorage.DeleteImage(_webHostEnvironment, existing.ImagePath);
                existing.ImagePath = path;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            FileStorage.DeleteImage(_webHostEnvironment, recipe.ImagePath);

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
