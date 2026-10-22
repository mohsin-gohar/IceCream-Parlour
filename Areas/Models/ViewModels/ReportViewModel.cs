using System.Collections.Generic;
using Ice_Cream_Parlour_Eproject.Areas.Models.Report;

namespace Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels
{
    public class ReportViewModel
    {
        public string ReportType { get; set; } = "daily";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartData { get; set; } = new();

        public List<OrderReportDto> RecentOrders { get; set; } = new();

        public List<TopProductDto> TopProducts { get; set; } = new();
    }

    public class OrderReportDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
}