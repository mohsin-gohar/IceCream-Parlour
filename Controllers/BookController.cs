using System.ComponentModel.DataAnnotations;
using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderEntity = Ice_Cream_Parlour_Eproject.Areas.Models.Order;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class BookController : Controller
    {
        private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "Credit Card", "Debit Card", "PayPal", "Cash on Delivery", "Bank Transfer"
        };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookController> _logger;

        public BookController(ApplicationDbContext context, ILogger<BookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== BOOK LIST =====
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .AsNoTracking()
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View("index", books);
        }

        // ===== BOOK ORDER (GET) =====
        public async Task<IActionResult> Order(int id)
        {
            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            if (book.StockQuantity <= 0)
            {
                TempData["Error"] = $"\"{book.Title}\" is currently out of stock.";
                return RedirectToAction(nameof(Index));
            }

            return View(book);
        }

        // ===== BOOK ORDER (POST) =====
        [HttpPost]
        public async Task<IActionResult> Order(Order order)
        {
            // These values are always calculated on the server - never trust the client.
            ModelState.Remove(nameof(OrderEntity.OrderNumber));
            ModelState.Remove(nameof(OrderEntity.Customer));
            ModelState.Remove(nameof(OrderEntity.Book));
            ModelState.Remove(nameof(OrderEntity.OrderItems));
            ModelState.Remove(nameof(OrderEntity.TotalAmount));
            ModelState.Remove(nameof(OrderEntity.OrderDate));
            ModelState.Remove(nameof(OrderEntity.OrderStatus));
            ModelState.Remove(nameof(OrderEntity.PaymentStatus));
            ModelState.Remove(nameof(OrderEntity.CustomerId));

            if (string.IsNullOrWhiteSpace(order.DeliveryAddress))
            {
                ModelState.AddModelError(nameof(OrderEntity.DeliveryAddress), "Delivery address is required.");
            }

            if (string.IsNullOrWhiteSpace(order.PaymentMethod) || !AllowedPaymentMethods.Contains(order.PaymentMethod))
            {
                ModelState.AddModelError(nameof(OrderEntity.PaymentMethod), "Please choose a valid payment method.");
            }

            if (!new EmailAddressAttribute().IsValid(order.CustomerEmail ?? string.Empty))
            {
                ModelState.AddModelError(nameof(OrderEntity.CustomerEmail), "Please enter a valid e-mail address.");
            }

            var book = order.BookId.HasValue
                ? await _context.Books.FirstOrDefaultAsync(b => b.Id == order.BookId.Value)
                : null;

            if (book == null)
            {
                return NotFound();
            }

            if (book.StockQuantity <= 0)
            {
                ModelState.AddModelError(string.Empty, "This book is out of stock.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields correctly.";
                return View(book);
            }

            var email = order.CustomerEmail?.Trim() ?? string.Empty;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = order.CustomerName.Trim(),
                        Email = email,
                        Phone = order.CustomerPhone?.Trim(),
                        Address = order.DeliveryAddress?.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Keep the customer record fresh with the latest delivery details.
                    customer.FullName = order.CustomerName.Trim();
                    customer.Phone = order.CustomerPhone?.Trim() ?? customer.Phone;
                    customer.Address = order.DeliveryAddress?.Trim() ?? customer.Address;
                    customer.LastActivityDate = DateTime.UtcNow;
                }

                order.Id = 0;
                order.CustomerId = customer.Id;
                order.CustomerName = order.CustomerName.Trim();
                order.CustomerEmail = email;
                order.OrderNumber = await OrderNumberHelper.GenerateUniqueAsync(
                    number => _context.Orders.AnyAsync(o => o.OrderNumber == number));
                order.OrderDate = DateTime.UtcNow;
                order.OrderStatus = "Pending";
                order.PaymentStatus = "Pending";
                // Shipping mirrors the checkout summary: free over $100, otherwise a flat fee.
                var shippingFee = book.Price >= 100m ? 0m : 4.99m;
                order.TotalAmount = Math.Round(book.Price + shippingFee, 2);
                order.Notes = shippingFee > 0
                    ? $"Shipping ${shippingFee:0.00} included"
                    : "Free shipping applied";

                book.StockQuantity -= 1;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderNumber} placed for book {BookId}.", order.OrderNumber, book.Id);
                TempData["Success"] = $"Order placed successfully! Your order number is {order.OrderNumber}.";

                return RedirectToAction("Track", "Order", new
                {
                    orderNumber = order.OrderNumber,
                    email = order.CustomerEmail
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to place an order for book {BookId}.", book.Id);
                ModelState.AddModelError(string.Empty, "We could not place your order. Please try again.");
                return View(book);
            }
        }
    }
}
