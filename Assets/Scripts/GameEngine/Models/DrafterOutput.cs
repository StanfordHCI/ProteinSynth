using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameEngine.Models;

/// <summary>
/// Structured output from the LLM drafter call.
/// Port of DrafterOutput from reflexion.py.
/// </summary>
public class DrafterOutput
{
    [JsonProperty("chosen_goal_for_turn")]
    public string? ChosenGoalForTurn { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = "";

    [JsonProperty("chosen_protein")]
    public string? ChosenProtein { get; set; }

    [JsonProperty("pending_phrase_updates")]
    public List<PhraseUpdate>? PendingPhraseUpdates { get; set; } = new();

    [JsonProperty("goal_relevance_score")]
    public int? GoalRelevanceScore { get; set; }

    [JsonProperty("responsiveness_score")]
    public int? ResponsivenessScore { get; set; }

    [JsonProperty("summary_critique")]
    public string? SummaryCritique { get; set; }

    [JsonProperty("action")]
    public string? Action { get; set; }

    [JsonProperty("student_interest")]
    public string? StudentInterest { get; set; }
}

public class PhraseUpdate
{
    [JsonProperty("phrase")]
    public string Phrase { get; set; } = "";

    [JsonProperty("concept")]
    public string Concept { get; set; } = "";
}

/// <summary>
/// Response data structure for the game engine.
/// </summary>
public class GameResponse
{
    public string ReflectionCritique { get; set; } = "";
    public string? Reflection { get; set; }
    public string? ReflectionError { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, bool>? GoalsMet { get; set; }
    public string? Action { get; set; }
    public string? StudentInterest { get; set; }
}
