using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public  CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

      

        // ===== INDEX - List all categories =====
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        // ===== DETAILS =====
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        // ===== CREATE (GET) =====
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ===== CREATE (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedDate = DateTime.Now;
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Categories.FindAsync(id);
                    if (existing == null)
                        return NotFound();

                    existing.Name = category.Name;
                    existing.Description = category.Description;
                    existing.IsActive = category.IsActive;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                        return NotFound();
                    throw;
                }
            }
            return View(category);
        }

        // ===== DELETE (GET) =====
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        // ===== DELETE (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}