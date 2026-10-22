using System.Net.Mail;
using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class FeedbackController : Controller
    {
        private const int MaxMessageLength = 500;

        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== FEEDBACK FORM (the form lives on the Home page) =====
        [HttpGet]
        public IActionResult Create() => RedirectToAction("Feedback", "Home");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            // Never trust identity related fields coming from the browser.
            feedback.UserId = null;
            feedback.SubmittedDate = DateTime.Now;
            feedback.IsRead = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                feedback.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(feedback.UserName))
                {
                    feedback.UserName = User.Identity?.Name ?? "User";
                }
                feedback.IsRegistered = true;
            }
            else
            {
                feedback.IsRegistered = false;
            }

            feedback.Rating = Math.Clamp(feedback.Rating, 1, 5);

            if (feedback.Message is { Length: > MaxMessageLength })
            {
                feedback.Message = feedback.Message[..MaxMessageLength];
            }

            if (!IsValidEmail(feedback.Email))
            {
                ModelState.AddModelError(nameof(feedback.Email), "Please enter a valid email address.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in the feedback form correctly.";
                return RedirectToAction("Feedback", "Home");
            }

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for your feedback!";
            return RedirectToAction("Feedback", "Home");
        }

        /// <summary>Legacy route kept for old bookmarks.</summary>
        public IActionResult Feedback() => RedirectToAction("Feedback", "Home");

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

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
    }
}
