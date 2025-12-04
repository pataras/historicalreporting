namespace HistoricalReporting.Core.Models;

public class ManagerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool ManagesAllDepartments { get; set; }
    public List<DepartmentDto> ManagedDepartments { get; set; } = [];
}

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
