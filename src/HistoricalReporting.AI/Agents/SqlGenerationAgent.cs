using HistoricalReporting.AI.Configuration;
using HistoricalReporting.AI.Plugins;
using HistoricalReporting.AI.Services;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace HistoricalReporting.AI.Agents;

public class SqlGenerationAgent
{
    private readonly Kernel _kernel;
    private readonly ChatCompletionAgent _agent;
    private readonly ClaudeApiSettings _claudeSettings;
    private readonly NlpQuerySettings _nlpSettings;

    private const string AgentInstructions = """
        You are a SQL query generation assistant for a historical reporting system. Your job is to convert natural language questions into valid SQL Server queries.

        ## Your Capabilities
        1. Use the database schema tool to understand table structures and relationships
        2. Use entity lookup tools to find correct IDs for organisations and departments
        3. Generate safe, read-only SELECT queries

        ## Rules
        1. ONLY generate SELECT queries - never INSERT, UPDATE, DELETE, DROP, or any DDL
        2. Always use parameterized query placeholders (@ParameterName) for dynamic values
        3. Apply appropriate JOINs based on the schema relationships
        4. Include WHERE clauses to filter by organisation/department when relevant
        5. Use proper date handling: Date column is yyyyMMdd integer format
        6. Never select PasswordHash or other sensitive columns
        7. Use TOP N to limit large result sets (default TOP 1000)
        8. Include ORDER BY for predictable results

        ## Response Format
        When you have enough information to generate a query, respond with a JSON object:
        {
            "sql": "The generated SQL query with @Parameters",
            "parameters": { "ParameterName": "value or description" },
            "explanation": "Brief explanation of what the query does",
            "warnings": ["Any warnings or limitations"]
        }

        If you need more information, ask clarifying questions.

        ## Common Patterns
        - Count by status: COUNT(*) with GROUP BY Status
        - Monthly aggregation: GROUP BY Date/10000, (Date/100)%100
        - Department filtering: JOIN OrganisationUsers ou ON ... JOIN Departments d ON ou.DepartmentId = d.Id
        - Organisation filtering: WHERE ou.OrganisationId = @OrganisationId
        """;

    public SqlGenerationAgent(
        ApplicationDbContext dbContext,
        IOptions<ClaudeApiSettings> claudeSettings,
        IOptions<NlpQuerySettings> nlpSettings,
        IHttpClientFactory httpClientFactory)
    {
        _claudeSettings = claudeSettings.Value;
        _nlpSettings = nlpSettings.Value;

        // Build kernel with Claude via OpenAI connector
        var builder = Kernel.CreateBuilder();

        // Use custom HTTP client for Claude API
        var httpClient = httpClientFactory.CreateClient("Claude");
        httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");

        builder.AddOpenAIChatCompletion(
            modelId: _claudeSettings.Model,
            apiKey: _claudeSettings.ApiKey,
            httpClient: httpClient);

        // Add plugins
        builder.Plugins.AddFromObject(new DatabaseSchemaPlugin(dbContext), "Schema");
        builder.Plugins.AddFromObject(new EntityLookupPlugin(dbContext), "Entities");

        _kernel = builder.Build();

        // Create the agent
        _agent = new ChatCompletionAgent
        {
            Name = "SqlGenerator",
            Instructions = AgentInstructions,
            Kernel = _kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };
    }

    public async Task<SqlGenerationResult> GenerateSqlAsync(
        string naturalLanguageQuery,
        QueryContext context,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();

        // Add context about the user's access
        var contextMessage = BuildContextMessage(context);
        chatHistory.AddSystemMessage(contextMessage);

        // Add the user's query
        chatHistory.AddUserMessage(naturalLanguageQuery);

        try
        {
            // Invoke the agent
            var responses = new List<string>();
            await foreach (var response in _agent.InvokeAsync(chatHistory, cancellationToken: cancellationToken))
            {
                if (response.Content != null)
                {
                    responses.Add(response.Content);
                }
            }

            var fullResponse = string.Join("\n", responses);

            // Try to parse as JSON result
            var result = ParseAgentResponse(fullResponse);
            result.NaturalLanguageQuery = naturalLanguageQuery;

            return result;
        }
        catch (Exception ex)
        {
            return new SqlGenerationResult
            {
                Success = false,
                NaturalLanguageQuery = naturalLanguageQuery,
                Error = $"Failed to generate SQL: {ex.Message}"
            };
        }
    }

    private string BuildContextMessage(QueryContext context)
    {
        var contextParts = new List<string>
        {
            $"Current user's organisation ID: {context.OrganisationId}"
        };

        if (context.ManagerId.HasValue)
        {
            contextParts.Add($"Current manager ID: {context.ManagerId}");
        }

        if (context.ManagesAllDepartments)
        {
            contextParts.Add("This manager has access to ALL departments in their organisation.");
        }
        else if (context.AccessibleDepartmentIds?.Any() == true)
        {
            contextParts.Add($"This manager has access to departments: {string.Join(", ", context.AccessibleDepartmentIds)}");
            contextParts.Add("IMPORTANT: Filter results to only include these department IDs.");
        }

        return string.Join("\n", contextParts);
    }

    private SqlGenerationResult ParseAgentResponse(string response)
    {
        // Try to extract JSON from the response
        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            try
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = System.Text.Json.JsonSerializer.Deserialize<AgentJsonResponse>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed != null && !string.IsNullOrEmpty(parsed.Sql))
                {
                    return new SqlGenerationResult
                    {
                        Success = true,
                        GeneratedSql = parsed.Sql,
                        Parameters = parsed.Parameters ?? new Dictionary<string, object>(),
                        Explanation = parsed.Explanation ?? "Query generated successfully.",
                        Warnings = parsed.Warnings ?? new List<string>()
                    };
                }
            }
            catch
            {
                // Fall through to return the raw response
            }
        }

        // If we couldn't parse JSON, the agent might be asking for clarification
        return new SqlGenerationResult
        {
            Success = false,
            ClarificationNeeded = true,
            ClarificationMessage = response,
            Error = "Could not generate SQL. Please provide more details."
        };
    }

    private class AgentJsonResponse
    {
        public string? Sql { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public string? Explanation { get; set; }
        public List<string>? Warnings { get; set; }
    }
}

public class SqlGenerationResult
{
    public bool Success { get; set; }
    public string? NaturalLanguageQuery { get; set; }
    public string? GeneratedSql { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Explanation { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool ClarificationNeeded { get; set; }
    public string? ClarificationMessage { get; set; }
    public string? Error { get; set; }
}

public class QueryContext
{
    public Guid OrganisationId { get; set; }
    public Guid? ManagerId { get; set; }
    public bool ManagesAllDepartments { get; set; }
    public List<Guid>? AccessibleDepartmentIds { get; set; }
}
