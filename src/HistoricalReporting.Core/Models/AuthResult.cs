namespace HistoricalReporting.Core.Models;

/// <summary>
/// Result of a successful authentication
/// </summary>
public class AuthResult
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required UserInfo User { get; set; }
}

/// <summary>
/// Information about the authenticated user
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? OrganisationId { get; set; }
    public string? OrganisationName { get; set; }
    public bool ManagesAllDepartments { get; set; }
    public List<ManagedDepartmentInfo> ManagedDepartments { get; set; } = [];
}

/// <summary>
/// Information about a department the user manages
/// </summary>
public class ManagedDepartmentInfo
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}
