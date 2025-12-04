using HistoricalReporting.AI.Agents;
using HistoricalReporting.AI.Configuration;
using HistoricalReporting.AI.Services;
using HistoricalReporting.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HistoricalReporting.AI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNlpQueryServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<ClaudeApiSettings>(configuration.GetSection(ClaudeApiSettings.SectionName));
        services.Configure<NlpQuerySettings>(configuration.GetSection(NlpQuerySettings.SectionName));

        // Register HTTP client for Claude API
        services.AddHttpClient("Claude", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var apiKey = configuration.GetSection(ClaudeApiSettings.SectionName)["ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        });

        // Register services
        services.AddScoped<SqlQueryValidator>();
        services.AddScoped<SqlGenerationAgent>();
        services.AddScoped<INlpQueryService, NlpQueryService>();

        return services;
    }
}
