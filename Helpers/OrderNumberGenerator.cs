using System.Security.Cryptography;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    public static class OrderNumberGenerator
    {
        /// <summary>Creates a human friendly, hard to guess order number.</summary>
        public static string Generate()
        {
            var suffix = RandomNumberGenerator.GetInt32(100000, 1000000);
            return $"ORD-{DateTime.Now:yyyyMMdd}-{suffix}";
        }
    }
}
