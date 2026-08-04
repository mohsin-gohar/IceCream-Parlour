using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===== LOGIN =====
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return await RedirectToLocal(returnUrl);
                }
                ModelState.AddModelError("", "Invalid login attempt.");
            }
            return View(model);
        }

        // ===== REGISTER =====
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        // ===== LOGOUT =====
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
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Generate password reset token
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                    // Build reset link
                    var resetLink = Url.Action("ResetPassword", "Account", new
                    {
                        email = model.Email,
                        token = token
                    }, protocol: Request.Scheme);

                    // TODO: Send email with resetLink
                    // For now, just show success message
                    TempData["Success"] = "Password reset link has been sent to your email.";
                    return RedirectToAction("Login");
                }
                ModelState.AddModelError("", "Email not found.");
            }
            return View(model);
        }

        // ===== RESET PASSWORD (GET) =====
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordModel
            {
                Token = token,
                Email = email
            };
            return View(model);
        }

        // ===== RESET PASSWORD (POST) =====
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user doesn't exist
                    TempData["Success"] = "Password has been reset successfully.";
                    return RedirectToAction("Login");
                }

                // Use NewPassword instead of Password
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Password reset successfully! Please login with your new password.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        // ===== ACCESS DENIED =====
        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ===== REDIRECT TO LOCAL =====
        private async Task<IActionResult> RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        // ===== PROFILE =====
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            return View(user);
        }

        // ===== UPDATE PROFILE PICTURE =====
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["Error"] = "Please select an image.";
                return RedirectToAction(nameof(Profile));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(profilePicture.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only image files (jpg, png, gif, webp) are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            // Validate file size (max 2MB)
            if (profilePicture.Length > 2 * 1024 * 1024)
            {
                TempData["Error"] = "Image size must be less than 2MB.";
                return RedirectToAction(nameof(Profile));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            // Save new image
            string folder = "images/profiles/";
            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, folder);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profilePicture.FileName);
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            user.ProfilePicture = "/" + folder + fileName;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile picture updated successfully!";
            return RedirectToAction(nameof(Profile));
        }
    }
}