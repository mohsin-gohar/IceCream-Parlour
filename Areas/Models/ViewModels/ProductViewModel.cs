using Ice_Cream_Parlour_Eproject.Areas.Models;
using System.ComponentModel.DataAnnotations;

namespace Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public string? CategoryName { get; set; }

        [Required]
        [Range(0.01, 9999.99)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        public decimal FinalPrice => Price - (Price * DiscountPercent / 100);

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int LowStockThreshold { get; set; } = 10;

        public string? Barcode { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public List<string> ExistingImages { get; set; } = new();
        public List<IFormFile> NewImages { get; set; } = new();
        public PaginatedList<ProductViewModel>? Products { get; internal set; }
        public string? SearchTerm { get; internal set; }
        public int? CategoryFilter { get; internal set; }
        public string? ProductCode { get; internal set; }
        public string? Category { get; set; }
        public string? ImagePath { get; set; }
        public bool IsFree { get; set; }
    }

 }

    
