using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class DashboardService : IDashboardService
    {
        private static readonly string[] ExcludedStatuses =
        {
            OrderStatus.Cancelled.ToString(),
            OrderStatus.Refunded.ToString()
        };

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
            var chartStart = today.AddDays(-6);

            var revenueQuery = _context.Orders
                .AsNoTracking()
                .Where(o => !ExcludedStatuses.Contains(o.OrderStatus));

            var model = new DashboardViewModel
            {
                TotalOrders = await _context.Orders.AsNoTracking().CountAsync(),
                TotalCustomers = await _context.Customers.AsNoTracking().CountAsync(),
                TotalProducts = await _context.Recipes.AsNoTracking().CountAsync(),
                TotalCategories = await _context.Categories.AsNoTracking().CountAsync(),
                PendingOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == OrderStatus.Pending.ToString()),
                ProcessingOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == OrderStatus.Processing.ToString()),
                ShippedOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == OrderStatus.Shipped.ToString()),
                CompletedOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.OrderStatus == OrderStatus.Completed.ToString()),
                TodaySales = await revenueQuery.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                MonthlySales = await revenueQuery.Where(o => o.OrderDate >= startOfMonth && o.OrderDate < tomorrow).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                YearlySales = await revenueQuery.Where(o => o.OrderDate >= startOfYear && o.OrderDate < tomorrow).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m
            };

            // ===== Revenue chart (last 7 days, zero-filled) =====
            var chartRows = await revenueQuery
                .Where(o => o.OrderDate >= chartStart && o.OrderDate < tomorrow)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            for (var i = 0; i < 7; i++)
            {
                var date = chartStart.AddDays(i);
                model.RevenueChartLabels.Add(date.ToString("ddd"));
                model.RevenueChartData.Add(chartRows.FirstOrDefault(r => r.Date == date)?.Total ?? 0m);
            }

            // ===== Low stock products =====
            model.LowStockProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StockQuantity = p.StockQuantity,
                    LowStockThreshold = p.LowStockThreshold
                })
                .ToListAsync();

            foreach (var item in model.LowStockProducts)
            {
                var target = Math.Max(item.LowStockThreshold, 1);
                item.StockPercent = (int)Math.Round(Math.Clamp((double)item.StockQuantity / target, 0, 1) * 100);
            }

            // ===== Top selling products =====
            model.TopSellingProducts = await _context.OrderItems
                .AsNoTracking()
                .Where(i => i.Order != null && !ExcludedStatuses.Contains(i.Order.OrderStatus))
                .GroupBy(i => i.ProductName)
                .Select(g => new TopSellingProductDto
                {
                    ProductName = g.Key,
                    TotalSold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var maxSold = model.TopSellingProducts.Count > 0
                ? model.TopSellingProducts.Max(x => x.TotalSold)
                : 0;

            foreach (var item in model.TopSellingProducts)
            {
                item.ProgressPercent = maxSold > 0
                    ? (int)Math.Round((double)item.TotalSold / maxSold * 100)
                    : 0;
            }

            // ===== Recent orders =====
            model.RecentOrders = await _context.Orders
                .AsNoTracking()
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
