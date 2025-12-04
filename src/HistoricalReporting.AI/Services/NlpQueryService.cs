using System.Data;
using System.Diagnostics;
using HistoricalReporting.AI.Agents;
using HistoricalReporting.AI.Configuration;
using HistoricalReporting.Core.Entities;
using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HistoricalReporting.AI.Services;

public class NlpQueryService : INlpQueryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqlGenerationAgent _sqlAgent;
    private readonly SqlQueryValidator _validator;
    private readonly NlpQuerySettings _settings;
    private readonly ILogger<NlpQueryService> _logger;

    public NlpQueryService(
        ApplicationDbContext dbContext,
        SqlGenerationAgent sqlAgent,
        SqlQueryValidator validator,
        IOptions<NlpQuerySettings> settings,
        ILogger<NlpQueryService> logger)
    {
        _dbContext = dbContext;
        _sqlAgent = sqlAgent;
        _validator = validator;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<NlpQueryResponse> ProcessQueryAsync(NlpQueryRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new NlpQueryResponse
        {
            NaturalLanguageQuery = request.Query
        };

        try
        {
            // Step 1: Generate SQL using the AI agent
            var context = new QueryContext
            {
                OrganisationId = request.OrganisationId,
                ManagerId = request.ManagerId,
                ManagesAllDepartments = request.ManagesAllDepartments,
                AccessibleDepartmentIds = request.AccessibleDepartmentIds
            };

            var generationResult = await _sqlAgent.GenerateSqlAsync(request.Query, context, cancellationToken);

            if (!generationResult.Success)
            {
                if (generationResult.ClarificationNeeded)
                {
                    response.ClarificationNeeded = true;
                    response.ClarificationMessage = generationResult.ClarificationMessage;
                    return response;
                }

                response.Error = generationResult.Error ?? "Failed to generate SQL query.";
                await LogQueryAsync(request, null, false, response.Error, 0, stopwatch.ElapsedMilliseconds);
                return response;
            }

            response.GeneratedSql = generationResult.GeneratedSql;
            response.Explanation = generationResult.Explanation;
            response.Warnings = generationResult.Warnings;

            // Step 2: Validate the generated SQL
            var validationResult = _validator.Validate(generationResult.GeneratedSql!);
            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Error = validationResult.ErrorMessage;
                response.Warnings.AddRange(validationResult.Warnings);
                await LogQueryAsync(request, generationResult.GeneratedSql, false, validationResult.ErrorMessage, 0, stopwatch.ElapsedMilliseconds);
                return response;
            }

            response.Warnings.AddRange(validationResult.Warnings);

            // Step 3: Sanitize and enforce row-level security
            var sanitizedSql = _validator.SanitizeAndEnforce(
                generationResult.GeneratedSql!,
                request.OrganisationId,
                request.AccessibleDepartmentIds,
                request.ManagesAllDepartments);

            response.GeneratedSql = sanitizedSql;

            // Step 4: Execute the query
            var (results, columns, totalRows, wasTruncated) = await ExecuteQueryAsync(
                sanitizedSql,
                generationResult.Parameters,
                request,
                cancellationToken);

            response.Success = true;
            response.Results = results;
            response.Columns = columns;
            response.TotalRows = totalRows;
            response.WasTruncated = wasTruncated;
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            // Log successful query
            await LogQueryAsync(request, sanitizedSql, true, null, totalRows, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NLP query: {Query}", request.Query);
            response.Success = false;
            response.Error = $"An error occurred while processing your query: {ex.Message}";
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            await LogQueryAsync(request, response.GeneratedSql, false, ex.Message, 0, stopwatch.ElapsedMilliseconds);

            return response;
        }
    }

    private async Task<(List<Dictionary<string, object?>> Results, List<string> Columns, int TotalRows, bool WasTruncated)>
        ExecuteQueryAsync(
            string sql,
            Dictionary<string, object> parameters,
            NlpQueryRequest request,
            CancellationToken cancellationToken)
    {
        var results = new List<Dictionary<string, object?>>();
        var columns = new List<string>();
        var totalRows = 0;
        var wasTruncated = false;

        // Build parameters with row-level security values
        var sqlParameters = new List<SqlParameter>
        {
            new SqlParameter("@OrganisationId", request.OrganisationId)
        };

        if (request.ManagerId.HasValue)
        {
            sqlParameters.Add(new SqlParameter("@ManagerId", request.ManagerId.Value));
        }

        // Add department IDs if not managing all
        if (!request.ManagesAllDepartments && request.AccessibleDepartmentIds?.Any() == true)
        {
            // For department filtering, we need to handle this specially
            // The AI should have generated a query with IN clause or similar
            for (int i = 0; i < request.AccessibleDepartmentIds.Count; i++)
            {
                sqlParameters.Add(new SqlParameter($"@DepartmentId{i}", request.AccessibleDepartmentIds[i]));
            }
        }

        // Add any additional parameters from the AI
        foreach (var param in parameters)
        {
            if (!sqlParameters.Any(p => p.ParameterName == $"@{param.Key}"))
            {
                sqlParameters.Add(new SqlParameter($"@{param.Key}", param.Value ?? DBNull.Value));
            }
        }

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = _settings.QueryTimeoutSeconds;

            foreach (var param in sqlParameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Get column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            // Read results
            while (await reader.ReadAsync(cancellationToken))
            {
                if (totalRows >= _settings.MaxResultRows)
                {
                    wasTruncated = true;
                    // Count remaining rows
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        totalRows++;
                    }
                    break;
                }

                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[columns[i]] = value == DBNull.Value ? null : value;
                }
                results.Add(row);
                totalRows++;
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        return (results, columns, totalRows, wasTruncated);
    }

    private async Task LogQueryAsync(
        NlpQueryRequest request,
        string? generatedSql,
        bool success,
        string? errorMessage,
        int resultCount,
        double executionTimeMs)
    {
        if (!_settings.EnableQueryHistory || !request.ManagerId.HasValue)
            return;

        try
        {
            var log = new NlpQueryLog
            {
                Id = Guid.NewGuid(),
                ManagerId = request.ManagerId.Value,
                OrganisationId = request.OrganisationId,
                NaturalLanguageQuery = request.Query,
                GeneratedSql = generatedSql,
                Success = success,
                ErrorMessage = errorMessage,
                ResultCount = resultCount,
                ExecutionTimeMs = executionTimeMs
            };

            _dbContext.NlpQueryLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log NLP query");
        }
    }

    public async Task<List<NlpQueryHistoryItem>> GetQueryHistoryAsync(
        Guid managerId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NlpQueryLogs
            .AsNoTracking()
            .Where(q => q.ManagerId == managerId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .Select(q => new NlpQueryHistoryItem
            {
                Id = q.Id,
                ManagerId = q.ManagerId,
                Query = q.NaturalLanguageQuery,
                GeneratedSql = q.GeneratedSql,
                Success = q.Success,
                ResultCount = q.ResultCount,
                ExecutedAt = q.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<string>> GetSuggestedQueriesAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<string>
        {
            "Show me the total number of valid and invalid audit records",
            "What is the compliance rate by department?",
            "Show monthly audit trends for the past year",
            "Which departments have the most invalid records?",
            "How many users are in each department?",
            "Show me the audit records for the IT department",
            "Compare this month's compliance to last month",
            "List the top 10 departments by audit volume",
            "What percentage of records are valid overall?",
            "Show daily audit counts for the last 30 days"
        };

        return Task.FromResult(suggestions);
    }
}
