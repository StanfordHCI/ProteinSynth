using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Models;
using GameEngine.Systems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameEngine.Activities;

/// <summary>
/// Protein synthesis lesson: intro conversation -> AR lab on a chosen protein ->
/// scaffolded reflection -> farewell. Port of protein_selection.py and the protein
/// parts of testing_8.py / actions.py.
/// </summary>
public sealed class ProteinSynthesisActivity : IActivity
{
    public const string IntroState = "0_intro_proteinSynthesis";
    public const string ReflectionState = "2_lab_reflection";
    public const string LabActionPrefix = "TO_PROTEIN_SYNTHESIS_LAB_";
    public const string FarewellAction = "ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL";
    private const string ChosenProteinKey = "chosen_protein";
    private const string ProteinGoalPrefix = "Introduce ONE of the following valid list of proteins based on the student's interest: ";

    public static readonly IReadOnlyList<string> Proteins = new[]
    {
        "lactase", "hemoglobin", "insulin", "myosin",
        "keratin", "immunoglobulins", "tyrosinase", "cytokines"
    };

    /// <param name="saveFolder">Null keeps ProteinSynth's legacy save location; a multi-activity host passes "protein".</param>
    public ProteinSynthesisActivity(string? saveFolder = null) => SaveFolder = saveFolder;

    public string Id => "protein";
    public string? SaveFolder { get; }
    public string IntroStateId => IntroState;
    public IReadOnlyList<string> StateIds { get; } = new[] { IntroState, ReflectionState };

    public IReadOnlyList<string> AdvancedConcepts { get; } = new[]
    {
        "protein synthesis", "transcription", "translation",
        "ribosomes", "mRNA", "tRNA", "amino acids", "codon"
    };

    public IReadOnlyCollection<string> FoundationalConcepts { get; } = new HashSet<string>
    {
        "cell", "nucleus", "instruction", "building blocks", "growth", "jobs"
    };

    public IReadOnlyDictionary<string, object> ResponseFields { get; } = new Dictionary<string, object>
    {
        [ChosenProteinKey] = new
        {
            type = new[] { "string", "null" },
            description = "The single protein you chose to introduce to the student, if any."
        }
    };

    public string LabCompletedMessage => "(The student completed the AR protein synthesis lab.)";

    private static string ProteinGoal => ProteinGoalPrefix + $"['{string.Join("', '", Proteins)}']";
    private static string LabAction(string protein) => LabActionPrefix + protein.ToUpper();
    private static string LabActionDescription(string protein) =>
        $"Start a lab to explore protein synthesis in depth, using the {protein} case study. IMPORTANT: This will IMMEDIATELY start the lab activity without giving the student a chance to respond. Do not call this action if you want to ask a follow-up question. If you take this action, do NOT prompt the student for a response.";

    public static string? ChosenProtein(GameSession session) =>
        session.ActivityValues.TryGetValue(ChosenProteinKey, out var value) ? value : null;

    private static void SetChosenProteinValue(GameSession session, string protein) =>
        session.ActivityValues[ChosenProteinKey] = protein;

    public void RegisterActions(ActionSystem actions)
    {
        actions.Register("TO_PROTEIN_SYNTHESIS_LAB", new ActionDefinition
        {
            Description = "Start the lab to explore protein synthesis in depth",
            SystemMessage = "The student has completed an augmented reality (AR) lab activity on protein synthesis and are now reflecting on their learning in the AR (augmented reality) activity about the processes of transcription and translation in protein synthesis.",
            NextStateId = ReflectionState,
            Condition = ActionCondition.All()
        });
        actions.Register("PROVIDE_VOCAB", new ActionDefinition
        {
            Description = "Provide students with the list of scientific vocabulary covered in the lesson (not as a bulleted or numbered list)",
            ResponseMessage = "Yari:: To summarize, here is the new scientific vocabulary we learned today!",
            SystemMessage = "The student is about to reflect on their learning in the AR (augmented reality) activity about the processes of transcription and translation in protein synthesis, practicing using the new scientific vocabulary they leanred.",
            Condition = ActionCondition.NoRequirement()
        });
        actions.Register(FarewellAction, new ActionDefinition
        {
            Description = "Kindly encourage the student in their lifelong learning journey and bid them farewell! IMPORTANT: This will IMMEDIATELY end the experience without giving the student a chance to respond. Do not call this action if you want to ask a follow-up question. If you take this action, do NOT prompt the student for a response.",
            SystemMessage = "The student has finished reflecting on their learning about the processes of transcription and translation in protein synthesis, and they will be moving onto the next step in their learning journey.",
            Condition = ActionCondition.All()
        });
        foreach (var protein in Proteins)
        {
            actions.Register(LabAction(protein), new ActionDefinition
            {
                Description = LabActionDescription(protein),
                SystemMessage = $"The student has completed an augmented-reality lab on protein synthesis using the specific example of {protein} and is now reflecting on their learning.",
                NextStateId = ReflectionState,
                Condition = ActionCondition.All()
            });
        }
    }

