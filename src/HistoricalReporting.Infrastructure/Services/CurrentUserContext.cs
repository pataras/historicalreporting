using System.Security.Claims;
using HistoricalReporting.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace HistoricalReporting.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user's context from JWT claims
/// </summary>
public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public Guid? ManagerId
    {
        get
        {
            var managerIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("manager_id")?.Value;
            return managerIdClaim != null && Guid.TryParse(managerIdClaim, out var managerId) ? managerId : null;
        }
    }

    public Guid? OrganisationId
    {
        get
        {
            var orgIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("organisation_id")?.Value;
            return orgIdClaim != null && Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsManager => ManagerId.HasValue;
}
