using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // ===== DbSets =====
        public DbSet<Category> Categories { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<UserRecipe> UserRecipes { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQLite has no native decimal type, so money columns are stored as REAL
            // there (development / test only). SQL Server keeps exact decimal precision.
            var isSqlite = Database.IsSqlite();

            var orderAmount = modelBuilder.Entity<Order>().Property(o => o.TotalAmount);
            var productPrice = modelBuilder.Entity<Product>().Property(p => p.Price);
            var productDiscount = modelBuilder.Entity<Product>().Property(p => p.DiscountPercent);
            var bookPrice = modelBuilder.Entity<Book>().Property(b => b.Price);
            var recipePrice = modelBuilder.Entity<Recipe>().Property(r => r.Price);
            var prizeMoney = modelBuilder.Entity<UserRecipe>().Property(ur => ur.PrizeMoney);
            var unitPrice = modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice);

            if (isSqlite)
            {
                orderAmount.HasConversion<double>();
                productPrice.HasConversion<double>();
                productDiscount.HasConversion<double>();
                bookPrice.HasConversion<double>();
                recipePrice.HasConversion<double>();
                prizeMoney.HasConversion<double?>();
                unitPrice.HasConversion<double>();
            }
            else
            {
                orderAmount.HasPrecision(18, 2);
                productPrice.HasPrecision(18, 2);
                productDiscount.HasPrecision(5, 2);
                bookPrice.HasPrecision(18, 2);
                recipePrice.HasPrecision(18, 2);
                prizeMoney.HasPrecision(18, 2);
                unitPrice.HasPrecision(18, 2);
            }

            // Preserve the original table name from the initial migration.
            modelBuilder.Entity<OrderItem>().ToTable("OrderItem");

            // ===== Uniqueness / lookup indexes =====
            // Uniqueness is enforced by OrderNumberHelper before insert; a non-unique
            // index keeps the migration safe for databases that already contain duplicates.
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerEmail);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode);

            modelBuilder.Entity<Recipe>()
                .HasIndex(r => r.Category);

            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.SubmittedDate);

            modelBuilder.Entity<UserRecipe>()
                .HasIndex(ur => ur.Status);

            // ===== Relationships =====
            // Deleting a book must not delete historic orders.
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Book)
                .WithMany()
                .HasForeignKey(o => o.BookId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
