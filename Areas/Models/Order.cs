using Ice_Cream_Parlour_Eproject.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ice_Cream_Parlour_Eproject.Areas.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // ===== Customer Relationship =====
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        // ===== Denormalized Customer Info =====
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone]
        public string? CustomerPhone { get; set; }

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        // ===== Order Details =====
        [Required]
        [Range(0.01, 99999.99)]
        public decimal TotalAmount { get; set; }

        [Required]
        public string OrderStatus { get; set; } = "Pending";

        [Required]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? Notes { get; set; }

        // ===== Book Relationship (Optional) =====
        public int? BookId { get; set; }

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }

        // ===== Order Items =====
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Optional link to a catalogue product. Orders can also contain
        // free-form line items (books / recipes) which have no Product row.
        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, 9999.99)]
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}