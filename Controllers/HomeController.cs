using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== MAIN PAGES =====
        public async Task<IActionResult> Index()
        {
            ViewBag.Products = await _context.Recipes
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .Take(8)
                .ToListAsync();

            return View();
        }

        public IActionResult Blog() => View();
        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult Service() => View();

        public IActionResult Gallery()
        {
            // The gallery is served by GalleryController (it has the data); avoid a
            // second, model-less copy of the page that would throw a NullReferenceException.
            return RedirectToAction("Index", "Gallery");
        }

        public async Task<IActionResult> Product()
        {
            var recipes = await _context.Recipes
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(recipes);
        }

        public IActionResult Privacy() => View();
        public IActionResult Faq() => View();
        public IActionResult Recipe() => View();
        public IActionResult Feedback() => View();

        // ===== CONTACT FORM POST =====
        [HttpPost]
        public async Task<IActionResult> Contact(string? name, string? email, string? subject, string? message)
        {
            name = name?.Trim();
            email = email?.Trim();
            subject = subject?.Trim();
            message = message?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in your name, e-mail address and message.";
                return RedirectToAction(nameof(Contact));
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                TempData["Error"] = "Please enter a valid e-mail address.";
                return RedirectToAction(nameof(Contact));
            }

            if (name.Length > 100 || email.Length > 100 || (subject?.Length ?? 0) > 150 || message.Length > 2000)
            {
                TempData["Error"] = "One of the fields is too long. Please shorten your message.";
                return RedirectToAction(nameof(Contact));
            }

            var feedback = new Feedback
            {
                UserName = name,
                Email = email,
                Message = string.IsNullOrWhiteSpace(subject) ? message : $"[{subject}] {message}",
                Rating = 5,
                SubmittedDate = DateTime.UtcNow,
                IsRegistered = User.Identity?.IsAuthenticated ?? false,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contact message received from {Email}.", email);
            TempData["Success"] = "Thank you for contacting us! We'll get back to you soon.";
            return RedirectToAction(nameof(Contact));
        }

        // ===== ERROR HANDLING =====
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ===== STATUS CODE PAGES (404, 403, ...) =====
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCodePage(int code = 404)
        {
            Response.StatusCode = code;
            ViewData["StatusCode"] = code;
            ViewData["Title"] = $"Error {code} - iCREAM";
            return View();
        }
    }
}
