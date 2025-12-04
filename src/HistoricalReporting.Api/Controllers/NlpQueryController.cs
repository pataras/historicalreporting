using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NlpQueryController : ControllerBase
{
    private readonly INlpQueryService _nlpQueryService;
    private readonly IRowLevelSecurityService _rlsService;
    private readonly ILogger<NlpQueryController> _logger;

    public NlpQueryController(
        INlpQueryService nlpQueryService,
        IRowLevelSecurityService rlsService,
        ILogger<NlpQueryController> logger)
    {
        _nlpQueryService = nlpQueryService;
        _rlsService = rlsService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a natural language query and returns the results.
    /// Row-level security is automatically applied based on the manager's department access.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ProcessQuery([FromBody] NlpQueryRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query cannot be empty");
        }

        var organisationId = _rlsService.GetCurrentOrganisationId();
        var managerId = _rlsService.GetCurrentManagerId();

        if (!organisationId.HasValue || !managerId.HasValue)
        {
            return Forbid("User is not associated with a manager role");
        }

        _logger.LogInformation(
            "Processing NLP query for manager {ManagerId}: {Query}",
            managerId.Value, request.Query);

        var managesAllDepartments = await _rlsService.ManagesAllDepartmentsAsync();
        var accessibleDepartments = await _rlsService.GetAccessibleDepartmentIdsAsync();

        var nlpRequest = new NlpQueryRequest
        {
            Query = request.Query,
            OrganisationId = organisationId.Value,
            ManagerId = managerId.Value,
            ManagesAllDepartments = managesAllDepartments,
            AccessibleDepartmentIds = accessibleDepartments.ToList()
        };

        var response = await _nlpQueryService.ProcessQueryAsync(nlpRequest, cancellationToken);

        if (!response.Success && !response.ClarificationNeeded)
        {
            _logger.LogWarning(
                "NLP query failed for manager {ManagerId}: {Error}",
                managerId.Value, response.Error);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets the query history for the authenticated manager.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetQueryHistory([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var managerId = _rlsService.GetCurrentManagerId();

        if (!managerId.HasValue)
        {
            return Forbid("User is not associated with a manager role");
        }

        var history = await _nlpQueryService.GetQueryHistoryAsync(managerId.Value, limit, cancellationToken);

        return Ok(history);
    }

    /// <summary>
    /// Gets suggested queries for the authenticated manager.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(CancellationToken cancellationToken)
    {
        var organisationId = _rlsService.GetCurrentOrganisationId();

        if (!organisationId.HasValue)
        {
            return Forbid("User is not associated with an organisation");
        }

        var suggestions = await _nlpQueryService.GetSuggestedQueriesAsync(organisationId.Value, cancellationToken);

        return Ok(suggestions);
    }

    /// <summary>
    /// Exports query results to CSV format.
    /// </summary>
    [HttpPost("export/csv")]
    public async Task<IActionResult> ExportToCsv([FromBody] NlpQueryRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query cannot be empty");
        }

        var organisationId = _rlsService.GetCurrentOrganisationId();
        var managerId = _rlsService.GetCurrentManagerId();

        if (!organisationId.HasValue || !managerId.HasValue)
        {
            return Forbid("User is not associated with a manager role");
        }

        var managesAllDepartments = await _rlsService.ManagesAllDepartmentsAsync();
        var accessibleDepartments = await _rlsService.GetAccessibleDepartmentIdsAsync();

        var nlpRequest = new NlpQueryRequest
        {
            Query = request.Query,
            OrganisationId = organisationId.Value,
            ManagerId = managerId.Value,
            ManagesAllDepartments = managesAllDepartments,
            AccessibleDepartmentIds = accessibleDepartments.ToList()
        };

        var response = await _nlpQueryService.ProcessQueryAsync(nlpRequest, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response.Error ?? "Failed to execute query");
        }

        var csv = GenerateCsv(response.Columns, response.Results);

        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"query-results-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static string GenerateCsv(List<string> columns, List<Dictionary<string, object?>> results)
    {
        var sb = new System.Text.StringBuilder();

        // Header row
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsvValue(c))));

        // Data rows
        foreach (var row in results)
        {
            var values = columns.Select(c =>
            {
                row.TryGetValue(c, out var value);
                return EscapeCsvValue(value?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

public class NlpQueryRequestDto
{
    public string Query { get; set; } = string.Empty;
}
