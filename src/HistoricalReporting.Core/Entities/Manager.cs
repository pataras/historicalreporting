namespace HistoricalReporting.Core.Entities;

public class Manager : BaseEntity
{
    public Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public bool ManagesAllDepartments { get; set; }

    public ICollection<ManagerDepartment> ManagedDepartments { get; set; } = [];
}
