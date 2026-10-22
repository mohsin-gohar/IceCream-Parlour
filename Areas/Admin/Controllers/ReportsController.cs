using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(string reportType = "daily", DateTime? startDate = null, DateTime? endDate = null)
    {
        ViewData["Title"] = "Reports";
        return View(await _reportService.GetReportAsync(reportType, startDate, endDate));
    }

    public async Task<IActionResult> ExportPdf(string reportType = "daily", DateTime? startDate = null, DateTime? endDate = null)
    {
        var pdf = await _reportService.ExportPdfAsync(reportType, startDate, endDate);
        return File(pdf, "application/pdf",
            FileStorage.BuildExportFileName($"Report-{reportType}", ".pdf"));
    }

    public async Task<IActionResult> ExportExcel(string reportType = "daily", DateTime? startDate = null, DateTime? endDate = null)
    {
        var csv = await _reportService.ExportExcelAsync(reportType, startDate, endDate);
        return File(csv, "text/csv",
            FileStorage.BuildExportFileName($"Report-{reportType}", ".csv"));
    }
}
