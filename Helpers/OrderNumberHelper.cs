using System.Security.Cryptography;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    public static class OrderNumberHelper
    {
        /// <summary>
        /// Creates a human readable, collision resistant order number such as
        /// ORD-20261022-7F3A2B. The callback is used to guarantee uniqueness at the
        /// database level before the order is saved.
        /// </summary>
        public static async Task<string> GenerateUniqueAsync(
            Func<string, Task<bool>> exists,
            DateTime? date = null,
            int maxAttempts = 10)
        {
            var day = (date ?? DateTime.UtcNow).ToString("yyyyMMdd");

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
                var candidate = $"ORD-{day}-{random}";

                if (!await exists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique order number. Please try again.");
        }
    }
}
