using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Health;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Services;
using Ice_Cream_Parlour_Eproject.Services.Email;
using Ice_Cream_Parlour_Eproject.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
// Provider can be switched with "Database:Provider" (SqlServer | Sqlite).
// SqlServer is the default; Sqlite is useful for local development and CI.
var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("icecreamcs");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString ?? "Data Source=icecream.db");
    }
    else
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:icecreamcs' is not configured. " +
                "Set it in appsettings.json, user secrets, or the ConnectionStrings__icecreamcs environment variable.");
        }

        options.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            sql.CommandTimeout(60);
        });
    }
});

// ===== Application services =====
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ===== Email =====
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// ===== Identity with Roles =====
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password policy: balanced for a small business app, but strong enough for production.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;

    // Brute-force protection.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Adds FullName / avatar / membership claims so layouts do not hit the database on every request.
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "iCREAM.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ===== MVC =====
builder.Services.AddControllersWithViews(options =>
{
    // Every unsafe request must carry an anti-forgery token unless explicitly opted out.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();

// ===== Security / infrastructure =====
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// ===== Middleware pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();

// TLS termination is handled by the reverse proxy in production (IIS / nginx / Azure App Service).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/lib"))
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
        }
    }
});

// Baseline security headers.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");

// ===== Routes =====
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id:int?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id:int?}");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
        });
    }
});

// ===== Seed Roles and Users =====
await DbInitializer.InitializeAsync(app.Services, app.Logger, app.Configuration, app.Environment);

app.Run();
