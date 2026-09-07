using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== MAIN PAGES =====
        public async Task<IActionResult> Index()
        {
            ViewBag.Products = await _context.Recipes.ToListAsync();
            return View();
        }
        public IActionResult Blog()
        {
            return View();
        }    
        
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Service()
        {
            return View();
        }

        public IActionResult Gallery()
        {
            return View();
        }

        public async Task<IActionResult> Product()
        {
            var recipes = await _context.Recipes.ToListAsync();
            return View(recipes);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Faq()
        {
            return View();
        }

        public IActionResult Recipe()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View();
        }

        public IActionResult Bookorder()
        {
            return View();
        }

        // ===== CONTACT FORM POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string Name, string Email, string Subject, string Message)
        {
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Message))
            {
                var feedback = new Feedback
                {
                    UserName = Name,
                    Email = Email,
                    Message = string.IsNullOrEmpty(Subject) ? Message : $"[{Subject}] {Message}",
                    Rating = 5,
                    SubmittedDate = DateTime.Now,
                    IsRegistered = User.Identity?.IsAuthenticated ?? false,
                    UserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you for contacting us! We'll get back to you soon.";
                return RedirectToAction("Contact");
            }
            TempData["Error"] = "Please fill in all required fields.";
            return View();
        }

        // ===== FEEDBACK FORM POST =====
        [HttpPost]
        public IActionResult Feedback(string Name, string Email, int Rating, string Message)
        {
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Email))
            {
                // Save feedback to database
                TempData["Success"] = "Thank you for your feedback!";
                return RedirectToAction("Feedback");
            }
            return View();
        }

        // ===== ERROR =====
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}