using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.webHostEnvironment = webHostEnvironment;
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var recipes = await context.Recipes.ToListAsync();
            return View(recipes);
        }

        // ===== CREATE (GET) =====
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Recipe());
        }

        // ===== CREATE (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Recipe recipe, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                // ✅ Ensure directory exists
                string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "recipes");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Handle Image Upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    recipe.ImagePath = "/images/recipes/" + fileName;
                }
                else
                {
                    // Default image
                    recipe.ImagePath = "/images/recipes/";
                }

                context.Recipes.Add(recipe);
                await context.SaveChangesAsync();

                TempData["Success"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(recipe);
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }
            return View(recipe);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Recipe recipe, IFormFile? ImageFile)
        {
            if (id != recipe.Id)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRecipe = await context.Recipes.FindAsync(id);
                    if (existingRecipe == null)
                    {
                        TempData["Error"] = "Product not found!";
                        return RedirectToAction(nameof(Index));
                    }

                    // Update fields
                    existingRecipe.Name = recipe.Name;
                    existingRecipe.Category = recipe.Category;
                    existingRecipe.Ingredients = recipe.Ingredients;
                    existingRecipe.Procedure = recipe.Procedure;
                    existingRecipe.IsFree = recipe.IsFree;
                    existingRecipe.Price = recipe.Price;

                    // ✅ Ensure directory exists
                    string uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "recipes");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // Handle Image Upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Delete old image
                        if (!string.IsNullOrEmpty(existingRecipe.ImagePath) &&
                            !existingRecipe.ImagePath.Contains("default-product.jpg"))
                        {
                            string oldImagePath = Path.Combine(webHostEnvironment.WebRootPath,
                                existingRecipe.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        existingRecipe.ImagePath = "/images/recipes/" + fileName;
                    }

                    await context.SaveChangesAsync();
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(recipe.Id))
                    {
                        TempData["Error"] = "Product not found!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(recipe);
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                TempData["Error"] = "Product not found!";
                return RedirectToAction(nameof(Index));
            }

            // Delete image file
            if (!string.IsNullOrEmpty(recipe.ImagePath) &&
                !recipe.ImagePath.Contains("default-product.jpg"))
            {
                string imagePath = Path.Combine(webHostEnvironment.WebRootPath,
                    recipe.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            context.Recipes.Remove(recipe);
            await context.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return context.Recipes.Any(e => e.Id == id);
        }
    }
}

