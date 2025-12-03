using HistoricalReporting.Core.Entities;

namespace HistoricalReporting.Core.Interfaces;

public interface IReportService
{
    Task<IEnumerable<Report>> GetAllReportsAsync();
    Task<Report?> GetReportByIdAsync(Guid id);
    Task<Report> CreateReportAsync(Report report);
    Task UpdateReportAsync(Report report);
    Task DeleteReportAsync(Guid id);
}
