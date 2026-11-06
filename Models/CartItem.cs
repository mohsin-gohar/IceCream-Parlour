namespace Ice_Cream_Parlour_Eproject.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
