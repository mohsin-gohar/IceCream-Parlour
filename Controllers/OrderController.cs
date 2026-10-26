using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== MY ORDERS =====
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Challenge();
            }

            var normalized = userEmail.ToLowerInvariant();
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Book)
                .Where(o => o.CustomerEmail.ToLower() == normalized)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ===== TRACK ORDER =====
        // An order can only be viewed by its owner: either the signed-in user or
        // someone who knows both the order number and the e-mail used to order.
        public async Task<IActionResult> Track(string? orderNumber, string? email)
        {
            orderNumber = orderNumber?.Trim();
            email = email?.Trim();

            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return View();
            }

            if (orderNumber.Length > 50)
            {
                ViewBag.Message = "That order number does not look valid.";
                return View();
            }

            string? userEmail = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            }

            if (string.IsNullOrWhiteSpace(userEmail) && string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Message = "Please enter the e-mail address that was used when the order was placed.";
                ViewBag.OrderNumber = orderNumber;
                return View();
            }

            var lookupEmail = (userEmail ?? email)!.ToLowerInvariant();
            var normalizedOrderNumber = orderNumber.ToLowerInvariant();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Book)
                .FirstOrDefaultAsync(o =>
                    o.OrderNumber.ToLower() == normalizedOrderNumber &&
                    o.CustomerEmail.ToLower() == lookupEmail);

            if (order == null)
            {
                ViewBag.Message = "No matching order found. Please check your order number and e-mail address.";
                ViewBag.OrderNumber = orderNumber;
            }

            return View(order);
        }
    }
}
