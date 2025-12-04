namespace HistoricalReporting.Core.Entities;

public class User : BaseEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Link to Manager role (if this user is a manager)
    public Guid? ManagerId { get; set; }
    public Manager? Manager { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
