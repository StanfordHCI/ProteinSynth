using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Models;
using GameEngine.Activities;

namespace GameEngine.Systems;

/// <summary>
/// Manages the action dictionary, availability checks, and action execution.
/// Port of actions.py. Action definitions come from the activity.
/// </summary>
public class ActionSystem
{
    private readonly Dictionary<string, ActionDefinition> _actionDictionary = new();
    private readonly IActivity _activity;

    public ActionSystem(IActivity activity)
    {
        _activity = activity;
        activity.RegisterActions(this);
    }

    /// <summary>Register (or replace) an action definition.</summary>
    public void Register(string actionId, ActionDefinition action) => _actionDictionary[actionId] = action;

    /// <summary>
    /// Check if an action's conditions are met in the given state.
    /// Port of check_action_available() from actions.py.
    /// </summary>
    public bool CheckActionAvailable(string actionId, GameState state)
    {
        if (!_actionDictionary.TryGetValue(actionId, out var action))
            return false;

        var condition = action.Condition;

        if (condition.RequiresAllGoals)
            return state.Goals.Count > 0 && state.UnlockableGoals.Count == 0 && state.Goals.Values.All(v => v);

        // Specific goals required
        foreach (var goalId in condition.RequiredGoals)
        {
            if (!state.Goals.TryGetValue(goalId, out var met) || !met)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if an action is registered in the state's action list.
    /// </summary>
    public bool CheckActionValid(string actionId, GameState state)
    {
        return state.Actions.ContainsKey(actionId);
    }

    /// <summary>
    /// Execute an action: handle state transitions, inject messages, run side effects.
    /// Port of take_action() from actions.py.
    /// </summary>
    public (GameState currentState, GameResponse? addedResponse) TakeAction(
        string actionId,
        GameState currentState,
        Dictionary<string, GameState> allStates,
        List<Dictionary<string, string>> messages)
    {
        GameResponse? addedResponse = null;

        if (!_actionDictionary.TryGetValue(actionId, out var action))
            return (currentState, addedResponse);

        if (!CheckActionValid(actionId, currentState) || !CheckActionAvailable(actionId, currentState))
        {
            InjectSystemMessage(messages, $"WARNING: The action {actionId} is not valid.");
            return (currentState, addedResponse);
        }

        Console.WriteLine($"\nDEBUG: Action {actionId} is valid and being called");

        // Execute side effects based on action ID
        ExecuteActionSideEffects(actionId, currentState, allStates);

        // State transition
        if (action.NextStateId != null && allStates.ContainsKey(action.NextStateId))
        {
            currentState = allStates[action.NextStateId];
        }

        // Response message
        if (!string.IsNullOrEmpty(action.ResponseMessage))
        {
            addedResponse = new GameResponse
            {
                Message = action.ResponseMessage,
                GoalsMet = null,
                Action = null
            };
        }

        // System message
        if (!string.IsNullOrEmpty(action.SystemMessage))
        {
            InjectSystemMessage(messages, "SYSTEM MESSAGE: " + action.SystemMessage);
        }

        return (currentState, addedResponse);
    }

    /// <summary>
    /// Handle action-specific side effects (removing actions from states, etc.)
    /// </summary>
    private void ExecuteActionSideEffects(
        string actionId,
        GameState currentState,
        Dictionary<string, GameState> allStates)
    {
        _activity.ApplyActionSideEffects(actionId, allStates);
    }

    /// <summary>
    /// Add an action to a state's action dictionary at runtime.
    /// Port of add_action() from actions.py.
    /// </summary>
    public void AddAction(Dictionary<string, GameState> allStates, string stateId, string actionId)
    {
        if (!allStates.TryGetValue(stateId, out var state))
        {
            Console.WriteLine($"State {stateId} not found in allStates");
            return;
        }

        if (_actionDictionary.TryGetValue(actionId, out var action))
        {
            state.Actions[actionId] = action.Description;
        }
        else
        {
            Console.WriteLine($"Action {actionId} not found in action_dictionary");
        }
    }

    /// <summary>
    /// Get the description of an action by ID.
    /// </summary>
    public string? GetActionDescription(string actionId)
    {
        return _actionDictionary.TryGetValue(actionId, out var action) ? action.Description : null;
    }

    /// <summary>
    /// Check if the game is completed, as defined by the activity.
    /// Port of is_game_completed() from actions.py.
    /// </summary>
    public bool IsGameCompleted(Dictionary<string, GameState> allStates) => _activity.IsCompleted(allStates);

    private void InjectSystemMessage(List<Dictionary<string, string>> messages, string content)
    {
        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "system",
            ["content"] = content
        });
    }
}
