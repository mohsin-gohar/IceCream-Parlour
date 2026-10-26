using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GalleryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = new List<GalleryItemViewModel>();

            // ===== 1. RECIPES (Ice Cream Flavors) =====
            var recipes = await _context.Recipes
                .AsNoTracking()
                .Select(r => new GalleryItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    ImagePath = r.ImagePath,
                    Description = r.Category,
                    Price = r.Price,
                    Type = "recipe",
                    Category = r.Category,
                    CreatedDate = r.CreatedDate
                })
                .ToListAsync();
            items.AddRange(recipes);

            // ===== 2. BOOKS =====
            var books = await _context.Books
                .AsNoTracking()
                .Select(b => new GalleryItemViewModel
                {
                    Id = b.Id,
                    Name = b.Title,
                    ImagePath = b.ImagePath,
                    Description = b.Description,
                    Price = b.Price,
                    Author = b.Author,
                    Type = "book",
                    CreatedDate = null
                })
                .ToListAsync();
            items.AddRange(books);

            // ===== 3. USER RECIPES (approved only) =====
            var userRecipes = await _context.UserRecipes
                .AsNoTracking()
                .Where(ur => ur.Status == "Approved" || ur.Status == "Selected")
                .Select(ur => new GalleryItemViewModel
                {
                    Id = ur.Id,
                    Name = ur.RecipeName,
                    ImagePath = ur.ImagePath,
                    Description = ur.Ingredients,
                    Price = 0,
                    Author = ur.UserName,
                    Type = "user",
                    CreatedDate = ur.SubmittedDate
                })
                .ToListAsync();
            items.AddRange(userRecipes);

            // Sort by CreatedDate descending (newest first)
            items = items
                .OrderByDescending(i => i.CreatedDate ?? DateTime.MinValue)
                .ToList();

            return View(items);
        }
    }
}
