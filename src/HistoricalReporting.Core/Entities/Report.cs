namespace HistoricalReporting.Core.Entities;

public class Report : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Query { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Draft;
}

public enum ReportStatus
{
    Draft,
    Active,
    Archived
}
