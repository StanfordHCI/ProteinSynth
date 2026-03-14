using GameEngine.Models;

namespace GameEngine.Systems;

/// <summary>
/// Manages goal unlocking and tracking.
/// Port of unlock_goals() and related logic from testing_8.py.
/// </summary>
public static class GoalSystem
{
    /// <summary>
    /// Check unlockable goals whose prerequisites are all met,
    /// and promote them to active (unmet) goals.
    /// Port of Game.unlock_goals() from testing_8.py:554-564.
    /// </summary>
    public static void UnlockGoals(GameState state)
    {
        if (state.UnlockableGoals.Count == 0) return;

        var goalsToUnlock = new List<string>();

        foreach (var (goal, prerequisites) in state.UnlockableGoals)
        {
            if (prerequisites.All(prereq =>
                state.Goals.TryGetValue(prereq, out var met) && met))
            {
                goalsToUnlock.Add(goal);
            }
        }

        foreach (var goal in goalsToUnlock)
        {
            Console.WriteLine($"DEBUG: Unlocking goal {goal}");
            state.Goals[goal] = false; // Add as unmet goal
            state.UnlockableGoals.Remove(goal);
        }
    }

    /// <summary>
    /// Mark goals as met from the LLM output.
    /// </summary>
    public static void MarkGoalsMet(GameState state, Dictionary<string, bool>? goalsMet)
    {
        if (goalsMet == null) return;

        foreach (var (goal, met) in goalsMet)
        {
            if (met && state.Goals.ContainsKey(goal))
            {
                state.Goals[goal] = true;
            }
        }
    }

    /// <summary>
    /// Get list of unmet goal names.
    /// </summary>
    public static List<string> GetUnmetGoals(GameState state)
    {
        return state.Goals
            .Where(kv => !kv.Value)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>
    /// Check if all goals in the state are met.
    /// </summary>
    public static bool AllGoalsMet(GameState state)
    {
        return state.Goals.Count > 0 && state.Goals.Values.All(v => v);
    }
}
