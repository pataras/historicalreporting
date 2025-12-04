using System.ComponentModel;
using System.Text.Json;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace HistoricalReporting.McpServer.Tools;

[McpServerToolType]
public class QueryHelperTool
{
    private readonly ApplicationDbContext _dbContext;

    public QueryHelperTool(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [McpServerTool("get_status_values")]
    [Description("Returns all possible status values used in AuditRecords table. Use this to ensure correct status values in WHERE clauses.")]
    public async Task<string> GetStatusValuesAsync()
    {
        var statuses = await _dbContext.AuditRecords
            .AsNoTracking()
            .Select(a => a.Status)
            .Distinct()
            .ToListAsync();

        var result = new StatusValuesResult
        {
            StatusValues = statuses,
            Notes = new List<string>
            {
                "Use these exact values in WHERE clauses (case-sensitive).",
                "Status values are: 'Valid' (compliant/passed) and 'Invalid' (non-compliant/failed).",
                "Example: WHERE Status = 'Valid' or WHERE Status = 'Invalid'"
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool("get_date_range")]
    [Description("Returns the minimum and maximum dates in the AuditRecords table. Use this to understand the available date range for queries.")]
    public async Task<string> GetDateRangeAsync()
    {
        var minDate = await _dbContext.AuditRecords.MinAsync(a => a.Date);
        var maxDate = await _dbContext.AuditRecords.MaxAsync(a => a.Date);

        var result = new DateRangeResult
        {
            MinDate = minDate,
            MaxDate = maxDate,
            MinDateFormatted = FormatDate(minDate),
            MaxDateFormatted = FormatDate(maxDate),
            Notes = new List<string>
            {
                "Dates are stored as integers in yyyyMMdd format (e.g., 20240115 = January 15, 2024).",
                "To filter by year: WHERE Date / 10000 = 2024",
                "To filter by month: WHERE (Date / 100) % 100 = 1 (for January)",
                "To filter by date range: WHERE Date >= 20240101 AND Date <= 20241231",
                "To extract year: Date / 10000",
                "To extract month: (Date / 100) % 100",
                "To extract day: Date % 100"
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool("get_query_examples")]
    [Description("Returns example SQL queries for common reporting scenarios. Use these as templates when generating queries.")]
    public string GetQueryExamples()
    {
        var examples = new QueryExamplesResult
        {
            Examples = new List<QueryExample>
            {
                new QueryExample
                {
                    Description = "Count audit records by status for a specific organisation",
                    NaturalLanguage = "How many valid and invalid records are there for organisation X?",
                    SqlTemplate = @"SELECT
    ar.Status,
    COUNT(*) AS RecordCount
FROM AuditRecords ar
INNER JOIN OrganisationUsers ou ON ar.UserId = ou.Id
WHERE ou.OrganisationId = @OrganisationId
GROUP BY ar.Status"
                },
                new QueryExample
                {
                    Description = "Monthly audit summary for a department",
                    NaturalLanguage = "Show me monthly valid/invalid counts for the IT department",
                    SqlTemplate = @"SELECT
    ar.Date / 10000 AS Year,
    (ar.Date / 100) % 100 AS Month,
    ar.Status,
    COUNT(*) AS RecordCount
FROM AuditRecords ar
INNER JOIN OrganisationUsers ou ON ar.UserId = ou.Id
WHERE ou.DepartmentId = @DepartmentId
GROUP BY ar.Date / 10000, (ar.Date / 100) % 100, ar.Status
ORDER BY Year, Month"
                },
                new QueryExample
                {
                    Description = "User count by department",
                    NaturalLanguage = "How many users are in each department?",
                    SqlTemplate = @"SELECT
    d.Name AS DepartmentName,
    COUNT(ou.Id) AS UserCount
FROM Departments d
LEFT JOIN OrganisationUsers ou ON d.Id = ou.DepartmentId
WHERE d.OrganisationId = @OrganisationId
GROUP BY d.Id, d.Name
ORDER BY d.Name"
                },
                new QueryExample
                {
                    Description = "Compliance rate by department",
                    NaturalLanguage = "What is the compliance rate for each department?",
                    SqlTemplate = @"SELECT
    d.Name AS DepartmentName,
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN ar.Status = 'Valid' THEN 1 ELSE 0 END) AS ValidCount,
    SUM(CASE WHEN ar.Status = 'Invalid' THEN 1 ELSE 0 END) AS InvalidCount,
    CAST(SUM(CASE WHEN ar.Status = 'Valid' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100 AS ComplianceRate
FROM AuditRecords ar
INNER JOIN OrganisationUsers ou ON ar.UserId = ou.Id
INNER JOIN Departments d ON ou.DepartmentId = d.Id
WHERE ou.OrganisationId = @OrganisationId
GROUP BY d.Id, d.Name
ORDER BY ComplianceRate DESC"
                },
                new QueryExample
                {
                    Description = "Year-over-year comparison",
                    NaturalLanguage = "Compare this year's records to last year",
                    SqlTemplate = @"SELECT
    ar.Date / 10000 AS Year,
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN ar.Status = 'Valid' THEN 1 ELSE 0 END) AS ValidCount,
    SUM(CASE WHEN ar.Status = 'Invalid' THEN 1 ELSE 0 END) AS InvalidCount
FROM AuditRecords ar
INNER JOIN OrganisationUsers ou ON ar.UserId = ou.Id
WHERE ou.OrganisationId = @OrganisationId
  AND ar.Date / 10000 IN (@CurrentYear, @PreviousYear)
GROUP BY ar.Date / 10000
ORDER BY Year"
                },
                new QueryExample
                {
                    Description = "Top departments by audit volume",
                    NaturalLanguage = "Which departments have the most audit records?",
                    SqlTemplate = @"SELECT TOP 10
    d.Name AS DepartmentName,
    COUNT(*) AS RecordCount
FROM AuditRecords ar
INNER JOIN OrganisationUsers ou ON ar.UserId = ou.Id
INNER JOIN Departments d ON ou.DepartmentId = d.Id
WHERE ou.OrganisationId = @OrganisationId
GROUP BY d.Id, d.Name
ORDER BY RecordCount DESC"
                }
            },
            Notes = new List<string>
            {
                "Always include appropriate JOINs to connect AuditRecords to OrganisationUsers to Departments/Organisations.",
                "Use parameterized queries with @Parameter syntax for security.",
                "Apply row-level security by filtering on OrganisationId and DepartmentId based on manager access.",
                "Use CAST for percentage calculations to avoid integer division.",
                "Consider using TOP N for large result sets."
            }
        };

        return JsonSerializer.Serialize(examples, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string FormatDate(int dateInt)
    {
        var year = dateInt / 10000;
        var month = (dateInt / 100) % 100;
        var day = dateInt % 100;
        return $"{year:D4}-{month:D2}-{day:D2}";
    }
}

public class StatusValuesResult
{
    public List<string> StatusValues { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public class DateRangeResult
{
    public int MinDate { get; set; }
    public int MaxDate { get; set; }
    public string MinDateFormatted { get; set; } = string.Empty;
    public string MaxDateFormatted { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = new();
}

public class QueryExamplesResult
{
    public List<QueryExample> Examples { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public class QueryExample
{
    public string Description { get; set; } = string.Empty;
    public string NaturalLanguage { get; set; } = string.Empty;
    public string SqlTemplate { get; set; } = string.Empty;
}
