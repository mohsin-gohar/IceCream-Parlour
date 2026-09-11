using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Extension methods for Controller operations.
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Gets the current user ID from the claims principal.
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>The user ID or null if not authenticated</returns>
        public static string? GetCurrentUserId(this Controller controller)
        {
            return controller.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Gets the current user's name from the identity.
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>The user name or null if not authenticated</returns>
        public static string? GetCurrentUserName(this Controller controller)
        {
            return controller.User?.Identity?.Name;
        }

        /// <summary>
        /// Checks if the current user is in a specific role.
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <param name="role">The role to check</param>
        /// <returns>True if the user is in the role, false otherwise</returns>
        public static bool IsUserInRole(this Controller controller, string role)
        {
            return controller.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Checks if the user is authenticated.
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>True if user is authenticated, false otherwise</returns>
        public static bool IsUserAuthenticated(this Controller controller)
        {
            return controller.User?.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// Gets the user's email from claims.
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>The user's email or null if not found</returns>
        public static string? GetCurrentUserEmail(this Controller controller)
        {
            return controller.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
