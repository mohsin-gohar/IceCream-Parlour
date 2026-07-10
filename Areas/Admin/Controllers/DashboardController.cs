using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(),
                TotalProducts = await _context.Recipes.CountAsync(),  
                TotalCategories = await _context.Recipes.Select(r => r.Category).Distinct().CountAsync(),

                TodaySales = await _context.Orders
                    .Where(o => o.OrderDate.Date == DateTime.Today)
                    .SumAsync(o => o.TotalAmount),

                MonthlySales = await _context.Orders
                    .Where(o => o.OrderDate.Month == DateTime.Now.Month)
                    .SumAsync(o => o.TotalAmount),

                // Revenue Chart Data (Last 7 Days)
                RevenueChartLabels = GetLast7DaysLabels(),
                RevenueChartData = await GetLast7DaysData(),

                // Low Stock Products (Recipes se)
                LowStockProducts = await _context.Recipes
                    .Where(r => r.IsFree == false)  // Premium recipes
                    .Select(r => new LowStockProductDto
                    {
                        Name = r.Name,
                        StockQuantity = 0,  // Recipes don't have stock, use boolean
                        LowStockThreshold = 0
                    })
                    .Take(5)
                    .ToListAsync(),

                // Top Selling Products (Orders se)
                TopSellingProducts = await _context.Orders
                    .GroupBy(o => o.BookId)
                    .Select(g => new TopSellingProductDto
                    {
                        ProductName = _context.Books.Where(b => b.Id == g.Key).Select(b => b.Title).FirstOrDefault() ?? "Unknown",
                        TotalSold = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(5)
                    .ToListAsync()
            };

            // Calculate Progress Percent
            if (model.TopSellingProducts.Any())
            {
                var maxSold = model.TopSellingProducts.Max(x => x.TotalSold);
                foreach (var item in model.TopSellingProducts)
                {
                    item.ProgressPercent = maxSold > 0 ? (int)((double)item.TotalSold / maxSold * 100) : 0;  // ✅ int me convert
                } 
            }

            return View(model);
        }

        private List<string> GetLast7DaysLabels()
        {
            return Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i).ToString("ddd"))
                .Reverse()
                .ToList();
        }

        private async Task<List<decimal>> GetLast7DaysData()
        {
            var data = new List<decimal>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var total = await _context.Orders
                    .Where(o => o.OrderDate.Date == date.Date)
                    .SumAsync(o => o.TotalAmount);
                data.Add(total);
            }
            return data;
        }

        // ===== PRODUCT MANAGEMENT (Recipes) =====
        public async Task<IActionResult> Products()
        {
            var products = await _context.Recipes.ToListAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Recipe recipe, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Image Upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var folder = "images/recipes/";
                    var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    recipe.ImagePath = "/" + folder + fileName;
                }

                recipe.CreatedDate = DateTime.Now;
                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Product added successfully!";
                return RedirectToAction("Products");
            }
            return View(recipe);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Recipes.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Recipe recipe, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.Recipes.FindAsync(recipe.Id);
                if (existing == null) return NotFound();

                existing.Name = recipe.Name;
                existing.Category = recipe.Category;
                existing.Ingredients = recipe.Ingredients;
                existing.Procedure = recipe.Procedure;
                existing.IsFree = recipe.IsFree;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var folder = "images/recipes/";
                    var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    existing.ImagePath = "/" + folder + fileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction("Products");
            }
            return View(recipe);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Recipes.FindAsync(id);
            if (product != null)
            {
                _context.Recipes.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product deleted successfully!";
            }
            return RedirectToAction("Products");
        }
    }

    }