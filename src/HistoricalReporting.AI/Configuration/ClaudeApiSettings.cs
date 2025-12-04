namespace HistoricalReporting.AI.Configuration;

public class ClaudeApiSettings
{
    public const string SectionName = "ClaudeApi";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.1;
}
