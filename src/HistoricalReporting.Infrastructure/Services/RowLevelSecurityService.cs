using System.Security.Claims;
using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HistoricalReporting.Infrastructure.Services;

/// <summary>
/// Service for enforcing row-level security based on manager's department access
/// </summary>
public class RowLevelSecurityService : IRowLevelSecurityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserContext _currentUserContext;

    public RowLevelSecurityService(
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext context,
        ICurrentUserContext currentUserContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleDepartmentIdsAsync()
    {
        var managerId = _currentUserContext.ManagerId;
        if (!managerId.HasValue)
        {
            return [];
        }

        // First check the claims for managed departments (faster, from JWT)
        var managedDepartmentClaims = _httpContextAccessor.HttpContext?.User
            .FindAll("managed_department")
            .Select(c => c.Value)
            .ToList();

        if (managedDepartmentClaims != null && managedDepartmentClaims.Count > 0)
        {
            return managedDepartmentClaims
                .Select(c => Guid.TryParse(c, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();
        }

        // Fallback to database lookup
        var manager = await _context.Managers
            .AsNoTracking()
            .Include(m => m.ManagedDepartments)
            .FirstOrDefaultAsync(m => m.Id == managerId.Value);

        if (manager == null)
        {
            return [];
        }

        return manager.ManagedDepartments.Select(md => md.DepartmentId).ToList();
    }

    public async Task<bool> CanAccessOrganisationAsync(Guid organisationId)
    {
        var currentOrgId = _currentUserContext.OrganisationId;
        if (!currentOrgId.HasValue)
        {
            return false;
        }

        return currentOrgId.Value == organisationId;
    }

    public async Task<bool> ManagesAllDepartmentsAsync()
    {
        // First check the claim (faster, from JWT)
        var managesAllClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("manages_all_departments")?.Value;

        if (managesAllClaim != null)
        {
            return bool.TryParse(managesAllClaim, out var result) && result;
        }

        // Fallback to database lookup
        var managerId = _currentUserContext.ManagerId;
        if (!managerId.HasValue)
        {
            return false;
        }

        var manager = await _context.Managers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == managerId.Value);

        return manager?.ManagesAllDepartments ?? false;
    }

    public Guid? GetCurrentOrganisationId()
    {
        return _currentUserContext.OrganisationId;
    }

    public Guid? GetCurrentManagerId()
    {
        return _currentUserContext.ManagerId;
    }
}
