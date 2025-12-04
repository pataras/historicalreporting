using System.Collections.Concurrent;
using HistoricalReporting.AI.Configuration;
using Microsoft.Extensions.Options;

namespace HistoricalReporting.Api.Middleware;

public class NlpQueryRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<NlpQueryRateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();

    public NlpQueryRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<NlpQueryRateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<NlpQuerySettings> settings)
    {
        // Only apply to NLP query endpoints
        if (!context.Request.Path.StartsWithSegments("/api/NlpQuery") ||
            context.Request.Method != HttpMethods.Post)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("manager_id")?.Value ??
                     context.User.FindFirst("sub")?.Value ??
                     context.Connection.RemoteIpAddress?.ToString() ??
                     "anonymous";

        var rateLimit = settings.Value.RateLimitPerMinute;
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        var rateLimitInfo = _rateLimitStore.AddOrUpdate(
            userId,
            _ => new RateLimitInfo { Requests = new List<DateTime> { now } },
            (_, existing) =>
            {
                // Remove old requests outside the window
                existing.Requests.RemoveAll(r => r < windowStart);
                existing.Requests.Add(now);
                return existing;
            });

        if (rateLimitInfo.Requests.Count > rateLimit)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId}. {Count} requests in the last minute.",
                userId, rateLimitInfo.Requests.Count);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Rate limit exceeded. Please wait before making more queries.",
                RetryAfterSeconds = 60
            });
            return;
        }

        // Add rate limit headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = rateLimit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rateLimit - rateLimitInfo.Requests.Count).ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private class RateLimitInfo
    {
        public List<DateTime> Requests { get; set; } = new();
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseNlpQueryRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<NlpQueryRateLimitingMiddleware>();
    }
}
