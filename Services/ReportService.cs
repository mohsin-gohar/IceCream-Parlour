using System.Globalization;
using System.Text;
using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class ReportService : IReportService
    {
        private static readonly string[] ExcludedStatuses =
        {
            OrderStatus.Cancelled.ToString(),
            OrderStatus.Refunded.ToString()
        };

        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportViewModel> GetReportAsync(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveRange(reportType, startDate, endDate);

            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= start && o.OrderDate < end && !ExcludedStatuses.Contains(o.OrderStatus));

            var model = new ReportViewModel
            {
                ReportType = reportType,
                StartDate = start,
                EndDate = end.AddDays(-1),
                TotalOrders = await query.CountAsync(),
                TotalRevenue = await query.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
                TotalItemsSold = await _context.OrderItems
                    .AsNoTracking()
                    .Where(i => i.Order != null &&
                                i.Order.OrderDate >= start && i.Order.OrderDate < end &&
                                !ExcludedStatuses.Contains(i.Order.OrderStatus))
                    .SumAsync(i => (int?)i.Quantity) ?? 0,
                RecentOrders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Take(20)
                    .Select(o => new OrderReportDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus,
                        PaymentStatus = o.PaymentStatus
                    })
                    .ToListAsync()
            };

            model.AverageOrderValue = model.TotalOrders > 0
                ? Math.Round(model.TotalRevenue / model.TotalOrders, 2)
                : 0m;

            // ===== Chart data =====
            if (string.Equals(reportType, "monthly", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(reportType, "yearly", StringComparison.OrdinalIgnoreCase))
            {
                var rows = await query
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(o => o.TotalAmount) })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToListAsync();

                model.ChartLabels = rows
                    .Select(r => new DateTime(r.Year, r.Month, 1)
                        .ToString(reportType.Equals("yearly", StringComparison.OrdinalIgnoreCase) ? "yyyy" : "MMM yyyy", CultureInfo.InvariantCulture))
                    .ToList();
                model.ChartData = rows.Select(r => r.Total).ToList();
            }
            else
            {
                var rows = await query
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                model.ChartLabels = rows.Select(r => r.Date.ToString("dd MMM", CultureInfo.InvariantCulture)).ToList();
                model.ChartData = rows.Select(r => r.Total).ToList();
            }

            return model;
        }

        public async Task<byte[]> ExportCsvAsync(string reportType, DateTime? startDate, DateTime? endDate)
        {
            var report = await GetReportAsync(reportType, startDate, endDate);

            var sb = new StringBuilder();
            sb.AppendLine("Order Number,Customer,Order Date,Total Amount,Order Status,Payment Status");

            foreach (var order in report.RecentOrders)
            {
                sb.AppendLine(string.Join(",",
                    Csv(order.OrderNumber),
                    Csv(order.CustomerName),
                    order.OrderDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    Csv(order.OrderStatus),
                    Csv(order.PaymentStatus)));
            }

            sb.AppendLine();
            sb.AppendLine($"Report Type,{Csv(report.ReportType)}");
            sb.AppendLine($"Period,{report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Total Orders,{report.TotalOrders}");
            sb.AppendLine($"Total Revenue,{report.TotalRevenue.ToString("0.00", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Average Order Value,{report.AverageOrderValue.ToString("0.00", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Items Sold,{report.TotalItemsSold}");

            // UTF-8 BOM so Excel opens the file with the correct encoding.
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static (DateTime Start, DateTime EndExclusive) ResolveRange(
            string reportType, DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;

            DateTime start;
            DateTime endExclusive;

            switch (reportType?.ToLowerInvariant())
            {
                case "weekly":
                    start = startDate?.Date ?? today.AddDays(-6);
                    endExclusive = (endDate?.Date ?? today).AddDays(1);
                    break;
                case "monthly":
                    start = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
                    endExclusive = (endDate?.Date ?? today).AddDays(1);
                    break;
                case "yearly":
                    start = startDate?.Date ?? new DateTime(today.Year, 1, 1);
                    endExclusive = (endDate?.Date ?? today).AddDays(1);
                    break;
                default:
                    start = startDate?.Date ?? today;
                    endExclusive = (endDate?.Date ?? today).AddDays(1);
                    break;
            }

            if (endExclusive <= start)
            {
                start = endExclusive.AddDays(-1);
            }

            return (start, endExclusive);
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? $"\"{escaped}\""
                : escaped;
        }
    }
}
