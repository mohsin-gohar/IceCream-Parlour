using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ice_Cream_Parlour_Eproject.Areas.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Required]
        [Range(0.01, 9999.99)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int LowStockThreshold { get; set; } = 10;
        [Required]
        [StringLength(12, MinimumLength = 12)]
        public string? Barcode { get; set; }
        public string? ProductCode { get; set; }  
        public string? ImagePath { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }



    public enum ProductStatus
    {
        Active,
        Inactive,
        OutOfStock,
        Discontinued
    }
}