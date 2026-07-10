using System.ComponentModel.DataAnnotations;

namespace Ice_Cream_Parlour_Eproject.Models
{
    public class GalleryItemViewModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string? ImagePath { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string? Author { get; set; }
        public string Type { get; set; } = "recipe"; 
        public string? Category { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}