using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Data
{
    /// <summary>
    /// Creates the default roles and (optionally) the bootstrap admin account.
    /// Seeding never crashes the application: if the database is unavailable the
    /// error is logged and startup continues so the problem can be diagnosed via /health.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            IServiceProvider services,
            ILogger logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            try
            {
                var db = provider.GetRequiredService<ApplicationDbContext>();

                if (db.Database.IsSqlite())
                {
                    // SQL Server migrations cannot be applied to SQLite; create the schema directly.
                    await db.Database.EnsureCreatedAsync();
                }

                var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = provider.GetRequiredService<UserManager<User>>();

                foreach (var role in new[] { AppRoles.Admin, AppRoles.Manager, AppRoles.Staff, AppRoles.User })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                if (configuration.GetValue("Seed:DemoData", false))
                {
                    await SeedDemoDataAsync(db);
                }

                await SyncProductsFromRecipesAsync(db, logger);
                var seedEnabled = configuration.GetValue("Seed:Enabled", true);
                if (!seedEnabled)
                {
                    return;
                }

                var adminEmail = configuration["Seed:AdminEmail"];
                var adminPassword = configuration["Seed:AdminPassword"];
                var userEmail = configuration["Seed:UserEmail"];
                var userPassword = configuration["Seed:UserPassword"];

                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    logger.LogInformation("Seed credentials are not configured; skipping demo user creation.");
                    return;
                }

                var admin = await userManager.FindByEmailAsync(adminEmail);
                if (admin == null)
                {
                    admin = new User
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FullName = "Administrator",
                        Address = "Admin Office",
                        IsPaid = true,
                        PaymentType = "Yearly",
                        PaymentDate = DateTime.UtcNow,
                        PaymentExpiry = DateTime.UtcNow.AddYears(1)
                    };

                    var result = await userManager.CreateAsync(admin, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                        logger.LogInformation("Seeded administrator account {Email}.", adminEmail);

                        if (environment.IsProduction())
                        {
                            logger.LogWarning(
                                "A default administrator account was created. Change the password immediately " +
                                "and remove Seed:AdminPassword from configuration.");
                        }
                    }
                    else
                    {
                        logger.LogError("Could not seed administrator: {Errors}",
                            string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }

                if (!string.IsNullOrWhiteSpace(userEmail) && !string.IsNullOrWhiteSpace(userPassword))
                {
                    var regular = await userManager.FindByEmailAsync(userEmail);
                    if (regular == null)
                    {
                        regular = new User
                        {
                            UserName = userEmail,
                            Email = userEmail,
                            EmailConfirmed = true,
                            FullName = "Regular User",
                            Address = "User Address",
                            IsPaid = false,
                            PaymentType = "Monthly",
                            PaymentDate = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(regular, userPassword);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(regular, AppRoles.User);
                            logger.LogInformation("Seeded demo user account {Email}.", userEmail);
                        }
                        else
                        {
                            logger.LogError("Could not seed demo user: {Errors}",
                                string.Join("; ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database initialisation failed. The application will keep running; check /health.");
            }
        }

        private static async Task SeedDemoDataAsync(ApplicationDbContext db)
        {
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Areas.Models.Category { Name = "Classic", Description = "Timeless ice cream flavours", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Areas.Models.Category { Name = "Premium", Description = "Small batch, extra creamy", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Areas.Models.Category { Name = "Sorbet", Description = "Dairy free and refreshing", IsActive = true, CreatedDate = DateTime.UtcNow });
            }

            if (!await db.Recipes.AnyAsync())
            {
                db.Recipes.AddRange(
                    new Recipe
                    {
                        Name = "Madagascar Vanilla",
                        Category = "Classic",
                        Ingredients = "2 cups heavy cream, 1 cup whole milk, 3/4 cup sugar, 1 vanilla bean",
                        Procedure = "Heat the milk and cream with the vanilla bean. Whisk in the sugar until dissolved, chill for 4 hours, then churn until creamy.",
                        ImagePath = "/img/product-1.jpg",
                        IsFree = true,
                        Price = 3.99m,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Recipe
                    {
                        Name = "Belgian Chocolate",
                        Category = "Premium",
                        Ingredients = "2 cups heavy cream, 1 cup milk, 1/2 cup cocoa, 150g dark chocolate, 3/4 cup sugar",
                        Procedure = "Melt the chocolate into the warm milk mixture, whisk in cocoa and sugar, chill overnight and churn.",
                        ImagePath = "/img/product-2.jpg",
                        IsFree = false,
                        Price = 4.99m,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Recipe
                    {
                        Name = "Mango Sorbet",
                        Category = "Sorbet",
                        Ingredients = "4 ripe mangoes, 1/2 cup sugar, 1 tbsp lemon juice, 1/2 cup water",
                        Procedure = "Blend the mangoes with the sugar syrup and lemon juice, chill and churn until smooth.",
                        ImagePath = "/img/product-4.jpg",
                        IsFree = true,
                        Price = 3.49m,
                        CreatedDate = DateTime.UtcNow
                    });
            }

            if (!await db.Books.AnyAsync())
            {
                db.Books.AddRange(
                    new Book
                    {
                        Title = "The Art of Homemade Ice Cream",
                        Author = "iCREAM Kitchen",
                        Description = "60 recipes from our parlour, from classic custard bases to modern sorbets.",
                        Price = 24.99m,
                        StockQuantity = 25,
                        ImagePath = "/images/books/09b89ffb-5590-4aad-8e8d-98e8d30427dc_product-11.png"
                    },
                    new Book
                    {
                        Title = "Frozen Desserts Masterclass",
                        Author = "iCREAM Kitchen",
                        Description = "Techniques, ratios and troubleshooting for perfect frozen desserts at home.",
                        Price = 19.99m,
                        StockQuantity = 40,
                        ImagePath = "/images/books/2fd470ea-e900-47c8-8c85-9375be633873_service-1.jpg"
                    },
                    new Book
                    {
                        Title = "Scoops for Every Season",
                        Author = "iCREAM Kitchen",
                        Description = "Seasonal flavour pairings and garnishes for every month of the year.",
                        Price = 15.99m,
                        StockQuantity = 15,
                        ImagePath = "/images/books/ecd73288-ca15-434d-902b-6461d8fe8054_gallery2.jpg"
                    });
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// The POS/order module works with the Product entity while the public menu is
        /// built from Recipes. Bootstrap matching products once so admin order creation
        /// is usable out of the box (existing rows are never touched).
        /// </summary>
        private static async Task SyncProductsFromRecipesAsync(ApplicationDbContext db, ILogger logger)
        {
            var recipes = await db.Recipes.ToListAsync();
            if (recipes.Count == 0)
            {
                return;
            }

            var categories = await db.Categories.ToListAsync();
            var added = 0;

            foreach (var recipe in recipes)
            {
                // Only add missing entries so manual product edits are preserved.
                if (await db.Products.AnyAsync(p => p.Name == recipe.Name))
                {
                    continue;
                }

                var category = categories.FirstOrDefault(c =>
                    string.Equals(c.Name, recipe.Category, StringComparison.OrdinalIgnoreCase));

                if (category == null)
                {
                    category = new Areas.Models.Category
                    {
                        Name = recipe.Category,
                        Description = $"Auto-created from recipe category '{recipe.Category}'",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    db.Categories.Add(category);
                    categories.Add(category);
                    await db.SaveChangesAsync();
                }

                db.Products.Add(new Areas.Models.Product
                {
                    Name = recipe.Name,
                    Description = recipe.Procedure.Length > 500 ? recipe.Procedure[..500] : recipe.Procedure,
                    CategoryId = category.Id,
                    Price = recipe.Price > 0 ? recipe.Price : 9.99m,
                    DiscountPercent = 0,
                    StockQuantity = 100,
                    LowStockThreshold = 10,
                    Barcode = await BarcodeHelper.GenerateUniqueBarcodeAsync(code => db.Products.AnyAsync(p => p.Barcode == code)),
                    ImagePath = recipe.ImagePath,
                    Status = Areas.Models.ProductStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });

                added++;
            }

            await db.SaveChangesAsync();
            if (added > 0)
            {
                logger.LogInformation("Synced {Count} product(s) from the recipe catalogue.", added);
            }
        }
    }
}
