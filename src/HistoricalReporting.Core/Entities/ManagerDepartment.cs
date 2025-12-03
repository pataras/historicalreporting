namespace HistoricalReporting.Core.Entities;

public class ManagerDepartment
{
    public Guid ManagerId { get; set; }
    public Manager? Manager { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
}
