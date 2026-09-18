using Ice_Cream_Parlour_Eproject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            if (string.IsNullOrEmpty(userEmail)) return Challenge();

            var orders = await _context.Orders
                .Include(o => o.Book)
                .Where(o => o.CustomerEmail.ToLower() == userEmail.ToLower())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ===== TRACK ORDER =====
        public async Task<IActionResult> Track(string? orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                return View();
            }

            var order = await _context.Orders
                .Include(o => o.Book)
                .FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == orderNumber.Trim().ToLower());

            if (order == null)
            {
                ViewBag.Message = "Order not found. Please check your order number and try again.";
            }

            return View(order);
        }
    }
}
