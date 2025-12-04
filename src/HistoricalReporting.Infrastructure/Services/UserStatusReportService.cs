using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HistoricalReporting.Infrastructure.Services;

public class UserStatusReportService : IUserStatusReportService
{
    private readonly ApplicationDbContext _context;

    public UserStatusReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlyUserStatusReportResult?> GetMonthlyUserStatusReportAsync(Guid organisationId)
    {
        var organisation = await _context.Organisations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organisationId);

        if (organisation == null)
        {
            return null;
        }

        // Query audit records for users in the specified organisation
        // Date is stored as int in yyyyMMdd format (e.g., 20240115)
        var monthlyData = await _context.AuditRecords
            .AsNoTracking()
            .Join(
                _context.OrganisationUsers.Where(u => u.OrganisationId == organisationId),
                ar => ar.UserId,
                ou => ou.Id,
                (ar, ou) => ar)
            .GroupBy(ar => new
            {
                Year = ar.Date / 10000,
                Month = (ar.Date / 100) % 100
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                ValidCount = g.Count(ar => ar.Status == "Valid"),
                InvalidCount = g.Count(ar => ar.Status == "Invalid")
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        return new MonthlyUserStatusReportResult
        {
            OrganisationId = organisationId,
            OrganisationName = organisation.Name,
            MonthlyData = monthlyData.Select(m => new MonthlyUserStatusReport
            {
                Year = m.Year,
                Month = m.Month,
                ValidCount = m.ValidCount,
                InvalidCount = m.InvalidCount
            }).ToList()
        };
    }
}
