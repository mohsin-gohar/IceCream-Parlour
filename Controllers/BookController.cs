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
        public async Task<IActionResult> Order(Order order)
        {
            if (ModelState.IsValid)
            {
                order.OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
                order.OrderDate = DateTime.Now;
                order.OrderStatus = "Pending";
                order.PaymentStatus = "Pending";

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Order placed successfully! Order #: " + order.OrderNumber;
                return RedirectToAction("Index");
            }
            return View(order);
        }

        // ===== BOOK ORDER PAGE (Static HTML) =====
        public IActionResult Bookorder()
        {
            return View();
        }
    }
}