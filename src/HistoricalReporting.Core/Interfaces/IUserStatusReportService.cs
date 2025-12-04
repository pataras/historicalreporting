using HistoricalReporting.Core.Models;

namespace HistoricalReporting.Core.Interfaces;

public interface IUserStatusReportService
{
    Task<MonthlyUserStatusReportResult?> GetMonthlyUserStatusReportAsync(Guid organisationId);
}
