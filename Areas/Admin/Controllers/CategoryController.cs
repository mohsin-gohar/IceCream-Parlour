using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Categories";
            var categories = await _context.Categories.AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        // ===== DETAILS =====
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            ViewData["Title"] = "Category Details";
            return View(category);
        }

        // ===== CREATE (GET) =====
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Category";
            return View(new Category { IsActive = true });
        }

        // ===== CREATE (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            var name = category.Name?.Trim() ?? string.Empty;

            if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
            {
                ModelState.AddModelError(nameof(category.Name), "A category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.Name = name;
            category.Description = category.Description?.Trim();
            category.CreatedDate = DateTime.Now;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            ViewData["Title"] = "Edit Category";
            return View(category);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            var existing = await _context.Categories.FindAsync(id);
            if (existing == null) return NotFound();

            var name = category.Name?.Trim() ?? string.Empty;

            if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != id))
            {
                ModelState.AddModelError(nameof(category.Name), "A category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            existing.Name = name;
            existing.Description = category.Description?.Trim();
            existing.IsActive = category.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE (GET) =====
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            ViewData["Title"] = "Delete Category";
            return View(category);
        }

        // ===== DELETE (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Products.AnyAsync(p => p.CategoryId == id))
            {
                TempData["Error"] = "This category cannot be deleted because products are linked to it.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
