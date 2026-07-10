using Microsoft.AspNetCore.Identity;

namespace Ice_Cream_Parlour_Eproject.Models
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? PaymentType { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? PaymentExpiry { get; set; }
        public bool IsPaid { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? ProfilePicture { get; set; }
    }
}