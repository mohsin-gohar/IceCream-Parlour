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
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BooksController> _logger;

        public BooksController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<BooksController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // ===== LIST BOOKS =====
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Books";
            var books = await _context.Books
                .AsNoTracking()
                .OrderByDescending(b => b.Id)
                .ToListAsync();

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
        public async Task<IActionResult> Create(Book book, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Book.ImagePath));

            if (!ModelState.IsValid)
            {
                return View(book);
            }

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    book.ImagePath = await FileUploadHelper.SaveImageAsync(imageFile, _environment, "images/books");
                }
            }
            catch (FileUploadHelper.ValidationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
                return View(book);
            }

            book.Id = 0;
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Book {BookId} created.", book.Id);
            TempData["Success"] = "Book added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Book";
            return View(book);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        public async Task<IActionResult> Edit(Book book, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Book.ImagePath));

            if (!ModelState.IsValid)
            {
                return View(book);
            }

            var existing = await _context.Books.FirstOrDefaultAsync(b => b.Id == book.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Title = book.Title;
            existing.Author = book.Author;
            existing.Description = book.Description;
            existing.Price = book.Price;
            existing.StockQuantity = book.StockQuantity;

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var newPath = await FileUploadHelper.SaveImageAsync(imageFile, _environment, "images/books");
                    FileUploadHelper.DeleteIfExists(_environment, existing.ImagePath);
                    existing.ImagePath = newPath;
                }
                catch (FileUploadHelper.ValidationException ex)
                {
                    ModelState.AddModelError("ImageFile", ex.Message);
                    return View(book);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Book updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction(nameof(Index));
            }

            FileUploadHelper.DeleteIfExists(_environment, book.ImagePath);

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Book {BookId} deleted.", id);
            TempData["Success"] = "Book deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
