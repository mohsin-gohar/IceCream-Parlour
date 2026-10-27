using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            // ===== Categories =====
            var categoryNames = new (string Name, string? Description)[]
            {
                ("Ice Cream", "Creamy frozen desserts made from milk, cream and sugar."),
                ("Milkshake", "Blended frozen mixes of ice cream, milk and syrups."),
                ("Sorbet", "Light, fruit-flavored frozen desserts without dairy."),
                ("Frozen Yogurt", "Frozen yogurt blended with fruit and sweeteners."),
                ("Frozen Pops", "Handcrafted fruit pops and ice lollies.")
            };

            var categoryIdMap = new Dictionary<string, int>();
            foreach (var category in categoryNames)
            {
                var existingCategory = db.Categories.FirstOrDefault(x => x.Name == category.Name);
                if (existingCategory == null)
                {
                    db.Categories.Add(new Category
                    {
                        Name = category.Name,
                        Description = category.Description,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    });
                    await db.SaveChangesAsync();
                    existingCategory = db.Categories.First(x => x.Name == category.Name);
                }
                categoryIdMap[category.Name] = existingCategory.Id;
            }

            // ===== Products =====
            var products = new List<Product>
            {
                new Product { Name = "Classic Vanilla Ice Cream", Description = "Silky smooth vanilla bean ice cream made with farm fresh milk and cream.", CategoryId = 0, Price = 5.99m, DiscountPercent = 0, StockQuantity = 120, LowStockThreshold = 10, Barcode = "100000000001", ProductCode = "PRD-0001", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/classic-vanilla.jpg" },
                new Product { Name = "Chocolate Hazelnut Truffle", Description = "Rich chocolate ice cream swirled with roasted hazelnut paste.", CategoryId = 0, Price = 7.49m, DiscountPercent = 10, StockQuantity = 85, LowStockThreshold = 10, Barcode = "100000000002", ProductCode = "PRD-0002", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/chocolate-hazelnut.jpg" },
                new Product { Name = "Strawberry Swirl", Description = "Fresh strawberry ice cream with ribbons of strawberry coulis.", CategoryId = 0, Price = 6.49m, DiscountPercent = 0, StockQuantity = 64, LowStockThreshold = 10, Barcode = "100000000003", ProductCode = "PRD-0003", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/strawberry-swirl.jpg" },
                new Product { Name = "Oreo Cookies and Cream", Description = "Classic chocolate cookie dough blended into creamy vanilla ice cream.", CategoryId = 0, Price = 6.99m, DiscountPercent = 0, StockQuantity = 150, LowStockThreshold = 10, Barcode = "100000000005", ProductCode = "PRD-0005", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/oreo-cookies-cream.jpg" },
                new Product { Name = "Iced Coffee Milkshake", Description = "Espresso ice cream blended with whole milk and whipped cream.", CategoryId = 1, Price = 8.99m, DiscountPercent = 5, StockQuantity = 96, LowStockThreshold = 10, Barcode = "100000000006", ProductCode = "PRD-0006", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/iced-coffee-milkshake.jpg" },
                new Product { Name = "Salted Caramel Milkshake", Description = "Vanilla ice cream, caramel sauce and flaky sea salt in a thick shake.", CategoryId = 1, Price = 9.49m, DiscountPercent = 0, StockQuantity = 73, LowStockThreshold = 10, Barcode = "100000000007", ProductCode = "PRD-0007", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/salted-caramel.jpg" },
                new Product { Name = "Mango Sorbet", Description = "Naturally sweet mango sorbet churned fresh with lime juice.", CategoryId = 2, Price = 5.49m, DiscountPercent = 0, StockQuantity = 58, LowStockThreshold = 10, Barcode = "100000000008", ProductCode = "PRD-0008", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/mango-sorbet.jpg" },
                new Product { Name = "Passion Fruit Sorbet", Description = "Zesty passion fruit sorbet with real passion fruit pulp.", CategoryId = 2, Price = 5.49m, DiscountPercent = 0, StockQuantity = 47, LowStockThreshold = 10, Barcode = "100000000009", ProductCode = "PRD-0009", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/passion-fruit-sorbet.jpg" },
                new Product { Name = "Raspberry Frozen Yogurt", Description = "Creamy Greek yogurt blended with raspberry swirl and seeds.", CategoryId = 3, Price = 6.99m, DiscountPercent = 10, StockQuantity = 66, LowStockThreshold = 10, Barcode = "100000000010", ProductCode = "PRD-0010", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/raspberry-yogurt.jpg" },
                new Product { Name = "Pistachio Frozen Yogurt", Description = "Smooth pistachio yogurt frozen with roasted pistachio paste.", CategoryId = 3, Price = 7.49m, DiscountPercent = 0, StockQuantity = 39, LowStockThreshold = 10, Barcode = "100000000011", ProductCode = "PRD-0011", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/pistachio-yogurt.jpg" },
                new Product { Name = "Watermelon Frozen Pop", Description = "Fresh watermelon ice pop on a wooden stick.", CategoryId = 4, Price = 2.99m, DiscountPercent = 0, StockQuantity = 200, LowStockThreshold = 20, Barcode = "100000000012", ProductCode = "PRD-0012", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/watermelon-pop.jpg" },
                new Product { Name = "Blueberry Frozen Pop", Description = "Wild blueberry ice pop with real berry chunks.", CategoryId = 4, Price = 2.99m, DiscountPercent = 0, StockQuantity = 180, LowStockThreshold = 20, Barcode = "100000000013", ProductCode = "PRD-0013", Status = ProductStatus.OutOfStock, CreatedAt = DateTime.Now, ImagePath = "/images/products/blueberry-pop.jpg" },
                new Product { Name = "Honey Lavender Ice Cream", Description = "Lavender infused ice cream finished with local honey.", CategoryId = 0, Price = 8.49m, DiscountPercent = 20, StockQuantity = 28, LowStockThreshold = 10, Barcode = "100000000014", ProductCode = "PRD-0014", Status = ProductStatus.Active, CreatedAt = DateTime.Now, ImagePath = "/images/products/honey-lavender.jpg" }
            };

            foreach (var product in products)
            {
                var categoryName = db.Categories.Where(c => c.Id == product.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "Ice Cream";
                product.CategoryId = categoryIdMap[categoryName];
                if (!db.Products.Any(p => p.Barcode == product.Barcode))
                {
                    db.Products.Add(product);
                    await db.SaveChangesAsync();
                }
            }

            // ===== Fake Feedbacks =====
            var feedbacks = new List<Feedback>
            {
                new Feedback { UserId = "1", UserName = "Ali Raza", Email = "ali.raza@example.com", Message = "Best ice cream in town! The mango coconut flavor is amazing.", Rating = 5, SubmittedDate = DateTime.Now.AddDays(-5), IsRegistered = true, IsRead = true },
                new Feedback { UserId = "2", UserName = "Sara Khan", Email = "sara.khan@example.com", Message = "Loved the chocolate hazelnut truffle, creamy and rich.", Rating = 4, SubmittedDate = DateTime.Now.AddDays(-3), IsRegistered = true, IsRead = true },
                new Feedback { UserId = "3", UserName = "Bilal Ahmed", Email = "bilal.ahmed@example.com", Message = "Great sorbet, very refreshing on hot summer days.", Rating = 5, SubmittedDate = DateTime.Now.AddDays(-1), IsRegistered = false, IsRead = false },
                new Feedback { UserId = "4", UserName = "Zara Sultana", Email = "zara.sultana@example.com", Message = "Delivery was fast and the strawberry swirl was delicious.", Rating = 4, SubmittedDate = DateTime.Now.AddDays(-2), IsRegistered = true, IsRead = true }
            };

            foreach (var feedback in feedbacks)
            {
                if (!db.Feedbacks.Any(f => f.Email == feedback.Email))
                {
                    db.Feedbacks.Add(feedback);
                    await db.SaveChangesAsync();
                }
            }

            // ===== Fake Books =====
            var books = new List<Book>
            {
                new Book { Title = "The Art of Homemade Ice Cream", Author = "Chef Michel Blanc", Description = "A complete guide to classic and gourmet ice cream making at home.", Price = 29.99m, StockQuantity = 25, ImagePath = "/images/books/art-of-ice-cream.jpg" },
                new Book { Title = "Frozen Treats for Everyone", Author = "Emma Wilson", Description = "Easy no-churn ice cream recipes for families and kids.", Price = 24.99m, StockQuantity = 40, ImagePath = "/images/books/frozen-treats.jpg" },
                new Book { Title = "Gelato Secrets", Author = "Luca Romano", Description = "Professional gelato techniques from Italy.", Price = 39.99m, StockQuantity = 12, ImagePath = "/images/books/gelato-secrets.jpg" }
            };

            foreach (var book in books)
            {
                if (!db.Books.Any(b => b.Title == book.Title))
                {
                    db.Books.Add(book);
                    await db.SaveChangesAsync();
                }
            }

            // ===== Fake Customers (required FK for Orders) =====
            var customers = new List<Customer>
            {
                new Customer { FullName = "Ali Raza", Email = "ali.raza@example.com", Phone = "0300123456", Address = "Street 1, Lahore", IsActive = true, CreatedAt = DateTime.Now.AddDays(-10), LastActivityDate = DateTime.Now.AddDays(-7) },
                new Customer { FullName = "Sara Khan", Email = "sara.khan@example.com", Phone = "0300987654", Address = "Street 2, Karachi", IsActive = true, CreatedAt = DateTime.Now.AddDays(-8), LastActivityDate = DateTime.Now.AddDays(-5) },
                new Customer { FullName = "Bilal Ahmed", Email = "bilal.ahmed@example.com", Phone = "0300556677", Address = "Street 3, Islamabad", IsActive = true, CreatedAt = DateTime.Now.AddDays(-5), LastActivityDate = DateTime.Now.AddDays(-2) }
            };

            foreach (var customer in customers)
            {
                if (!db.Customers.Any(c => c.Email == customer.Email))
                {
                    db.Customers.Add(customer);
                    await db.SaveChangesAsync();
                }
            }

            // ===== Fake Orders =====
            var orders = new List<Order>
            {
                new Order { OrderNumber = "ORD-1001", CustomerId = 1, CustomerName = "Ali Raza", CustomerEmail = "ali.raza@example.com", PaymentMethod = "CreditCard", TotalAmount = 19.97m, OrderStatus = "Delivered", PaymentStatus = "Paid", OrderDate = DateTime.Now.AddDays(-7) },
                new Order { OrderNumber = "ORD-1002", CustomerId = 2, CustomerName = "Sara Khan", CustomerEmail = "sara.khan@example.com", PaymentMethod = "CreditCard", TotalAmount = 26.47m, OrderStatus = "Delivered", PaymentStatus = "Paid", OrderDate = DateTime.Now.AddDays(-5) },
                new Order { OrderNumber = "ORD-1003", CustomerId = 3, CustomerName = "Bilal Ahmed", CustomerEmail = "bilal.ahmed@example.com", PaymentMethod = "CashOnDelivery", TotalAmount = 13.97m, OrderStatus = "Pending", PaymentStatus = "Pending", OrderDate = DateTime.Now.AddDays(-2) }
            };

            foreach (var order in orders)
            {
                if (!db.Orders.Any(o => o.CustomerEmail == order.CustomerEmail && o.OrderDate == order.OrderDate))
                {
                    db.Orders.Add(order);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
