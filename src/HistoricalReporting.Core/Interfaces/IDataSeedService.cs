using HistoricalReporting.Core.Models;

namespace HistoricalReporting.Core.Interfaces;

public interface IDataSeedService
{
    Task SeedDataAsync(Func<SeedProgress, Task> progressCallback, CancellationToken cancellationToken = default);
    Task ClearDataAsync(Func<SeedProgress, Task> progressCallback, CancellationToken cancellationToken = default);
}
