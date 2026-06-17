using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameEngine.Models;

/// <summary>
/// Represents a single game state with goals, actions, and unlockable goals.
/// Port of State class from testing_8.py.
/// </summary>
public class GameState
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public List<string> Characters { get; set; } = new();
    public string Examples { get; set; } = "";
    public string Username { get; set; } = "";
    public string GradeLevel { get; set; } = "";

    /// <summary>Goal name -> met (true/false)</summary>
    public Dictionary<string, bool> Goals { get; set; } = new();

    /// <summary>Goal name -> list of prerequisite goal names that must be met to unlock</summary>
    public Dictionary<string, List<string>> UnlockableGoals { get; set; } = new();

    /// <summary>Action ID -> description (all actions registered for this state)</summary>
    public Dictionary<string, string> Actions { get; set; } = new();

    /// <summary>Actions whose conditions are currently met</summary>
    [JsonIgnore]
    public Dictionary<string, string> Available { get; set; } = new();

    /// <summary>Actions whose conditions are NOT currently met</summary>
    [JsonIgnore]
    public Dictionary<string, string> Unavailable { get; set; } = new();

    /// <summary>
    /// Recomputes Available/Unavailable by checking each action's conditions.
    /// </summary>
    public void UpdateActions(Systems.ActionSystem actionSystem)
    {
        Available = new Dictionary<string, string>();
        Unavailable = new Dictionary<string, string>();

        foreach (var (actionId, description) in Actions)
        {
            if (actionSystem.CheckActionAvailable(actionId, this))
                Available[actionId] = description;
            else
                Unavailable[actionId] = description;
        }
    }
}

/// <summary>
/// JSON shape of state.json files on disk (before processing).
/// </summary>
public class StateJsonData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("goals")]
    public Dictionary<string, bool> Goals { get; set; } = new();

    [JsonPropertyName("unlockable_goals")]
    public Dictionary<string, List<string>> UnlockableGoals { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<string> Actions { get; set; } = new();

    [JsonPropertyName("location")]
    public string Location { get; set; } = "";

    [JsonPropertyName("characters")]
    public List<string> Characters { get; set; } = new();
}
