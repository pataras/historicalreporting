namespace HistoricalReporting.Core.Entities;

public class Department : BaseEntity
{
    public required string Name { get; set; }

    public Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }

    public ICollection<Department> SubDepartments { get; set; } = [];
    public ICollection<OrganisationUser> Users { get; set; } = [];
    public ICollection<ManagerDepartment> ManagerDepartments { get; set; } = [];
}
