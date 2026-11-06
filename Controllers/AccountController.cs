using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment webHostEnvironment,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
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
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Users sign in with their e-mail address; resolve the actual user name first
            // so this keeps working if a user name ever differs from the e-mail.
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} signed in.", user.Email);
                return await RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "This account is temporarily locked because of too many failed attempts. Please try again in 15 minutes.");
                return View(model);
            }

            if (result.RequiresTwoFactor)
            {
                ModelState.AddModelError(string.Empty, "Two-factor authentication is required for this account.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ===== REGISTER =====
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var user = new User
            {
                UserName = email,
                Email = email,
                FullName = model.FullName.Trim(),
                CreatedAt = DateTime.UtcNow
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

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Could not assign the User role to {Email}: {Errors}",
                    email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = "Welcome to iCREAM! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        // ===== LOGOUT (POST only - prevents CSRF logout) =====
        [HttpPost]
        [Authorize]
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new
                {
                    email = user.Email,
                    token
                }, protocol: Request.Scheme);

                try
                {
                    await _emailSender.SendEmailAsync(
                        user.Email!,
                        "Reset your iCREAM password",
                        $"<p>Hello {(user.FullName ?? "there")},</p>" +
                        $"<p>Click the link below to reset your password. The link expires in 24 hours.</p>" +
                        $"<p><a href=\"{resetLink}\">Reset my password</a></p>" +
                        "<p>If you did not request this, you can safely ignore this e-mail.</p>");
                }
                catch (Exception ex)
                {
                    // Never reveal e-mail delivery problems to the caller.
                    _logger.LogError(ex, "Failed to send password reset e-mail to {Email}.", user.Email);
                }
            }

            // Same response whether or not the account exists (prevents account enumeration).
            TempData["Success"] = "If an account exists for that e-mail address, a password reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }

        // ===== RESET PASSWORD =====
        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordModel { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                // Do not reveal whether the account exists.
                TempData["Success"] = "Your password has been reset. Please sign in.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                TempData["Success"] = "Password reset successfully. Please sign in with your new password.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ===== ACCESS DENIED =====
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ===== REDIRECT TO LOCAL =====
        private async Task<IActionResult> RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
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

        // ===== PROFILE =====
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ===== UPDATE PROFILE DETAILS =====
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string? fullName, string? address, string? phoneNumber)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 3)
            {
                TempData["Error"] = "Please enter a valid full name (at least 3 characters).";
                return RedirectToAction(nameof(Profile));
            }

            user.FullName = fullName.Trim();
            user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            // Refresh the cookie so the new name appears immediately in the navbar.
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ===== UPDATE PROFILE PICTURE =====
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile? profilePicture)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var newPath = await FileUploadHelper.SaveImageAsync(
                    profilePicture, _webHostEnvironment, "images/profiles", maxBytes: 2 * 1024 * 1024);

                FileUploadHelper.DeleteIfExists(_webHostEnvironment, user.ProfilePicture);
                user.ProfilePicture = newPath;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    FileUploadHelper.DeleteIfExists(_webHostEnvironment, newPath);
                    TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Profile));
                }

                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Profile picture updated successfully!";
            }
            catch (FileUploadHelper.ValidationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile picture upload failed for user {UserId}.", userId);
                TempData["Error"] = "The image could not be uploaded. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}
