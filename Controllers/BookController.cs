using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== BOOK LIST =====
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.ToListAsync();
            return View("index", books);
        }

        // ===== BOOK ORDER =====
        public async Task<IActionResult> Order(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(Order order)
        {
            ModelState.Remove("OrderNumber");
            ModelState.Remove("Customer");
            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                // Find or create customer based on email
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
                return RedirectToAction("Index");
            }

            var book = await _context.Books.FindAsync(order.BookId);
            if (book == null) return NotFound();
            TempData["Error"] = "Please fill all required fields correctly.";
            return View(book);
        }

        // ===== BOOK ORDER PAGE (Static HTML) =====
        public IActionResult Bookorder()
        {
            return View();
        }
    }
}