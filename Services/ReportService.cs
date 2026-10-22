using System.Text;
using Ice_Cream_Parlour_Eproject.Areas.Models.Report;
using ReportVm = Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels.ReportViewModel;
using OrderReportVm = Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels.OrderReportDto;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportVm> GetReportAsync(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var (start, endExclusive) = ResolveRange(reportType, startDate, endDate);

            var query = _context.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= start && o.OrderDate < endExclusive);

            var model = new ReportVm
            {
                ReportType = string.IsNullOrWhiteSpace(reportType) ? "daily" : reportType,
                StartDate = start,
                EndDate = endExclusive.AddDays(-1),
                TotalOrders = await query.CountAsync(),
                TotalRevenue = await query.SumAsync(o => (decimal?)o.TotalAmount) ?? 0
            };

            model.AverageOrderValue = model.TotalOrders > 0
                ? model.TotalRevenue / model.TotalOrders
                : 0;

            model.TotalItemsSold = await _context.OrderItems.AsNoTracking()
                .Where(i => i.Order != null && i.Order.OrderDate >= start && i.Order.OrderDate < endExclusive)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            model.RecentOrders = await query
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new OrderReportVm
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus
                })
                .ToListAsync();

            var topProducts = await _context.OrderItems.AsNoTracking()
                .Where(i => i.Order != null && i.Order.OrderDate >= start && i.Order.OrderDate < endExclusive)
                .GroupBy(i => i.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalSold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            var maxSold = topProducts.Count > 0 ? topProducts.Max(x => x.TotalSold) : 0;
            model.TopProducts = topProducts.Select(x => new TopProductDto
            {
                ProductName = string.IsNullOrWhiteSpace(x.ProductName) ? "Unknown" : x.ProductName,
                TotalSold = x.TotalSold,
                Revenue = x.Revenue,
                ProgressPercent = maxSold > 0 ? (int)(x.TotalSold * 100.0 / maxSold) : 0
            }).ToList();

            // Chart - daily revenue inside the selected range (max 31 points).
            var chartRows = await query
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            var revenueByDay = chartRows
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

            var days = Math.Min(31, Math.Max(1, (int)(endExclusive.Date - start.Date).TotalDays));
            for (var i = 0; i < days; i++)
            {
                var date = start.Date.AddDays(i);
                model.ChartLabels.Add(date.ToString("dd MMM"));
                model.ChartData.Add(revenueByDay.TryGetValue(date, out var total) ? total : 0);
            }

            return model;
        }

        public async Task<byte[]> ExportPdfAsync(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var report = await GetReportAsync(reportType, startDate, endDate);

            var pdf = new SimplePdfDocument();
            pdf.WriteLine("iCREAM - Sales Report", 20, bold: true);
            pdf.WriteLine($"Report type: {report.ReportType}", 11);
            pdf.WriteLine($"Period: {report.StartDate:dd MMM yyyy} - {report.EndDate:dd MMM yyyy}", 11);
            pdf.WriteLine($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}", 9);
            pdf.AddSpace();
            pdf.AddHorizontalLine();

            pdf.WriteColumns(new[] { "Total orders", report.TotalOrders.ToString() }, new[] { 150f, 200f }, 11);
            pdf.WriteColumns(new[] { "Total revenue", $"${report.TotalRevenue:N2}" }, new[] { 150f, 200f }, 11);
            pdf.WriteColumns(new[] { "Average order", $"${report.AverageOrderValue:N2}" }, new[] { 150f, 200f }, 11);
            pdf.WriteColumns(new[] { "Items sold", report.TotalItemsSold.ToString() }, new[] { 150f, 200f }, 11);

            pdf.AddSpace(12);
            pdf.WriteLine("Top products", 13, bold: true);
            pdf.WriteColumns(new[] { "Product", "Qty", "Revenue" }, new[] { 300f, 60f, 120f }, 9, bold: true);
            pdf.AddHorizontalLine();

            if (report.TopProducts.Count == 0)
            {
                pdf.WriteLine("No product sales in this period.", 9);
            }
            else
            {
                foreach (var product in report.TopProducts)
                {
                    pdf.WriteColumns(new[]
                    {
                        product.ProductName,
                        product.TotalSold.ToString(),
                        $"${product.Revenue:N2}"
                    }, new[] { 300f, 60f, 120f }, 9);
                }
            }

            pdf.AddSpace(12);
            pdf.WriteLine("Recent orders", 13, bold: true);
            pdf.WriteColumns(new[] { "Order #", "Date", "Customer", "Status", "Total" },
                new[] { 110f, 70f, 150f, 70f, 80f }, 9, bold: true);
            pdf.AddHorizontalLine();

            if (report.RecentOrders.Count == 0)
            {
                pdf.WriteLine("No orders in this period.", 9);
            }
            else
            {
                foreach (var order in report.RecentOrders)
                {
                    pdf.WriteColumns(new[]
                    {
                        order.OrderNumber,
                        order.OrderDate.ToString("dd MMM yy"),
                        order.CustomerName,
                        order.OrderStatus,
                        $"${order.TotalAmount:N2}"
                    }, new[] { 110f, 70f, 150f, 70f, 80f }, 9);
                }
            }

            return pdf.Build();
        }

        public async Task<byte[]> ExportExcelAsync(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var report = await GetReportAsync(reportType, startDate, endDate);

            var csv = new StringBuilder();
            csv.AppendLine("iCREAM Sales Report");
            csv.AppendLine($"Report type,{Csv(report.ReportType)}");
            csv.AppendLine($"Period,{report.StartDate:yyyy-MM-dd},{report.EndDate:yyyy-MM-dd}");
            csv.AppendLine();
            csv.AppendLine("Summary");
            csv.AppendLine("Total Orders,Total Revenue,Average Order Value,Items Sold");
            csv.AppendLine($"{report.TotalOrders},{report.TotalRevenue:F2},{report.AverageOrderValue:F2},{report.TotalItemsSold}");
            csv.AppendLine();
            csv.AppendLine("Top Products");
            csv.AppendLine("Product,Quantity Sold,Revenue");
            foreach (var product in report.TopProducts)
            {
                csv.AppendLine($"{Csv(product.ProductName)},{product.TotalSold},{product.Revenue:F2}");
            }
            csv.AppendLine();
            csv.AppendLine("Orders");
            csv.AppendLine("Order Number,Date,Customer,Status,Payment Status,Total");
            foreach (var order in report.RecentOrders)
            {
                csv.AppendLine(string.Join(',',
                    Csv(order.OrderNumber),
                    order.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                    Csv(order.CustomerName),
                    Csv(order.OrderStatus),
                    Csv(order.PaymentStatus),
                    order.TotalAmount.ToString("F2")));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<byte[]> ExportOrderInvoicePdfAsync(int orderId)
        {
            var order = await _context.Orders.AsNoTracking()
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Array.Empty<byte>();
            }

            var pdf = new SimplePdfDocument();
            pdf.WriteLine("iCREAM", 24, bold: true);
            pdf.WriteLine("123 Ice Cream Street, New York, NY 10001", 9);
            pdf.WriteLine("Phone: +1 (012) 345 6789  |  info@icream.com", 9);
            pdf.AddSpace(6);
            pdf.WriteLine("INVOICE", 18, bold: true);
            pdf.AddHorizontalLine();

            pdf.WriteColumns(new[] { $"Invoice: {order.OrderNumber}", $"Date: {order.OrderDate:dd MMM yyyy}" },
                new[] { 260f, 240f }, 10);
            pdf.WriteColumns(new[] { $"Customer: {order.CustomerName}", $"Payment: {order.PaymentStatus}" },
                new[] { 260f, 240f }, 10);
            pdf.WriteLine($"Email: {order.CustomerEmail}", 10);
            pdf.WriteLine($"Phone: {order.CustomerPhone ?? "N/A"}", 10);
            pdf.WriteLine($"Address: {order.DeliveryAddress ?? "Store pickup"}", 10);
            pdf.WriteLine($"Payment method: {order.PaymentMethod}", 10);
            pdf.WriteLine($"Order status: {order.OrderStatus}", 10);

            pdf.AddSpace(10);
            pdf.WriteColumns(new[] { "Item", "Qty", "Unit price", "Line total" },
                new[] { 280f, 50f, 80f, 90f }, 9, bold: true);
            pdf.AddHorizontalLine();

            foreach (var item in order.OrderItems)
            {
                pdf.WriteColumns(new[]
                {
                    item.ProductName,
                    item.Quantity.ToString(),
                    $"${item.UnitPrice:N2}",
                    $"${item.LineTotal:N2}"
                }, new[] { 280f, 50f, 80f, 90f }, 9);
            }

            pdf.AddHorizontalLine();
            pdf.WriteColumns(new[] { string.Empty, string.Empty, "Subtotal", $"${order.TotalAmount:N2}" },
                new[] { 280f, 50f, 80f, 90f }, 10, bold: true);
            pdf.WriteColumns(new[] { string.Empty, string.Empty, "Total", $"${order.TotalAmount:N2}" },
                new[] { 280f, 50f, 80f, 90f }, 12, bold: true);

            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                pdf.AddSpace(10);
                pdf.WriteLine($"Notes: {order.Notes}", 9);
            }

            pdf.AddSpace(16);
            pdf.WriteLine("Thank you for shopping with iCREAM!", 10);

            return pdf.Build();
        }

        private static (DateTime Start, DateTime EndExclusive) ResolveRange(
            string? reportType, DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            var type = (reportType ?? "daily").Trim().ToLowerInvariant();

            DateTime start;
            DateTime endInclusive;

            if (startDate.HasValue || endDate.HasValue)
            {
                endInclusive = (endDate ?? today).Date;
                start = (startDate ?? endInclusive.AddDays(-30)).Date;
            }
            else
            {
                endInclusive = today;
                start = type switch
                {
                    "weekly" => today.AddDays(-6),
                    "monthly" => new DateTime(today.Year, today.Month, 1),
                    "yearly" => new DateTime(today.Year, 1, 1),
                    _ => today
                };
            }

            if (start > endInclusive)
            {
                (start, endInclusive) = (endInclusive, start);
            }

            return (start, endInclusive.AddDays(1));
        }

        private static string Csv(string? value)
        {
            var text = value ?? string.Empty;
            if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            return text;
        }
    }
}
