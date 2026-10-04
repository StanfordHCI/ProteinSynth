using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Models;

namespace GameEngine.LLM;

/// <summary>
/// Claude drafter and reflection adapter. Unity supplies an authenticated transport;
/// the standalone harness can use a development API key.
/// </summary>
public class AnthropicClient : ITutorModel, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Func<string, CancellationToken, Task<string>>? _transport;
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

    public AnthropicClient(Func<string, CancellationToken, Task<string>> transport)
    {
        _transport = transport; _apiKey = ""; _model = "claude-haiku-4-5-20251001";
        _httpClient = new HttpClient();
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<string> SendAsync(string json, CancellationToken token)
    {
        if (_transport != null) return await _transport(json, token);
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        using var response = await _httpClient.SendAsync(request, token);
        var body = await response.Content.ReadAsStringAsync();
        token.ThrowIfCancellationRequested();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Tutor service returned HTTP {(int)response.StatusCode}.");
        return body;
    }

    public async Task<string> ReflectAsync(string tutor, List<Dictionary<string, string>> messages,
        string critique, CancellationToken cancellationToken)
    {
        var prompt = "Reflect privately on this tutoring conversation. Identify what helped or confused the student, "
            + "their interests and misconceptions, and concrete adjustments for future turns. "
            + "Do not revise the last response. Return at most 150 words. Tutor: " + tutor
            + "\nConversation: " + JsonConvert.SerializeObject(messages)
            + "\nCritique of the latest turn: " + critique;
        var body = await SendAsync(JsonConvert.SerializeObject(new {
            model = _model, max_tokens = 384, system = prompt,
            messages = new[] { new { role = "user", content = "Write the reflection." } }
        }), cancellationToken);
        var root = JObject.Parse(body);
        var text = new StringBuilder();
        foreach (var block in root["content"] ?? new JArray())
            if ((string?)block["type"] == "text") text.AppendLine((string?)block["text"]);
        if (text.Length == 0) throw new InvalidDataException("The tutor returned no reflection.");
        return text.ToString().Trim();
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

        var jsonContent = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var responseBody = await SendAsync(jsonContent, cancellationToken);
        return ParseToolUseResponse(responseBody);
    }

    /// <summary>
    /// Parse the Anthropic API response to extract the tool_use input as DrafterOutput.
    /// </summary>
    private DrafterOutput ParseToolUseResponse(string responseBody)
    {
        var root = JObject.Parse(responseBody);
        if ((string?)root["stop_reason"] == "max_tokens")
            throw new InvalidDataException("The tutor response was truncated.");

        // Find the tool_use content block
        if (root.TryGetValue("content", out var content))
        {
            foreach (var block in content)
            {
                if (block is JObject toolBlock &&
                    (string?)toolBlock["type"] == "tool_use" &&
                    (string?)toolBlock["name"] == "respond" &&
                    toolBlock.TryGetValue("input", out var input))
                {
                    var inputJson = input.ToString(Formatting.None);
                    return JsonConvert.DeserializeObject<DrafterOutput>(inputJson)
                        ?? throw new InvalidDataException("The tutor response was empty.");
                }
            }
        }

        throw new InvalidDataException("The tutor did not return the expected structured response.");
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
