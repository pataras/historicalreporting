using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HistoricalReporting.AI.Configuration;
using Microsoft.Extensions.Options;

namespace HistoricalReporting.AI.Services;

/// <summary>
/// HTTP handler that translates OpenAI API format to Anthropic Claude API format.
/// This allows Semantic Kernel's OpenAI connector to work with Claude.
/// </summary>
public class ClaudeHttpClientHandler : DelegatingHandler
{
    private readonly ClaudeApiSettings _settings;
    private const string ClaudeApiBaseUrl = "https://api.anthropic.com/v1";

    public ClaudeHttpClientHandler(IOptions<ClaudeApiSettings> settings)
    {
        _settings = settings.Value;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only intercept chat completions requests
        if (request.RequestUri?.PathAndQuery.Contains("/chat/completions") == true)
        {
            return await SendToClaudeAsync(request, cancellationToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendToClaudeAsync(
        HttpRequestMessage originalRequest, CancellationToken cancellationToken)
    {
        var originalContent = await originalRequest.Content!.ReadAsStringAsync(cancellationToken);
        var openAiRequest = JsonNode.Parse(originalContent)!;

        // Convert OpenAI format to Claude format
        var claudeRequest = ConvertToClaudeFormat(openAiRequest);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ClaudeApiBaseUrl}/messages")
        {
            Content = new StringContent(
                claudeRequest.ToJsonString(),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("x-api-key", _settings.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var claudeResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = ConvertToOpenAiFormat(claudeResponse);

            response.Content = new StringContent(
                openAiResponse,
                Encoding.UTF8,
                "application/json");
        }

        return response;
    }

    private JsonObject ConvertToClaudeFormat(JsonNode openAiRequest)
    {
        var messages = openAiRequest["messages"]?.AsArray() ?? new JsonArray();
        var claudeMessages = new JsonArray();
        string? systemPrompt = null;

        foreach (var message in messages)
        {
            var role = message?["role"]?.GetValue<string>();
            var content = message?["content"];

            if (role == "system")
            {
                systemPrompt = content?.GetValue<string>();
                continue;
            }

            var claudeRole = role == "assistant" ? "assistant" : "user";

            // Handle tool calls in assistant messages
            if (role == "assistant" && message?["tool_calls"] != null)
            {
                var toolCalls = message["tool_calls"]!.AsArray();
                var contentBlocks = new JsonArray();

                if (content != null)
                {
                    contentBlocks.Add(new JsonObject { ["type"] = "text", ["text"] = content.GetValue<string>() });
                }

                foreach (var toolCall in toolCalls)
                {
                    contentBlocks.Add(new JsonObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = toolCall?["id"]?.GetValue<string>(),
                        ["name"] = toolCall?["function"]?["name"]?.GetValue<string>(),
                        ["input"] = JsonNode.Parse(toolCall?["function"]?["arguments"]?.GetValue<string>() ?? "{}")
                    });
                }

                claudeMessages.Add(new JsonObject
                {
                    ["role"] = claudeRole,
                    ["content"] = contentBlocks
                });
            }
            // Handle tool results
            else if (role == "tool")
            {
                var toolResultContent = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = message?["tool_call_id"]?.GetValue<string>(),
                        ["content"] = content?.GetValue<string>()
                    }
                };

                claudeMessages.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = toolResultContent
                });
            }
            else
            {
                claudeMessages.Add(new JsonObject
                {
                    ["role"] = claudeRole,
                    ["content"] = content?.GetValue<string>()
                });
            }
        }

        var claudeRequest = new JsonObject
        {
            ["model"] = _settings.Model,
            ["max_tokens"] = _settings.MaxTokens,
            ["messages"] = claudeMessages
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            claudeRequest["system"] = systemPrompt;
        }

        // Handle tools
        var tools = openAiRequest["tools"]?.AsArray();
        if (tools != null && tools.Count > 0)
        {
            var claudeTools = new JsonArray();
            foreach (var tool in tools)
            {
                if (tool?["type"]?.GetValue<string>() == "function")
                {
                    var function = tool["function"];
                    claudeTools.Add(new JsonObject
                    {
                        ["name"] = function?["name"]?.GetValue<string>(),
                        ["description"] = function?["description"]?.GetValue<string>(),
                        ["input_schema"] = function?["parameters"]?.DeepClone()
                    });
                }
            }
            claudeRequest["tools"] = claudeTools;
        }

        return claudeRequest;
    }

    private string ConvertToOpenAiFormat(string claudeResponse)
    {
        var claude = JsonNode.Parse(claudeResponse)!;
        var content = claude["content"]?.AsArray();

        string? textContent = null;
        JsonArray? toolCalls = null;

        if (content != null)
        {
            foreach (var block in content)
            {
                var type = block?["type"]?.GetValue<string>();
                if (type == "text")
                {
                    textContent = block?["text"]?.GetValue<string>();
                }
                else if (type == "tool_use")
                {
                    toolCalls ??= new JsonArray();
                    toolCalls.Add(new JsonObject
                    {
                        ["id"] = block?["id"]?.GetValue<string>(),
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = block?["name"]?.GetValue<string>(),
                            ["arguments"] = block?["input"]?.ToJsonString()
                        }
                    });
                }
            }
        }

        var message = new JsonObject
        {
            ["role"] = "assistant"
        };

        if (textContent != null)
        {
            message["content"] = textContent;
        }

        if (toolCalls != null)
        {
            message["tool_calls"] = toolCalls;
        }

        var openAiResponse = new JsonObject
        {
            ["id"] = claude["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString(),
            ["object"] = "chat.completion",
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["model"] = _settings.Model,
            ["choices"] = new JsonArray
            {
                new JsonObject
                {
                    ["index"] = 0,
                    ["message"] = message,
                    ["finish_reason"] = MapStopReason(claude["stop_reason"]?.GetValue<string>())
                }
            },
            ["usage"] = new JsonObject
            {
                ["prompt_tokens"] = claude["usage"]?["input_tokens"]?.GetValue<int>() ?? 0,
                ["completion_tokens"] = claude["usage"]?["output_tokens"]?.GetValue<int>() ?? 0,
                ["total_tokens"] = (claude["usage"]?["input_tokens"]?.GetValue<int>() ?? 0) +
                                  (claude["usage"]?["output_tokens"]?.GetValue<int>() ?? 0)
            }
        };

        return openAiResponse.ToJsonString();
    }

    private static string MapStopReason(string? claudeReason)
    {
        return claudeReason switch
        {
            "end_turn" => "stop",
            "tool_use" => "tool_calls",
            "max_tokens" => "length",
            _ => "stop"
        };
    }
}
