using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Data
{
    /// <summary>
    /// Applies pending migrations and creates the roles / starter accounts.
    /// Everything is configuration driven so demo credentials are never
    /// hard-coded in production.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var context = provider.GetRequiredService<ApplicationDbContext>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = provider.GetRequiredService<UserManager<User>>();

            if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                try
                {
                    if (context.Database.IsRelational())
                    {
                        await context.Database.MigrateAsync();
                        logger.LogInformation("Database migrations applied successfully.");
                    }
                    else
                    {
                        await context.Database.EnsureCreatedAsync();
                        logger.LogInformation("Database created (non-relational provider).");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Database initialisation failed. The application will continue to start.");
                }
            }

            try
            {
                foreach (var role in new[] { AppRoles.Admin, AppRoles.Manager, AppRoles.Staff, AppRoles.User })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Created role {Role}.", role);
                    }
                }

                if (!configuration.GetValue<bool>("Seed:Enabled"))
                {
                    return;
                }

                var adminEmail = configuration["Seed:AdminEmail"];
                var adminPassword = configuration["Seed:AdminPassword"];

                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                {
                    await EnsureUserAsync(userManager, logger, new User
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = configuration["Seed:AdminFullName"] ?? "Administrator",
                        Address = "Head Office",
                        IsPaid = true,
                        PaymentType = "Yearly",
                        PaymentDate = DateTime.Now,
                        PaymentExpiry = DateTime.Now.AddYears(1),
                        EmailConfirmed = true
                    }, adminPassword, AppRoles.Admin);
                }

                if (configuration.GetValue<bool>("Seed:DemoUserEnabled"))
                {
                    var demoEmail = configuration["Seed:DemoUserEmail"];
                    var demoPassword = configuration["Seed:DemoUserPassword"];

                    if (!string.IsNullOrWhiteSpace(demoEmail) && !string.IsNullOrWhiteSpace(demoPassword))
                    {
                        await EnsureUserAsync(userManager, logger, new User
                        {
                            UserName = demoEmail,
                            Email = demoEmail,
                            FullName = "Demo Customer",
                            Address = "Demo Street 1",
                            IsPaid = false,
                            EmailConfirmed = true
                        }, demoPassword, AppRoles.User);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seeding roles/users failed.");
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<User> userManager,
            ILogger logger,
            User template,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(template.Email!);
            if (user == null)
            {
                var result = await userManager.CreateAsync(template, password);
                if (!result.Succeeded)
                {
                    logger.LogWarning("Could not create seed user {Email}: {Errors}",
                        template.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
                    return;
                }

                user = template;
                logger.LogInformation("Created seed user {Email}.", template.Email);
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
