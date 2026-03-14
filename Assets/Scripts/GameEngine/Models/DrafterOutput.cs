using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameEngine.Models;

/// <summary>
/// Structured output from the LLM drafter call.
/// Port of DrafterOutput from reflexion.py.
/// </summary>
public class DrafterOutput
{
    [JsonPropertyName("chosen_goal_for_turn")]
    public string? ChosenGoalForTurn { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("chosen_protein")]
    public string? ChosenProtein { get; set; }

    [JsonPropertyName("pending_phrase_updates")]
    public List<PhraseUpdate>? PendingPhraseUpdates { get; set; } = new();

    [JsonPropertyName("goal_relevance_score")]
    public int? GoalRelevanceScore { get; set; }

    [JsonPropertyName("responsiveness_score")]
    public int? ResponsivenessScore { get; set; }

    [JsonPropertyName("summary_critique")]
    public string? SummaryCritique { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("student_interest")]
    public string? StudentInterest { get; set; }
}

public class PhraseUpdate
{
    [JsonPropertyName("phrase")]
    public string Phrase { get; set; } = "";

    [JsonPropertyName("concept")]
    public string Concept { get; set; } = "";
}

/// <summary>
/// Response data structure for the game engine.
/// Named GameResponse to avoid collision with Unity's ResponseData in WebSocketManager.cs.
/// </summary>
public class GameResponse
{
    public string Message { get; set; } = "";
    public Dictionary<string, bool>? GoalsMet { get; set; }
    public string? Action { get; set; }
    public string? StudentInterest { get; set; }
}
