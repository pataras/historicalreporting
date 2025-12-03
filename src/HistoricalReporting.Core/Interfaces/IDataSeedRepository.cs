using HistoricalReporting.Core.Entities;

namespace HistoricalReporting.Core.Interfaces;

public interface IDataSeedRepository
{
    Task AddOrganisationsAsync(IEnumerable<Organisation> organisations, CancellationToken cancellationToken = default);
    Task AddDepartmentsAsync(IEnumerable<Department> departments, CancellationToken cancellationToken = default);
    Task UpdateDepartmentHierarchyAsync(IEnumerable<(Guid DeptId, Guid ParentId)> updates, CancellationToken cancellationToken = default);
    Task AddManagersAsync(IEnumerable<Manager> managers, CancellationToken cancellationToken = default);
    Task AddManagerDepartmentsAsync(IEnumerable<ManagerDepartment> managerDepartments, CancellationToken cancellationToken = default);
    Task AddUsersAsync(IEnumerable<OrganisationUser> users, CancellationToken cancellationToken = default);
    Task AddAuditRecordsAsync(IEnumerable<AuditRecord> records, CancellationToken cancellationToken = default);
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);
}
