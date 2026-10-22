using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var model = new DashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                TodaySales = await _context.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                MonthlySales = await _context.Orders
                    .Where(o => o.OrderDate >= startOfMonth && o.OrderDate < tomorrow)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                YearlySales = await _context.Orders
                    .Where(o => o.OrderDate >= startOfYear && o.OrderDate < tomorrow)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0
            };

            // Order status counters in a single round trip.
            var statusCounts = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int CountFor(string status) => statusCounts
                .FirstOrDefault(s => string.Equals(s.Status, status, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;

            model.PendingOrders = CountFor("Pending");
            model.ProcessingOrders = CountFor("Processing");
            model.ShippedOrders = CountFor("Shipped");
            model.CompletedOrders = CountFor("Completed") + CountFor("Delivered");

            // Last 7 days revenue - aggregated in memory to stay provider agnostic.
            var chartStart = today.AddDays(-6);
            var chartRows = await _context.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= chartStart && o.OrderDate < tomorrow)
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            var revenueByDay = chartRows
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

            for (var i = 0; i < 7; i++)
            {
                var date = chartStart.AddDays(i);
                model.RevenueChartLabels.Add(date.ToString("ddd dd"));
                model.RevenueChartData.Add(revenueByDay.TryGetValue(date, out var total) ? total : 0);
            }

            // Low stock alerts from the real product catalogue.
            model.LowStockProducts = await _context.Products.AsNoTracking()
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StockQuantity = p.StockQuantity,
                    LowStockThreshold = p.LowStockThreshold,
                    StockPercent = p.LowStockThreshold > 0
                        ? (int)(p.StockQuantity * 100.0 / p.LowStockThreshold)
                        : 0
                })
                .ToListAsync();

            // Best sellers from real order lines.
            var topSelling = await _context.OrderItems.AsNoTracking()
                .GroupBy(i => i.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalSold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var maxSold = topSelling.Count > 0 ? topSelling.Max(x => x.TotalSold) : 0;
            model.TopSellingProducts = topSelling.Select(x => new TopSellingProductDto
            {
                ProductName = string.IsNullOrWhiteSpace(x.ProductName) ? "Unknown" : x.ProductName,
                TotalSold = x.TotalSold,
                Revenue = x.Revenue,
                ProgressPercent = maxSold > 0 ? (int)(x.TotalSold * 100.0 / maxSold) : 0
            }).ToList();

            model.RecentOrders = await _context.Orders.AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus
                })
                .ToListAsync();

            return model;
        }
    }
}
