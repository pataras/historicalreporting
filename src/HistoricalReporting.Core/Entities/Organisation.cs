namespace HistoricalReporting.Core.Entities;

public class Organisation : BaseEntity
{
    public required string Name { get; set; }

    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Manager> Managers { get; set; } = [];
    public ICollection<OrganisationUser> Users { get; set; } = [];
}
