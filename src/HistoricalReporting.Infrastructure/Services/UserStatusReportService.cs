using HistoricalReporting.Core.Entities;
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

    public async Task<MonthlyUserStatusReportResult?> GetMonthlyUserStatusReportByManagerAsync(Guid organisationId, Guid managerId)
    {
        var organisation = await _context.Organisations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organisationId);

        if (organisation == null)
        {
            return null;
        }

        var manager = await _context.Managers
            .AsNoTracking()
            .Where(m => m.Id == managerId && m.OrganisationId == organisationId)
            .Include(m => m.ManagedDepartments)
                .ThenInclude(md => md.Department)
            .FirstOrDefaultAsync();

        if (manager == null)
        {
            return null;
        }

        // Get all department IDs managed by this manager (including sub-departments if needed)
        IQueryable<AuditRecord> auditRecordsQuery;

        if (manager.ManagesAllDepartments)
        {
            // Manager manages all departments in the organisation
            auditRecordsQuery = _context.AuditRecords
                .AsNoTracking()
                .Join(
                    _context.OrganisationUsers.Where(u => u.OrganisationId == organisationId),
                    ar => ar.UserId,
                    ou => ou.Id,
                    (ar, ou) => ar);
        }
        else
        {
            // Manager manages specific departments
            var managedDepartmentIds = manager.ManagedDepartments.Select(md => md.DepartmentId).ToList();

            if (managedDepartmentIds.Count == 0)
            {
                return new MonthlyUserStatusReportResult
                {
                    OrganisationId = organisationId,
                    OrganisationName = organisation.Name,
                    ManagerId = managerId,
                    ManagerName = GenerateManagerName(manager),
                    MonthlyData = []
                };
            }

            auditRecordsQuery = _context.AuditRecords
                .AsNoTracking()
                .Join(
                    _context.OrganisationUsers.Where(u =>
                        u.OrganisationId == organisationId &&
                        managedDepartmentIds.Contains(u.DepartmentId)),
                    ar => ar.UserId,
                    ou => ou.Id,
                    (ar, ou) => ar);
        }

        var monthlyData = await auditRecordsQuery
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
            ManagerId = managerId,
            ManagerName = GenerateManagerName(manager),
            MonthlyData = monthlyData.Select(m => new MonthlyUserStatusReport
            {
                Year = m.Year,
                Month = m.Month,
                ValidCount = m.ValidCount,
                InvalidCount = m.InvalidCount
            }).ToList()
        };
    }

    private static string GenerateManagerName(Manager manager)
    {
        if (manager.ManagesAllDepartments)
        {
            return "All Departments";
        }

        var departmentNames = manager.ManagedDepartments
            .Where(md => md.Department != null)
            .Select(md => md.Department!.Name)
            .OrderBy(n => n)
            .Take(3)
            .ToList();

        if (departmentNames.Count == 0)
        {
            return "No Departments";
        }

        var suffix = manager.ManagedDepartments.Count > 3
            ? $" +{manager.ManagedDepartments.Count - 3} more"
            : "";

        return string.Join(", ", departmentNames) + suffix;
    }
}
