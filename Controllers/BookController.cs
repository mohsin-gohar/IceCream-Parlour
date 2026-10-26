using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Helpers;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    /// <summary>
    /// Controller for managing book browsing and ordering functionality.
    /// </summary>
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookController> _logger;

        public BookController(ApplicationDbContext context, ILogger<BookController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Displays the list of all available books.
        /// </summary>
        /// <returns>View with list of books or error page</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Fetching books list");
                var books = await _context.Books.ToListAsync();

                if (books.IsNullOrEmpty())
                {
                    _logger.LogWarning("No books found in database");
                    TempData["Info"] = "No books available at the moment.";
                    return View("index", new List<Book>());
                }

                _logger.LogInformation("Successfully retrieved {BookCount} books", books.Count);
                return View("index", books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books list");
                TempData["Error"] = "An error occurred while retrieving books. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Displays the book details and order form for a specific book.
        /// </summary>
        /// <param name="id">The book ID</param>
        /// <returns>Book details view or NotFound if book doesn't exist</returns>
        public async Task<IActionResult> Order(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid book ID requested: {BookId}", id);
                    return BadRequest("Invalid book ID");
                }

                var book = await _context.Books
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (book == null)
                {
                    _logger.LogWarning("Book not found with ID: {BookId}", id);
                    return NotFound("The requested book could not be found.");
                }

                _logger.LogInformation("Retrieved book details for ID: {BookId}", id);
                return View(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book details for ID: {BookId}", id);
                TempData["Error"] = "An error occurred while retrieving book details.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Processes the book order submission (POST).
        /// </summary>
        /// <param name="order">The order object with book details</param>
        /// <returns>Redirect to index on success or the form on validation failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(Order order)
        {
            try
            {
                ModelState.Remove("OrderNumber");
                ModelState.Remove("Customer");
                ModelState.Remove("Book");

                if (order == null)
                {
                    _logger.LogWarning("Null order object received");
                    return BadRequest("Invalid order data");
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Order validation failed. Errors: {@Errors}", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return View(order);
                }

                // Validate required fields
                if (order.CustomerName.IsNullOrWhiteSpace())
                {
                    ModelState.AddModelError("CustomerName", "Customer name is required");
                    return View(order);
                }

                if (order.CustomerEmail.IsNullOrWhiteSpace() || !order.CustomerEmail.IsValidEmail())
                {
                    ModelState.AddModelError("CustomerEmail", "Valid email address is required");
                    return View(order);
                }

                if (order.CustomerPhone.IsNullOrWhiteSpace() || !order.CustomerPhone.IsValidPhoneNumber())
                {
                    ModelState.AddModelError("CustomerPhone", "Valid phone number is required");
                    return View(order);
                }

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

                // Generate order number
                order.OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                order.OrderDate = DateTime.Now;
                order.OrderStatus = "Pending";
                order.PaymentStatus = "Pending";

                // Add and save order
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order successfully created with number: {OrderNumber}", order.OrderNumber);
                TempData["Success"] = $"Order placed successfully! Order #: {order.OrderNumber}";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating order");
                TempData["Error"] = "An error occurred while saving your order. Please try again.";
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing order");
                TempData["Error"] = "An unexpected error occurred. Please try again later.";
                return View(order);
            }
        }

        /// <summary>
        /// Displays the static book order information page.
        /// </summary>
        /// <returns>Static book order page</returns>
        public IActionResult Bookorder()
        {
            try
            {
                _logger.LogInformation("Accessed book order information page");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading book order page");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}