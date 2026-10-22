using System.Diagnostics;
using System.Net.Mail;
using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class HomeController : Controller
    {
        private const int MaxMessageLength = 500;

        private readonly ApplicationDbContext _context;
        private readonly IGalleryService _galleryService;

        public HomeController(ApplicationDbContext context, IGalleryService galleryService)
        {
            _context = context;
            _galleryService = galleryService;
        }

        // ===== MAIN PAGES =====
        public async Task<IActionResult> Index()
        {
            ViewBag.Products = await _context.Recipes.AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .Take(8)
                .ToListAsync();
            return View();
        }

        public IActionResult Blog() => View();

        public IActionResult About() => View();

        public IActionResult Contact() => View();

        public IActionResult Service() => View();

        public async Task<IActionResult> Gallery()
        {
            return View(await _galleryService.GetItemsAsync());
        }

        public IActionResult Privacy() => View();

        public IActionResult Faq() => View();

        public IActionResult Recipe() => View();

        public IActionResult Feedback() => View();

        /// <summary>Legacy entry point - the book order form lives on BookController.</summary>
        public IActionResult Bookorder() => RedirectToAction("Index", "Book");

        public async Task<IActionResult> Product()
        {
            var recipes = await _context.Recipes.AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();
            return View(recipes);
        }

        // ===== CONTACT FORM POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string? name, string? email, string? subject, string? message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Contact));
            }

            if (!IsValidEmail(email))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Contact));
            }

            var feedback = new Feedback
            {
                UserName = Trim(name, 100),
                Email = Trim(email, 100),
                Message = Trim(string.IsNullOrWhiteSpace(subject) ? message : $"[{Trim(subject, 100)}] {message}", MaxMessageLength),
                Rating = 5,
                SubmittedDate = DateTime.Now,
                IsRegistered = User.Identity?.IsAuthenticated ?? false,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for contacting us! We'll get back to you soon.";
            return RedirectToAction(nameof(Contact));
        }

        // ===== FEEDBACK FORM POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(string? name, string? email, int rating, string? message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Feedback));
            }

            if (!IsValidEmail(email))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(Feedback));
            }

            var feedback = new Feedback
            {
                UserName = Trim(name, 100),
                Email = Trim(email, 100),
                Message = Trim(message, MaxMessageLength),
                Rating = Math.Clamp(rating, 1, 5),
                SubmittedDate = DateTime.Now,
                IsRegistered = User.Identity?.IsAuthenticated ?? false,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for your feedback!";
            return RedirectToAction(nameof(Feedback));
        }

        // ===== ERROR =====
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var code = statusCode ?? (HttpContext.Response.StatusCode >= 400 ? HttpContext.Response.StatusCode : 500);
            Response.StatusCode = code;
            ViewData["StatusCode"] = code;

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var address = new MailAddress(email.Trim());
                return address.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        private static string Trim(string value, int maxLength)
        {
            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }
    }
}
