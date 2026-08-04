using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        public IActionResult Index()
        {
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

        public IActionResult Product()
        {
            return View();
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
        public IActionResult Contact(string Name, string Email, string Subject, string Message)
        {
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Email))
            {
                // Save feedback logic here
                TempData["Success"] = "Thank you for contacting us! We'll get back to you soon.";
                return RedirectToAction("Contact");
            }
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