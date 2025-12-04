namespace HistoricalReporting.Core.Models;

public class MonthlyUserStatusReport
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int TotalCount => ValidCount + InvalidCount;
}

public class MonthlyUserStatusReportResult
{
    public Guid OrganisationId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public List<MonthlyUserStatusReport> MonthlyData { get; set; } = [];
}
