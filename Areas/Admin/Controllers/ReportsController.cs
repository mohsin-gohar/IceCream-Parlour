using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public class ReportsController : Controller
    {
        private static readonly string[] AllowedReportTypes = { "daily", "weekly", "monthly", "yearly" };

        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index(string reportType = "daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewData["Title"] = "Reports";
            return View(await _reportService.GetReportAsync(Normalize(reportType), startDate, endDate));
        }

        public async Task<IActionResult> ExportCsv(string reportType = "daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            var type = Normalize(reportType);
            var csv = await _reportService.ExportCsvAsync(type, startDate, endDate);
            return File(csv, "text/csv", $"iCREAM-report-{type}-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        private static string Normalize(string? reportType)
        {
            var value = reportType?.Trim().ToLowerInvariant();
            return value != null && AllowedReportTypes.Contains(value) ? value : "daily";
        }
    }
}
