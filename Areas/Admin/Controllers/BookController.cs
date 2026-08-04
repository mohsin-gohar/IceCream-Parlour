using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
            var books = await _context.Books.ToListAsync();
            return View(books);
        }

        // ===== CREATE (GET) =====
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ===== CREATE (POST) - SIRF EK =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string folder = "images/books/";
                    string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, folder);

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string fullPath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                        await ImageFile.CopyToAsync(stream);

                    book.ImagePath = "/" + folder + fileName;
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Book added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // ===== EDIT (GET) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        // ===== EDIT (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Book book, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Books.FindAsync(book.Id);
                if (existing == null) return NotFound();

                existing.Title = book.Title;
                existing.Author = book.Author;
                existing.Description = book.Description;
                existing.Price = book.Price;
                existing.StockQuantity = book.StockQuantity;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string folder = "images/books/";
                    string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, folder);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string fullPath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                        await ImageFile.CopyToAsync(stream);

                    existing.ImagePath = "/" + folder + fileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Book updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                if (!string.IsNullOrEmpty(book.ImagePath))
                {
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, book.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Book deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ORDER - Admin se bhi order kar sakte hain (optional)
        // ============================================================

        // ===== ORDER (GET) =====
        [HttpGet]
        public async Task<IActionResult> Order(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);  // ✅ View expects Book model
        }

        // ===== ORDER (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(Order order)
        {
            // Remove navigation properties and autogenerated values from model state
            ModelState.Remove("Customer");
            ModelState.Remove("OrderNumber");

            if (ModelState.IsValid)
            {
                // Resolve or create Customer record so we have a valid CustomerId
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == order.CustomerEmail);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = order.CustomerName,
                        Email = order.CustomerEmail,
                        Phone = order.CustomerPhone,
                        Address = order.DeliveryAddress,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                order.CustomerId = customer.Id;
                order.OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
                order.OrderDate = DateTime.Now;
                order.OrderStatus = "Pending";
                order.PaymentStatus = "Pending";

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Order placed successfully! Order #: " + order.OrderNumber;
                return RedirectToAction("Index", "Home");  // ✅ Public Home page par redirect
            }

            // ❌ Validation fail → wapas Order form par bhejo
            var book = await _context.Books.FindAsync(order.BookId);
            if (book == null) return NotFound();

            // ✅ Error message set karo
            TempData["Error"] = "Please fill all required fields correctly.";
            return RedirectToAction("Order", new { id = order.BookId });
        }
    }
}