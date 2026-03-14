using System.Collections.Generic;
using System.Linq;

namespace GameEngine.Models;

/// <summary>
/// Defines a game action with its conditions and effects.
/// Port of action_dictionary entries from actions.py.
/// </summary>
public class ActionDefinition
{
    /// <summary>Short description shown to the LLM</summary>
    public string Description { get; set; } = "";

    /// <summary>Message sent when action triggers (empty = no message)</summary>
    public string ResponseMessage { get; set; } = "";

    /// <summary>System note injected when action triggers</summary>
    public string SystemMessage { get; set; } = "";

    /// <summary>State to transition to (null = stay in current state)</summary>
    public string? NextStateId { get; set; }

    /// <summary>
    /// "all" = all goals in state must be true.
    /// Empty list = no goals required (always available).
    /// List of goal names = those specific goals must be true.
    /// </summary>
    public ActionCondition Condition { get; set; } = ActionCondition.NoRequirement();

    /// <summary>Whether to summarize conversation before executing</summary>
    public bool Summarize { get; set; }
}

/// <summary>
/// Represents the condition for an action to be available.
/// Either "all goals must be met" or "specific goals must be met" or "no requirement".
/// </summary>
public class ActionCondition
{
    public bool RequiresAllGoals { get; set; }
    public List<string> RequiredGoals { get; set; } = new();

    public static ActionCondition All() => new() { RequiresAllGoals = true };
    public static ActionCondition NoRequirement() => new() { RequiresAllGoals = false, RequiredGoals = new() };
    public static ActionCondition Specific(params string[] goals) => new() { RequiresAllGoals = false, RequiredGoals = goals.ToList() };
}
