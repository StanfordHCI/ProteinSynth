using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameEngine.Models;

namespace GameEngine.LLM;

/// <summary>
/// Direct Anthropic API client using HttpClient.
/// Replaces the entire LangGraph pipeline with a single Claude API call.
/// </summary>
public class AnthropicClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";

    public AnthropicClient(string apiKey, string model = "claude-haiku-4-5-20251001")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    /// <summary>
    /// Send a message to Claude and get a structured DrafterOutput response.
    /// Uses tool_use (function calling) for reliable structured output.
    /// </summary>
    public async Task<DrafterOutput> SendMessageAsync(string systemPrompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _model,
            max_tokens = 2048,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = "(Respond based on the context provided above.)" }
            },
            tools = new[]
            {
                new
                {
                    name = "respond",
                    description = "Generate a tutoring response with goal tracking and action selection.",
                    input_schema = GetDrafterOutputSchema()
                }
            },
            tool_choice = new { type = "tool", name = "respond" }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Anthropic API error ({response.StatusCode}): {responseBody}");
        }

        return ParseToolUseResponse(responseBody);
    }

    /// <summary>
    /// Parse the Anthropic API response to extract the tool_use input as DrafterOutput.
    /// </summary>
    private DrafterOutput ParseToolUseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Find the tool_use content block
        if (root.TryGetProperty("content", out var content))
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) &&
                    type.GetString() == "tool_use" &&
                    block.TryGetProperty("input", out var input))
                {
                    var inputJson = input.GetRawText();
                    return JsonSerializer.Deserialize<DrafterOutput>(inputJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new DrafterOutput { Message = "I'm sorry, something went wrong. Could you try again?" };
                }
            }
        }

        // Fallback: try to parse the text content as JSON
        if (root.TryGetProperty("content", out var textContent))
        {
            foreach (var block in textContent.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) &&
                    type.GetString() == "text" &&
                    block.TryGetProperty("text", out var text))
                {
                    var textStr = text.GetString() ?? "";
                    try
                    {
                        return JsonSerializer.Deserialize<DrafterOutput>(textStr,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                            ?? new DrafterOutput { Message = textStr };
                    }
                    catch
                    {
                        return new DrafterOutput { Message = textStr };
                    }
                }
            }
        }

        return new DrafterOutput { Message = "I'm sorry, I'm having trouble responding. Could you try again?" };
    }

    /// <summary>
    /// JSON schema for the DrafterOutput tool parameter.
    /// </summary>
    private static object GetDrafterOutputSchema()
    {
        return new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["chosen_goal_for_turn"] = new
                {
                    type = new[] { "string", "null" },
                    description = "The single goal you have chosen to focus on for this turn, or null if none."
                },
                ["message"] = new
                {
                    type = "string",
                    description = "The peer tutor's dialogue response."
                },
                ["chosen_protein"] = new
                {
                    type = new[] { "string", "null" },
                    description = "The single protein you chose to introduce to the student, if any."
                },
                ["pending_phrase_updates"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new Dictionary<string, object>
                        {
                            ["phrase"] = new { type = "string" },
                            ["concept"] = new { type = "string" }
                        },
                        required = new[] { "phrase", "concept" }
                    },
                    description = "Pending phrase updates based on student input of current turn."
                },
                ["goal_relevance_score"] = new
                {
                    type = new[] { "integer", "null" },
                    description = "Score between 1-5 for goal relevance, or null."
                },
                ["responsiveness_score"] = new
                {
                    type = new[] { "integer", "null" },
                    description = "Score between 1-5 for responsiveness."
                },
                ["summary_critique"] = new
                {
                    type = new[] { "string", "null" },
                    description = "2-3 sentences summarizing how to improve the response."
                },
                ["action"] = new
                {
                    type = new[] { "string", "null" },
                    description = "The name of the action being taken, or null if none."
                },
                ["student_interest"] = new
                {
                    type = new[] { "string", "null" },
                    description = "The single student interest being referenced, if any."
                }
            },
            required = new[] { "message" }
        };
    }
}
