using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameEngine.Models;

namespace GameEngine.Systems;

/// <summary>
/// Loads game states from JSON data.
/// Port of init_state() from testing_8.py:91-118.
/// </summary>
public class StateLoader
{
    private readonly ActionSystem _actionSystem;

    public StateLoader(ActionSystem actionSystem)
    {
        _actionSystem = actionSystem;
    }

    /// <summary>
    /// Initialize a GameState from raw JSON string and examples text.
    /// The caller is responsible for loading the data (from filesystem, TextAssets, etc.).
    /// </summary>
    /// <param name="stateJson">Contents of state.json</param>
    /// <param name="examplesText">Contents of examples.txt</param>
    /// <param name="username">Student name</param>
    /// <param name="gradeLevel">Student grade level</param>
    /// <param name="peerTutor">Selected peer tutor name</param>
    public GameState LoadState(
        string stateJson,
        string examplesText,
        string username,
        string gradeLevel,
        string peerTutor)
    {
        // Parse the JSON
        var stateData = JsonSerializer.Deserialize<StateJsonData>(stateJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to parse state JSON");

        // Replace [[PERSONA]] tokens
        var processedData = ReplacePersonaTokenInStateData(stateData, peerTutor);
        var processedExamples = examplesText.Replace("[[PERSONA]]", peerTutor);

        // Build action descriptions from the action system
        var actions = new Dictionary<string, string>();
        foreach (var actionName in processedData.Actions)
        {
            var description = _actionSystem.GetActionDescription(actionName);
            if (description != null)
            {
                actions[actionName] = description;
            }
            else
            {
                Console.WriteLine($"WARNING: Action {actionName} not found in action_dictionary");
            }
        }

        return new GameState
        {
            Id = processedData.Id,
            Description = processedData.Description,
            Location = processedData.Location,
            Characters = processedData.Characters,
            Goals = processedData.Goals,
            UnlockableGoals = processedData.UnlockableGoals,
            Actions = actions,
            Examples = processedExamples,
            Username = username,
            GradeLevel = gradeLevel,
        };
    }

    /// <summary>
    /// Replace [[PERSONA]] token recursively in the state data.
    /// Port of replace_persona_token() from persona_utils.py.
    /// </summary>
    private StateJsonData ReplacePersonaTokenInStateData(StateJsonData data, string peerTutor)
    {
        var json = JsonSerializer.Serialize(data);
        var replaced = json.Replace("[[PERSONA]]", peerTutor);
        return JsonSerializer.Deserialize<StateJsonData>(replaced,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>
    /// Update the intro state's unlockable goal to include the current PROTEINS_LIST.
    /// Port of update_protein_selection_goal() from protein_selection.py.
    /// This modifies the state in-place rather than writing to disk.
    /// </summary>
    public static void UpdateProteinSelectionGoal(GameState introState)
    {
        var proteinsListStr = $"['{string.Join("', '", Data.ProteinData.ProteinsList)}']";
        var prefix = "Introduce ONE of the following valid list of proteins based on the student's interest: ";

        // Find and replace the protein selection unlockable goal
        var goalToReplace = introState.UnlockableGoals.Keys
            .FirstOrDefault(g => g.StartsWith(prefix));

        if (goalToReplace != null)
        {
            var newGoal = prefix + proteinsListStr;
            var conditions = introState.UnlockableGoals[goalToReplace];
            introState.UnlockableGoals.Remove(goalToReplace);
            introState.UnlockableGoals[newGoal] = conditions;
        }

        // Add all protein lab actions to the state
        introState.Actions.Clear();
        foreach (var protein in Data.ProteinData.ProteinsList)
        {
            var actionId = $"TO_PROTEIN_SYNTHESIS_LAB_{protein.ToUpper()}";
            var desc = $"Start a lab to explore protein synthesis in depth, using the {protein} case study. IMPORTANT: This will IMMEDIATELY start the lab activity without giving the student a chance to respond. Do not call this action if you want to ask a follow-up question. If you take this action, do NOT prompt the student for a response.";
            introState.Actions[actionId] = desc;
        }
    }
}
