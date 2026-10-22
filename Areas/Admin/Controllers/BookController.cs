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
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BooksController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===== LIST BOOKS =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Books";
            var books = await _context.Books.AsNoTracking().OrderBy(b => b.Title).ToListAsync();
            return View(books);
        }

        // ===== CREATE (GET) =====
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Book";
            return View(new Book());
        }

        // ===== CREATE (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Create(Book book, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(book);
            }

            if (ImageFile is { Length: > 0 })
            {
                var (path, error) = await FileStorage.SaveImageAsync(ImageFile, _webHostEnvironment, "books");
                if (path == null)
                {
                    ModelState.AddModelError(nameof(ImageFile), error ?? "Invalid image.");
                    return View(book);
                }

                book.ImagePath = path;
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            ViewData["Title"] = "Edit Book";
            return View(book);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Edit(int id, Book book, IFormFile? ImageFile)
        {
            if (id != book.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(book);
            }

            var existing = await _context.Books.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = book.Title.Trim();
            existing.Author = book.Author.Trim();
            existing.Description = book.Description;
            existing.Price = book.Price;
            existing.StockQuantity = book.StockQuantity;

            if (ImageFile is { Length: > 0 })
            {
                var (path, error) = await FileStorage.SaveImageAsync(ImageFile, _webHostEnvironment, "books");
                if (path == null)
                {
                    ModelState.AddModelError(nameof(ImageFile), error ?? "Invalid image.");
                    return View(book);
                }

                FileStorage.DeleteImage(_webHostEnvironment, existing.ImagePath);
                existing.ImagePath = path;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction(nameof(Index));
            }

            FileStorage.DeleteImage(_webHostEnvironment, book.ImagePath);

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