    // Port of update_protein_selection_goal(): the intro goal names the current protein
    // list, and the intro state offers every protein's lab action.
    public void PrepareStates(IDictionary<string, GameState> states)
    {
        var intro = states[IntroState];
        var goalToReplace = intro.UnlockableGoals.Keys.FirstOrDefault(g => g.StartsWith(ProteinGoalPrefix));
        if (goalToReplace != null)
        {
            var conditions = intro.UnlockableGoals[goalToReplace];
            intro.UnlockableGoals.Remove(goalToReplace);
            intro.UnlockableGoals[ProteinGoal] = conditions;
        }
        intro.Actions.Clear();
        foreach (var protein in Proteins)
            intro.Actions[LabAction(protein)] = LabActionDescription(protein);
    }

    public void ApplyActionSideEffects(string actionId, IDictionary<string, GameState> states)
    {
        if ((actionId == "PROVIDE_VOCAB" || actionId == FarewellAction)
            && states.TryGetValue(ReflectionState, out var reflection))
            reflection.Actions.Remove(actionId);
    }

    public bool IsCompleted(IDictionary<string, GameState> states) =>
        states.TryGetValue(ReflectionState, out var reflection) && reflection.Actions.Count == 0;

    public string BuildExtraContext(GameSession session, IReadOnlyList<string> unmetGoals)
    {
        var extraContext = "";
        if (session.CurrentGameState.Id == IntroState && unmetGoals.Contains(ProteinGoal))
        {
            extraContext += "Choose one protein to introduce the student to and populate the `chosen_protein` field of the JSON you are returning with this protein. The value should be a string.";
        }
        if (session.CurrentGameState.Id == ReflectionState)
        {
            extraContext += @"
          ### SPECIAL DIALOGUE INSTRUCTION — Scaffolded, learner-friendly feedback
          Your primary goal is to evaluate the student's reflection and respond in a way that keeps them motivated and clear on next steps—without revealing internal criteria.

          IF THE REFLECTION **MEETS** THE TARGET:
          - Respond with brief, specific positive reinforcement that names what worked (1 sentence), then transition the lesson (1 sentence).
          - Keep it concise; no new tasks unless it advances the next goal.

          IF THE REFLECTION **DOES NOT MEET** THE TARGET:
          - Use the pattern: **Affirm → Diagnose → Guide → Ask**.
            1) **Affirm** one specific thing they did (use their wording).
            2) **Diagnose (internally)** the main gap using one or two tags from this list (do NOT show tags to the student):
              - `missing_terms` (didn't bring in enough precise terms)
              - `weak_connection` (real-world/personal link is vague)
              - `mechanism_unclear` (the ""how/why"" is thin)
              - `transfer_mismatch` (near/far mapping is off)
              - `misconception` (scientific error)
            3) **Guide** with ONE actionable cue in friendly, non-numerical language (e.g., ""try naming a few key terms we used,"" ""explain what changes and why"").
            4) **Ask** ONE focused follow-up question that makes the next step obvious.
          - Keep to **2 sentences + 1 question**. Avoid listing rules or numbers. Do not mention ""criteria,"" ""rubric,"" or internal tags.

          TONE & STYLE
          - Warm, nonjudgmental, growth-mindset (""You're close—let's refine…"").
          - Use the student's phrasing when helpful; avoid jargon in feedback itself.
          - Prefer choices when useful: ""Want to connect this to your soccer training or cooking at home?""
          ";
        }
        return extraContext;
    }

    public IReadOnlyDictionary<string, string> PromptValues(GameSession session) => new Dictionary<string, string>
    {
        ["{chosen_protein}"] = ChosenProtein(session) ?? "",
        ["{PROTEINS_LIST}"] = JsonConvert.SerializeObject(Proteins)
    };

    public void OnResponse(GameSession session, DrafterOutput output, IReadOnlyDictionary<string, bool> goalsMet)
    {
        var proposed = output.Extra != null && output.Extra.TryGetValue(ChosenProteinKey, out var token)
            && token.Type == JTokenType.String ? (string?)token : null;
        if (!string.IsNullOrEmpty(proposed) && Proteins.Contains(proposed!.ToLower()))
            SetChosenProteinValue(session, proposed);

        // Offer the chosen protein's lab as soon as its goal is met.
        var chosen = ChosenProtein(session);
        if (goalsMet.TryGetValue(ProteinGoal, out var met) && met
            && session.CurrentGameState.Id == IntroState
            && !string.IsNullOrEmpty(chosen)
            && chosen!.ToLower() != "none" && chosen.ToLower() != "null")
        {
            var actionId = LabAction(chosen);
            Console.WriteLine($"DEBUG: INJECTING ACTION {actionId}");
            session.ActionSystem.AddAction(session.AllStates, session.CurrentGameState.Id, actionId);
            session.CurrentGameState.UpdateActions(session.ActionSystem);
        }
    }

    public void OnActionTaken(GameSession session, string actionId)
    {
        if (actionId.StartsWith(LabActionPrefix))
            SetChosenProteinValue(session, actionId.Substring(LabActionPrefix.Length).ToLowerInvariant());
    }

    // Failsafe: if the intro is finished but no valid protein was chosen, pick one.
    public void AfterTurn(GameSession session)
    {
        var chosen = ChosenProtein(session);
        if (session.CurrentGameState.Id == IntroState
            && (string.IsNullOrEmpty(chosen) || !Proteins.Contains(chosen!.ToLower()))
            && GoalSystem.AllGoalsMet(session.CurrentGameState)
            && session.CurrentGameState.Available.Count == 0)
        {
            var protein = Proteins[new Random().Next(Proteins.Count)];
            SetChosenProteinValue(session, protein);
            Console.WriteLine($"****FAILSAFE: Auto-selected protein: {protein}");
            session.ActionSystem.AddAction(session.AllStates, session.CurrentGameState.Id, LabAction(protein));
            session.CurrentGameState.UpdateActions(session.ActionSystem);
        }
    }

    public bool IsLabAction(string actionId) => actionId.StartsWith("TO_PROTEIN_SYNTHESIS_LAB");

    public bool IsCompletionAction(string actionId) => actionId == FarewellAction;

    public string? DialogueNodeFor(string? actionId)
    {
        if (actionId == FarewellAction) return "EndGame";
        if (actionId == "TO_PROTEIN_SYNTHESIS_LAB") return "ProteinSynthesisLab";
        if (actionId != null && actionId.StartsWith(LabActionPrefix))
        {
            var suffix = actionId.Substring(LabActionPrefix.Length).ToLowerInvariant();
            if (Proteins.Contains(suffix))
                return "ProteinSynthesisLab" + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
        }
        return null;
    }

    public void SelectLab(GameSession session, GameSession fresh, string actionId)
    {
        if (!actionId.StartsWith(LabActionPrefix) || DialogueNodeFor(actionId) == null)
            throw new ArgumentException("Selected lab is invalid.", nameof(actionId));
        session.AllStates[ReflectionState] = fresh.AllStates[ReflectionState];
        session.CurrentGameState = session.AllStates[ReflectionState];
        SetChosenProteinValue(session, actionId.Substring(LabActionPrefix.Length).ToLowerInvariant());
    }

    // Version 1 session checkpoints stored the protein in a dedicated field.
    public void UpgradeSession(SessionState state)
    {
        if (state.ChosenProtein != null && !state.ActivityValues.ContainsKey(ChosenProteinKey))
            state.ActivityValues[ChosenProteinKey] = state.ChosenProtein;
        state.ChosenProtein = null;
    }

    public void ExportServerFields(SessionState state, JObject game) =>
        game[ChosenProteinKey] = state.ActivityValues.TryGetValue(ChosenProteinKey, out var value) ? value : null;

    public void ImportServerFields(JObject game, SessionState state)
    {
        var value = (string?)game[ChosenProteinKey];
        if (value != null) state.ActivityValues[ChosenProteinKey] = value;
    }
}
