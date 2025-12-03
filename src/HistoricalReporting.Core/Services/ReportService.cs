using HistoricalReporting.Core.Entities;
using HistoricalReporting.Core.Interfaces;

namespace HistoricalReporting.Core.Services;

public class ReportService : IReportService
{
    private readonly IRepository<Report> _reportRepository;

    public ReportService(IRepository<Report> reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<Report>> GetAllReportsAsync()
    {
        return await _reportRepository.GetAllAsync();
    }

    public async Task<Report?> GetReportByIdAsync(Guid id)
    {
        return await _reportRepository.GetByIdAsync(id);
    }

    public async Task<Report> CreateReportAsync(Report report)
    {
        report.CreatedAt = DateTime.UtcNow;
        return await _reportRepository.AddAsync(report);
    }

    public async Task UpdateReportAsync(Report report)
    {
        report.UpdatedAt = DateTime.UtcNow;
        await _reportRepository.UpdateAsync(report);
    }

    public async Task DeleteReportAsync(Guid id)
    {
        var report = await _reportRepository.GetByIdAsync(id);
        if (report != null)
        {
            await _reportRepository.DeleteAsync(report);
        }
    }
}
