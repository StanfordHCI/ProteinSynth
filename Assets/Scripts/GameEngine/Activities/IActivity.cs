using System.Collections.Generic;
using GameEngine.Models;
using GameEngine.Systems;
using Newtonsoft.Json.Linq;

namespace GameEngine.Activities;

/// <summary>
/// Lesson-specific behavior. The engine core (session, actions, prompts, persistence)
/// knows nothing about any particular lesson; everything that does lives behind this
/// interface so one host app can run several activities (protein synthesis, enzymes, ...).
/// </summary>
public interface IActivity
{
    /// <summary>Stable identifier, e.g. "protein". Used for data folders and saves.</summary>
    string Id { get; }

    /// <summary>State the conversation starts in.</summary>
    string IntroStateId { get; }

    /// <summary>All states to load from GameData/Activities/{Id}/States/.</summary>
    IReadOnlyList<string> StateIds { get; }

    /// <summary>Scientific terms the tutor introduces carefully.</summary>
    IReadOnlyList<string> AdvancedConcepts { get; }

    /// <summary>Everyday concepts the tutor builds on.</summary>
    IReadOnlyCollection<string> FoundationalConcepts { get; }

    /// <summary>Extra properties in the model's structured response schema.</summary>
    IReadOnlyDictionary<string, object> ResponseFields { get; }

    /// <summary>Submitted as the student's turn when the hands-on activity finishes.</summary>
    string LabCompletedMessage { get; }

    void RegisterActions(ActionSystem actions);

    /// <summary>Adjusts freshly loaded states (e.g. fills in generated goals and actions).</summary>
    void PrepareStates(IDictionary<string, GameState> states);

    void ApplyActionSideEffects(string actionId, IDictionary<string, GameState> states);

    bool IsCompleted(IDictionary<string, GameState> states);

    /// <summary>Activity-specific instructions appended to the prompt's extra context.</summary>
    string BuildExtraContext(GameSession session, IReadOnlyList<string> unmetGoals);

    /// <summary>Activity-specific prompt placeholders, e.g. "{PROTEINS_LIST}" -> value.</summary>
    IReadOnlyDictionary<string, string> PromptValues(GameSession session);

    /// <summary>Runs after the model responds, before goals are marked met.</summary>
    void OnResponse(GameSession session, DrafterOutput output, IReadOnlyDictionary<string, bool> goalsMet);

    /// <summary>Runs when an accepted action is about to execute.</summary>
    void OnActionTaken(GameSession session, string actionId);

    /// <summary>Runs at the end of every turn, after goals unlock.</summary>
    void AfterTurn(GameSession session);

    /// <summary>True for actions that start the hands-on activity.</summary>
    bool IsLabAction(string actionId);

    /// <summary>True for the action that ends the experience.</summary>
    bool IsCompletionAction(string actionId);

    /// <summary>Yarn node to run for an action, or null if the action has none.</summary>
    string? DialogueNodeFor(string? actionId);

    /// <summary>
    /// Applies an explicit lab selection (a user entry point, not a model action).
    /// <paramref name="fresh"/> is a newly instantiated session supplying clean states.
    /// </summary>
    void SelectLab(GameSession session, GameSession fresh, string actionId);

    /// <summary>Migrates fields from saves written before activities existed.</summary>
    void UpgradeSession(SessionState state);

    /// <summary>Writes activity fields into the Python-server save format.</summary>
    void ExportServerFields(SessionState state, JObject game);

    /// <summary>Reads activity fields from the Python-server save format.</summary>
    void ImportServerFields(JObject game, SessionState state);
}
