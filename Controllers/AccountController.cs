using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // ===== LOGIN =====
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return await RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "This account is temporarily locked. Please try again later."
                : "Invalid login attempt.");

            return View(model);
        }

        // ===== REGISTER =====
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, AppRoles.User);
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Welcome to iCREAM! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        // ===== LOGOUT =====
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ===== FORGOT PASSWORD =====
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new
                {
                    email = model.Email,
                    token
                }, protocol: Request.Scheme);

                // Email delivery is not configured in this project - the link is
                // written to the log so administrators can hand it over securely.
                _logger.LogInformation("Password reset link for {Email}: {ResetLink}", model.Email, resetLink);
            }

            // Always show the same message so the endpoint cannot be used to
            // discover which email addresses have an account.
            TempData["Success"] = "If an account exists for that email address, a password reset link has been generated.";
            return RedirectToAction(nameof(Login));
        }

        // ===== RESET PASSWORD (GET) =====
        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordModel
            {
                Token = token,
                Email = email
            });
        }

        // ===== RESET PASSWORD (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Success"] = "Password has been reset successfully. Please login with your new password.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password reset successfully! Please login with your new password.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ===== ACCESS DENIED =====
        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ===== PROFILE =====
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(user);
        }

        // ===== UPDATE PROFILE PICTURE =====
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile? profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var (path, error) = await FileStorage.SaveImageAsync(profilePicture, _webHostEnvironment, "profiles");
            if (path == null)
            {
                TempData["Error"] = error ?? "Please select a valid image.";
                return RedirectToAction(nameof(Profile));
            }

            FileStorage.DeleteImage(_webHostEnvironment, user.ProfilePicture);

            user.ProfilePicture = path;
            var result = await _userManager.UpdateAsync(user);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Profile picture updated successfully!"
                : "The profile picture could not be saved.";

            return RedirectToAction(nameof(Profile));
        }

        // ===== REDIRECT TO LOCAL =====
        private async Task<IActionResult> RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user != null && await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
