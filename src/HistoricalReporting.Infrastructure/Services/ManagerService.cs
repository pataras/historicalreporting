using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HistoricalReporting.Infrastructure.Services;

public class ManagerService : IManagerService
{
    private readonly ApplicationDbContext _context;

    public ManagerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ManagerDto>> GetManagersByOrganisationAsync(Guid organisationId)
    {
        var managers = await _context.Managers
            .AsNoTracking()
            .Where(m => m.OrganisationId == organisationId)
            .Include(m => m.ManagedDepartments)
                .ThenInclude(md => md.Department)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return managers.Select((m, index) => new ManagerDto
        {
            Id = m.Id,
            Name = GenerateManagerName(m.ManagesAllDepartments, m.ManagedDepartments, index + 1),
            ManagesAllDepartments = m.ManagesAllDepartments,
            ManagedDepartments = m.ManagedDepartments
                .Where(md => md.Department != null)
                .Select(md => new DepartmentDto
                {
                    Id = md.DepartmentId,
                    Name = md.Department!.Name
                })
                .OrderBy(d => d.Name)
                .ToList()
        }).ToList();
    }

    public async Task<ManagerDto?> GetManagerByIdAsync(Guid managerId)
    {
        var manager = await _context.Managers
            .AsNoTracking()
            .Where(m => m.Id == managerId)
            .Include(m => m.ManagedDepartments)
                .ThenInclude(md => md.Department)
            .FirstOrDefaultAsync();

        if (manager == null)
        {
            return null;
        }

        return new ManagerDto
        {
            Id = manager.Id,
            Name = GenerateManagerName(manager.ManagesAllDepartments, manager.ManagedDepartments, 1),
            ManagesAllDepartments = manager.ManagesAllDepartments,
            ManagedDepartments = manager.ManagedDepartments
                .Where(md => md.Department != null)
                .Select(md => new DepartmentDto
                {
                    Id = md.DepartmentId,
                    Name = md.Department!.Name
                })
                .OrderBy(d => d.Name)
                .ToList()
        };
    }

    private static string GenerateManagerName(
        bool managesAllDepartments,
        ICollection<Core.Entities.ManagerDepartment> managedDepartments,
        int index)
    {
        if (managesAllDepartments)
        {
            return $"Manager {index} (All Departments)";
        }

        var departmentNames = managedDepartments
            .Where(md => md.Department != null)
            .Select(md => md.Department!.Name)
            .OrderBy(n => n)
            .Take(3)
            .ToList();

        if (departmentNames.Count == 0)
        {
            return $"Manager {index} (No Departments)";
        }

        var suffix = managedDepartments.Count > 3
            ? $" +{managedDepartments.Count - 3} more"
            : "";

        return $"Manager {index} ({string.Join(", ", departmentNames)}{suffix})";
    }
}
