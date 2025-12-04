namespace HistoricalReporting.Core.Interfaces;

/// <summary>
/// Provides access to the current authenticated user's context
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// The authenticated user's ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The manager ID associated with the authenticated user (if they are a manager)
    /// </summary>
    Guid? ManagerId { get; }

    /// <summary>
    /// The organisation ID the manager belongs to
    /// </summary>
    Guid? OrganisationId { get; }

    /// <summary>
    /// Whether the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Whether the authenticated user is a manager
    /// </summary>
    bool IsManager { get; }
}
