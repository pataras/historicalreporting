namespace HistoricalReporting.Core.Entities;

public class NlpQueryLog : BaseEntity
{
    public Guid ManagerId { get; set; }
    public Manager? Manager { get; set; }

    public Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public required string NaturalLanguageQuery { get; set; }
    public string? GeneratedSql { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResultCount { get; set; }
    public double ExecutionTimeMs { get; set; }
}
