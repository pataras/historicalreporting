using HistoricalReporting.Core.Entities;
using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HistoricalReporting.Infrastructure.Repositories;

public class DataSeedRepository : IDataSeedRepository
{
    private readonly ApplicationDbContext _context;

    public DataSeedRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddOrganisationsAsync(IEnumerable<Organisation> organisations, CancellationToken cancellationToken = default)
    {
        _context.Organisations.AddRange(organisations);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDepartmentsAsync(IEnumerable<Department> departments, CancellationToken cancellationToken = default)
    {
        _context.Departments.AddRange(departments);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDepartmentHierarchyAsync(IEnumerable<(Guid DeptId, Guid ParentId)> updates, CancellationToken cancellationToken = default)
    {
        foreach (var (deptId, parentId) in updates)
        {
            var dept = await _context.Departments.FindAsync([deptId], cancellationToken);
            if (dept != null)
            {
                dept.ParentDepartmentId = parentId;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddManagersAsync(IEnumerable<Manager> managers, CancellationToken cancellationToken = default)
    {
        _context.Managers.AddRange(managers);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddManagerDepartmentsAsync(IEnumerable<ManagerDepartment> managerDepartments, CancellationToken cancellationToken = default)
    {
        _context.ManagerDepartments.AddRange(managerDepartments);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddUsersAsync(IEnumerable<OrganisationUser> users, CancellationToken cancellationToken = default)
    {
        _context.OrganisationUsers.AddRange(users);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAuditRecordsAsync(IEnumerable<AuditRecord> records, CancellationToken cancellationToken = default)
    {
        _context.AuditRecords.AddRange(records);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        // Clear in correct order to respect foreign keys
        await _context.AuditRecords.ExecuteDeleteAsync(cancellationToken);
        await _context.OrganisationUsers.ExecuteDeleteAsync(cancellationToken);
        await _context.ManagerDepartments.ExecuteDeleteAsync(cancellationToken);
        await _context.Managers.ExecuteDeleteAsync(cancellationToken);

        // Clear parent references first
        await _context.Departments
            .Where(d => d.ParentDepartmentId != null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.ParentDepartmentId, (Guid?)null), cancellationToken);

        await _context.Departments.ExecuteDeleteAsync(cancellationToken);
        await _context.Organisations.ExecuteDeleteAsync(cancellationToken);
    }
}
