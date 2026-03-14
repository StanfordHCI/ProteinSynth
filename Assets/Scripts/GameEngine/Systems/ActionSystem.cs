using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Models;
using GameEngine.Data;

namespace GameEngine.Systems;

/// <summary>
/// Manages the action dictionary, availability checks, and action execution.
/// Port of actions.py (protein synthesis actions only).
/// </summary>
public class ActionSystem
{
    private readonly Dictionary<string, ActionDefinition> _actionDictionary = new();

    public ActionSystem()
    {
        RegisterProteinSynthesisActions();
        PopulateProteinActions();
    }

    /// <summary>
    /// Register the static protein synthesis actions.
    /// These are the only actions needed for the protein synthesis flow.
    /// </summary>
    private void RegisterProteinSynthesisActions()
    {
        _actionDictionary["TO_PROTEIN_SYNTHESIS_LAB"] = new ActionDefinition
        {
            Description = "Start the lab to explore protein synthesis in depth",
            ResponseMessage = "",
            SystemMessage = "The student has completed an augmented reality (AR) lab activity on protein synthesis and are now reflecting on their learning in the AR (augmented reality) activity about the processes of transcription and translation in protein synthesis.",
            NextStateId = "2_lab_reflection",
            Condition = ActionCondition.All(),
            Summarize = false
        };

        _actionDictionary["PROVIDE_VOCAB"] = new ActionDefinition
        {
            Description = "Provide students with the list of scientific vocabulary covered in the lesson (not as a bulleted or numbered list)",
            ResponseMessage = "Yari:: To summarize, here is the new scientific vocabulary we learned today!",
            SystemMessage = "The student is about to reflect on their learning in the AR (augmented reality) activity about the processes of transcription and translation in protein synthesis, practicing using the new scientific vocabulary they leanred.",
            NextStateId = null,
            Condition = ActionCondition.NoRequirement(),
            Summarize = false
        };

        _actionDictionary["ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL"] = new ActionDefinition
        {
            Description = "Kindly encourage the student in their lifelong learning journey and bid them farewell! IMPORTANT: This will IMMEDIATELY end the experience without giving the student a chance to respond. Do not call this action if you want to ask a follow-up question. If you take this action, do NOT prompt the student for a response.",
            ResponseMessage = "",
            SystemMessage = "The student has finished reflecting on their learning about the processes of transcription and translation in protein synthesis, and they will be moving onto the next step in their learning journey.",
            NextStateId = null,
            Condition = ActionCondition.All(),
            Summarize = false
        };
    }

    /// <summary>
    /// Dynamically generate TO_PROTEIN_SYNTHESIS_LAB_{PROTEIN} actions for each protein.
    /// Port of populate_protein_actions() from protein_selection.py.
    /// </summary>
    public void PopulateProteinActions()
    {
        foreach (var protein in ProteinData.ProteinsList)
        {
            var actionId = $"TO_PROTEIN_SYNTHESIS_LAB_{protein.ToUpper()}";
            if (!_actionDictionary.ContainsKey(actionId))
            {
                _actionDictionary[actionId] = new ActionDefinition
                {
                    Description = $"Start a lab to explore protein synthesis in depth, using the {protein} case study. IMPORTANT: This will IMMEDIATELY start the lab activity without giving the student a chance to respond. Do not call this action if you want to ask a follow-up question. If you take this action, do NOT prompt the student for a response.",
                    ResponseMessage = "",
                    SystemMessage = $"The student has completed an augmented-reality lab on protein synthesis using the specific example of {protein} and is now reflecting on their learning.",
                    NextStateId = "2_lab_reflection",
                    Condition = ActionCondition.All(),
                    Summarize = false
                };
            }
        }
    }

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
            return state.Goals.Count > 0 && state.Goals.Values.All(v => v);

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

        if (!CheckActionValid(actionId, currentState))
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
        switch (actionId)
        {
            case "PROVIDE_VOCAB":
                // Remove PROVIDE_VOCAB from lab_reflection state
                if (allStates.TryGetValue("2_lab_reflection", out var labState))
                    labState.Actions.Remove("PROVIDE_VOCAB");
                break;

            case "ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL":
                // Remove this action from lab_reflection state
                if (allStates.TryGetValue("2_lab_reflection", out var labState2))
                    labState2.Actions.Remove("ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL");
                break;
        }
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
    /// Check if the game is completed (no actions left in lab_reflection state).
    /// Port of is_game_completed() from actions.py.
    /// </summary>
    public bool IsGameCompleted(Dictionary<string, GameState> allStates)
    {
        return allStates.TryGetValue("2_lab_reflection", out var labState)
               && labState.Actions.Count == 0;
    }

    private void InjectSystemMessage(List<Dictionary<string, string>> messages, string content)
    {
        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "system",
            ["content"] = content
        });
    }
}
