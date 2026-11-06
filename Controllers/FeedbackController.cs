using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        // The feedback form lives on Home/Feedback; there is no separate Feedback view.
        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction("Feedback", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            // These values are always derived on the server.
            ModelState.Remove(nameof(Feedback.UserId));
            ModelState.Remove(nameof(Feedback.IsRegistered));
            ModelState.Remove(nameof(Feedback.IsRead));
            ModelState.Remove(nameof(Feedback.SubmittedDate));

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in your name, a valid e-mail address and your message.";
                return RedirectToAction("Feedback", "Home");
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                feedback.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                feedback.UserName = User.Identity?.Name ?? feedback.UserName;
                feedback.IsRegistered = true;
            }
            else
            {
                feedback.IsRegistered = false;
                feedback.UserId = null;
            }

            feedback.UserName = feedback.UserName.Trim();
            feedback.Email = feedback.Email.Trim();
            feedback.Message = feedback.Message.Trim();
            feedback.SubmittedDate = DateTime.UtcNow;
            feedback.IsRead = false;

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for your feedback!";
            return RedirectToAction("Feedback", "Home");
        }
    }
}
