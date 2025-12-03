namespace HistoricalReporting.Core.Entities;

public class AuditRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public OrganisationUser? User { get; set; }

    /// <summary>
    /// Date represented as integer in yyyyMMdd format (e.g., 20240115)
    /// </summary>
    public int Date { get; set; }

    /// <summary>
    /// Status of the audit record: "Valid" or "Invalid"
    /// </summary>
    public required string Status { get; set; }
}
