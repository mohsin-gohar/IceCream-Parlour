using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ice_Cream_Parlour_Eproject.Models
{
    public class Recipe
    {
        internal DateTime UpdatedDate;

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Recipe name is required")]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Ingredients { get; set; } = string.Empty;

        [Required]
        public string Procedure { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public bool IsFree { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        public decimal Price { get; set; }

    }
}