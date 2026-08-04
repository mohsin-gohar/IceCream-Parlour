using Microsoft.AspNetCore.Mvc;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using System.Security.Claims;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== FEEDBACK FORM =====
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                feedback.SubmittedDate = DateTime.Now;

                if (User.Identity?.IsAuthenticated == true)
                {
                    feedback.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    feedback.UserName = User.Identity?.Name ?? "User";
                    feedback.IsRegistered = true;
                }
                else
                {
                    feedback.IsRegistered = false;
                }

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thank you for your feedback!";
                return RedirectToAction("Index", "Home");
            }
            return View(feedback);
        }

        // ===== FEEDBACK STATIC PAGE =====
        public IActionResult Feedback()
        {
            return View();
        }
    }
}