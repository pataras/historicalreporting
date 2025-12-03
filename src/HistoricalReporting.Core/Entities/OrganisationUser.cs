namespace HistoricalReporting.Core.Entities;

public class OrganisationUser : BaseEntity
{
    public Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<AuditRecord> AuditRecords { get; set; } = [];
}
