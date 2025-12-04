using HistoricalReporting.Core.Models;

namespace HistoricalReporting.Core.Interfaces;

public interface IManagerService
{
    Task<List<ManagerDto>> GetManagersByOrganisationAsync(Guid organisationId);
    Task<ManagerDto?> GetManagerByIdAsync(Guid managerId);
}
