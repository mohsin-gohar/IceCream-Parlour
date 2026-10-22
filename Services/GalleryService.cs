using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public interface IGalleryService
    {
        Task<List<GalleryItemViewModel>> GetItemsAsync();
    }

    /// <summary>
    /// Builds the public gallery from recipes, books and approved user recipes.
    /// Shared by HomeController and GalleryController so both pages stay in sync.
    /// </summary>
    public class GalleryService : IGalleryService
    {
        private readonly ApplicationDbContext _context;

        public GalleryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GalleryItemViewModel>> GetItemsAsync()
        {
            var items = new List<GalleryItemViewModel>();

            var recipes = await _context.Recipes.AsNoTracking()
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

            var books = await _context.Books.AsNoTracking()
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

            var userRecipes = await _context.UserRecipes.AsNoTracking()
                .Where(ur => ur.Status == "Selected")
                .Select(ur => new GalleryItemViewModel
                {
                    Id = ur.Id,
                    Name = ur.RecipeName,
                    ImagePath = ur.ImagePath,
                    Description = ur.Ingredients,
                    Price = ur.PrizeMoney ?? 0,
                    Author = ur.UserName,
                    Type = "user",
                    CreatedDate = ur.SubmittedDate
                })
                .ToListAsync();
            items.AddRange(userRecipes);

            return items
                .OrderByDescending(i => i.CreatedDate ?? DateTime.MinValue)
                .ThenBy(i => i.Name)
                .ToList();
        }
    }
}
