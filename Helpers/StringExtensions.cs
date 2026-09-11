using System.Text.RegularExpressions;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Extension methods for string operations.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Checks if a string is null or empty.
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <returns>True if null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Checks if a string is null, empty, or contains only whitespace.
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <returns>True if null, empty, or whitespace, false otherwise</returns>
        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Validates an email address format.
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if valid email format, false otherwise</returns>
        public static bool IsValidEmail(this string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a phone number format (basic validation).
        /// </summary>
        /// <param name="phone">The phone number to validate</param>
        /// <returns>True if valid phone format, false otherwise</returns>
        public static bool IsValidPhoneNumber(this string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            try
            {
                var pattern = @"^\d{10,}$";
                return Regex.IsMatch(phone.Replace("-", "").Replace(" ", ""), pattern);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Truncates a string to a maximum length.
        /// </summary>
        /// <param name="value">The string to truncate</param>
        /// <param name="maxLength">The maximum length</param>
        /// <param name="suffix">The suffix to add if truncated (default: "...")</param>
        /// <returns>The truncated string</returns>
        public static string Truncate(this string? value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Slugifies a string for URLs.
        /// </summary>
        /// <param name="value">The string to slugify</param>
        /// <returns>The slugified string</returns>
        public static string Slugify(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim().ToLower();
            value = Regex.Replace(value, @"[^\w\s-]", "");
            value = Regex.Replace(value, @"[\s_]+", "-");
            value = Regex.Replace(value, @"-+", "-");

            return value.TrimStart('-').TrimEnd('-');
        }
    }
}
