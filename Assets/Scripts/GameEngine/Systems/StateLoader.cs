using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
        var stateData = JsonConvert.DeserializeObject<StateJsonData>(stateJson)
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
        var json = JsonConvert.SerializeObject(data);
        var replaced = json.Replace("[[PERSONA]]", peerTutor);
        return JsonConvert.DeserializeObject<StateJsonData>(replaced)!;
    }
}
