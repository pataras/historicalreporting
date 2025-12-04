namespace HistoricalReporting.Core.Models;

public class NlpQueryRequest
{
    public required string Query { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid? ManagerId { get; set; }
    public bool ManagesAllDepartments { get; set; }
    public List<Guid>? AccessibleDepartmentIds { get; set; }
}

public class NlpQueryResponse
{
    public bool Success { get; set; }
    public string? NaturalLanguageQuery { get; set; }
    public string? GeneratedSql { get; set; }
    public string? Explanation { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<Dictionary<string, object?>> Results { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public int TotalRows { get; set; }
    public bool WasTruncated { get; set; }
    public double ExecutionTimeMs { get; set; }
    public bool ClarificationNeeded { get; set; }
    public string? ClarificationMessage { get; set; }
    public string? Error { get; set; }
}

public class NlpQueryHistoryItem
{
    public Guid Id { get; set; }
    public Guid ManagerId { get; set; }
    public required string Query { get; set; }
    public string? GeneratedSql { get; set; }
    public bool Success { get; set; }
    public int ResultCount { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class NlpQueryAuditLog
{
    public Guid Id { get; set; }
    public Guid ManagerId { get; set; }
    public Guid OrganisationId { get; set; }
    public required string NaturalLanguageQuery { get; set; }
    public string? GeneratedSql { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResultCount { get; set; }
    public double ExecutionTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
