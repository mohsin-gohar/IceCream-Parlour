using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public interface IReportService
    {
        Task<ReportViewModel> GetReportAsync(string reportType, DateTime? startDate, DateTime? endDate);

        /// <summary>Exports the selected report as CSV (opens directly in Excel).</summary>
        Task<byte[]> ExportCsvAsync(string reportType, DateTime? startDate, DateTime? endDate);
    }
}
