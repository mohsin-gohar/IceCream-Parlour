using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var books = await _context.Books.AsNoTracking()
                .OrderBy(b => b.Title)
                .ToListAsync();
            return View("index", books);
        }

        // ===== BOOK ORDER FORM =====
        public async Task<IActionResult> Order(int id)
        {
            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            if (!book.IsAvailable)
            {
                TempData["Error"] = "This book is currently out of stock.";
                return RedirectToAction(nameof(Index));
            }

            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(Order order, int quantity = 1)
        {
            // These values are generated / resolved on the server.
            ModelState.Remove("OrderNumber");
            ModelState.Remove("Customer");
            ModelState.Remove("Book");
            ModelState.Remove("TotalAmount");
            ModelState.Remove("CustomerId");

            var book = order.BookId.HasValue
                ? await _context.Books.FirstOrDefaultAsync(b => b.Id == order.BookId.Value)
                : null;

            if (book == null) return NotFound();

            quantity = Math.Clamp(quantity, 1, 99);

            if (book.StockQuantity < quantity)
            {
                TempData["Error"] = $"Only {book.StockQuantity} copy/copies of '{book.Title}' are in stock.";
                return RedirectToAction(nameof(Order), new { id = book.Id });
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields correctly.";
                return RedirectToAction(nameof(Order), new { id = book.Id });
            }

            var email = order.CustomerEmail.Trim();
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = order.CustomerName.Trim(),
                    Email = email,
                    Phone = order.CustomerPhone,
                    Address = order.DeliveryAddress,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
            }
            else
            {
                customer.FullName = order.CustomerName.Trim();
                customer.Phone = order.CustomerPhone ?? customer.Phone;
                customer.Address = order.DeliveryAddress ?? customer.Address;
            }

            customer.LastActivityDate = DateTime.Now;

            var orderNumber = await GenerateUniqueOrderNumberAsync();

            var newOrder = new Order
            {
                Customer = customer,
                CustomerName = customer.FullName,
                CustomerEmail = customer.Email,
                CustomerPhone = customer.Phone,
                DeliveryAddress = order.DeliveryAddress,
                BookId = book.Id,
                OrderNumber = orderNumber,
                OrderDate = DateTime.Now,
                OrderStatus = "Pending",
                PaymentStatus = "Pending",
                PaymentMethod = string.IsNullOrWhiteSpace(order.PaymentMethod) ? "CashOnDelivery" : order.PaymentMethod,
                Notes = order.Notes,
                TotalAmount = book.Price * quantity
            };

            newOrder.OrderItems.Add(new OrderItem
            {
                ProductName = book.Title,
                Quantity = quantity,
                UnitPrice = book.Price
            });

            book.StockQuantity -= quantity;

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order placed successfully! Your order number is {newOrder.OrderNumber}.";
            return RedirectToAction(nameof(Index));
        }

        // ===== BOOK ORDER PAGE (legacy route) =====
        public IActionResult Bookorder() => RedirectToAction(nameof(Index));

        private async Task<string> GenerateUniqueOrderNumberAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var number = OrderNumberGenerator.Generate();
                if (!await _context.Orders.AnyAsync(o => o.OrderNumber == number))
                {
                    return number;
                }
            }

            return $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}
