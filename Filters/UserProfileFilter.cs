using System.Security.Claims;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Filters
{
    /// <summary>
    /// Loads the signed-in user once per request and exposes the layout data
    /// through ViewData. This replaces the synchronous database calls that used
    /// to live inside _Layout.cshtml (they ran twice on every single page).
    /// </summary>
    public class UserProfileFilter : IAsyncResultFilter
    {
        private const string CacheKey = "IceCream_CurrentUser";
        private readonly ApplicationDbContext _context;

        public UserProfileFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ViewResult viewResult)
            {
                var viewData = viewResult.ViewData;
                viewData["ShowMembershipBanner"] = true;

                var identity = context.HttpContext.User.Identity;
                if (identity?.IsAuthenticated == true)
                {
                    var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        User? user = null;

                        if (context.HttpContext.Items[CacheKey] is User cached)
                        {
                            user = cached;
                        }
                        else
                        {
                            user = await _context.Users.AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Id == userId);

                            if (user != null)
                            {
                                context.HttpContext.Items[CacheKey] = user;
                            }
                        }

                        if (user != null)
                        {
                            var displayName = string.IsNullOrWhiteSpace(user.FullName)
                                ? user.UserName ?? string.Empty
                                : user.FullName!;

                            viewData["CurrentUser"] = user;
                            viewData["DisplayName"] = displayName;
                            viewData["AvatarImage"] = user.ProfilePicture ?? string.Empty;
                            viewData["UserInitials"] = BuildInitials(displayName);

                            var membershipActive = user.IsPaid
                                && user.PaymentExpiry.HasValue
                                && user.PaymentExpiry.Value > DateTime.Now;

                            viewData["ShowMembershipBanner"] = !membershipActive;
                        }
                    }
                }
            }

            await next();
        }

        private static string BuildInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "U";
            }

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return (char.ToUpperInvariant(parts[0][0]).ToString()
                        + char.ToUpperInvariant(parts[^1][0])).ToUpperInvariant();
            }

            return char.ToUpperInvariant(parts[0][0]).ToString();
        }
    }
}
