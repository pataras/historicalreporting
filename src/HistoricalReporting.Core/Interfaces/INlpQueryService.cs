using HistoricalReporting.Core.Models;

namespace HistoricalReporting.Core.Interfaces;

public interface INlpQueryService
{
    /// <summary>
    /// Processes a natural language query and returns the results.
    /// </summary>
    Task<NlpQueryResponse> ProcessQueryAsync(NlpQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the query history for a manager.
    /// </summary>
    Task<List<NlpQueryHistoryItem>> GetQueryHistoryAsync(Guid managerId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested queries for a manager.
    /// </summary>
    Task<List<string>> GetSuggestedQueriesAsync(Guid organisationId, CancellationToken cancellationToken = default);
}
