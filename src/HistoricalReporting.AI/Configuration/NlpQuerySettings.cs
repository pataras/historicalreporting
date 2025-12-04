namespace HistoricalReporting.AI.Configuration;

public class NlpQuerySettings
{
    public const string SectionName = "NlpQuery";

    public int MaxResultRows { get; set; } = 1000;
    public int QueryTimeoutSeconds { get; set; } = 30;
    public bool EnableQueryHistory { get; set; } = true;
    public bool EnableSqlPreview { get; set; } = true;
    public int RateLimitPerMinute { get; set; } = 10;
}
