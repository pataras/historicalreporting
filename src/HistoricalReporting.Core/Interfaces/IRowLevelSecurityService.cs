namespace HistoricalReporting.Core.Interfaces;

/// <summary>
/// Service for enforcing row-level security based on manager's department access
/// </summary>
public interface IRowLevelSecurityService
{
    /// <summary>
    /// Gets the list of department IDs the current user can access
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAccessibleDepartmentIdsAsync();

    /// <summary>
    /// Checks if the current user can access data for the specified organisation
    /// </summary>
    Task<bool> CanAccessOrganisationAsync(Guid organisationId);

    /// <summary>
    /// Checks if the current user manages all departments in their organisation
    /// </summary>
    Task<bool> ManagesAllDepartmentsAsync();

    /// <summary>
    /// Gets the current user's organisation ID
    /// </summary>
    Guid? GetCurrentOrganisationId();

    /// <summary>
    /// Gets the current user's manager ID
    /// </summary>
    Guid? GetCurrentManagerId();
}
