using Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Middleware;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Services;
using Ice_Cream_Parlour_Eproject.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

try
{
    // ===== Serilog Configuration =====
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File("logs/icecream-parlour-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "IceCreamParlour")
        .CreateLogger();

    builder.Host.UseSerilog();

    // ===== Database Context =====
    var useSqlite = builder.Environment.IsDevelopment();
    if (useSqlite)
    {
        var sqliteConnectionString = builder.Configuration.GetConnectionString("SQLite");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("IceCreamParlourDb");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }

    // ===== Register Services =====
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();

    // ===== Identity with Roles =====
    builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // ===== Session =====
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // ===== MVC =====
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();
    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // ===== Middleware =====
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();

    // ===== Routes =====
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();

    // ===== Seed Database (Create Schema + Fake Data) =====
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(db);

        // ===== Seed Roles and Users =====
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // --- Create Roles ---
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // --- Create Admin User ---
        var adminEmail = "admin@icream.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin",
                Address = "Admin Office",
                IsPaid = true,
                PaymentType = "Yearly",
                PaymentDate = DateTime.Now,
                PaymentExpiry = DateTime.Now.AddYears(1)
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // --- Create Regular User ---
        var userEmail = "user@icream.com";
        var regularUser = await userManager.FindByEmailAsync(userEmail);
        if (regularUser == null)
        {
            regularUser = new User
            {
                UserName = userEmail,
                Email = userEmail,
                FullName = "Regular User",
                Address = "User Address",
                IsPaid = false,
                PaymentType = "Monthly",
                PaymentDate = DateTime.Now,
                PaymentExpiry = DateTime.Now.AddMonths(1)
            };
            var result = await userManager.CreateAsync(regularUser, "User@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(regularUser, "User");
        }
    }

    Log.Information("Application started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
