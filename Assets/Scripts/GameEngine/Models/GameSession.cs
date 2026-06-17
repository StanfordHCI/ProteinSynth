using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Data;
using GameEngine.LLM;
using GameEngine.Systems;

namespace GameEngine.Models;

/// <summary>
/// Main game session orchestrator.
/// Port of Game class from testing_8.py.
/// </summary>
public class GameSession
{
    // Player info
    public string Username { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string PeerTutor { get; set; } = "";
    public string PersonaDescription { get; set; } = "";
    public string ParticipantId { get; set; } = "";
    public int Step { get; set; }

    // Conversation
    public List<Dictionary<string, string>> Messages { get; set; } = new();

    // State machine
    public GameState CurrentGameState { get; set; } = null!;
    public Dictionary<string, GameState> AllStates { get; set; } = new();

    // Student tracking
    public Dictionary<string, List<ConceptData.PhraseEntry>> StudentConceptLanguage { get; set; } = new();
    public string? StudentInterest { get; set; }
    public string? ChosenProtein { get; set; }
    public List<string> LongTermReflections { get; set; } = new();

    // Settings
    public string ParameterSetting { get; set; } = "strict";
    public bool NoReviser { get; set; } = true;

    // Systems (injected)
    public ActionSystem ActionSystem { get; private set; } = null!;
    private StateLoader _stateLoader = null!;
    private PromptBuilder? _promptBuilder;
    private AnthropicClient? _anthropicClient;

    public GameSession(string username, string gradeLevel = "", string peerTutor = "")
    {
        Username = username;
        GradeLevel = gradeLevel;
        PeerTutor = string.IsNullOrEmpty(peerTutor) ? "Yari" : peerTutor;
        ParticipantId = Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// Initialize the game with states, persona, and protein list.
    /// Port of Game.instantiate_game() from testing_8.py:314-351.
    /// </summary>
    /// <param name="stateDataProvider">Function that provides (stateJson, examplesText) for a given stateId</param>
    /// <param name="characterDataProvider">Function that provides character.json content for a given character name</param>
    /// <param name="promptDataProvider">Function that provides prompt template content for a given filename</param>
    /// <param name="parameterSetting">"strict" or "lenient"</param>
    /// <param name="noReviser">Whether to skip the revision step</param>
    /// <param name="initialStateId">Override initial state (default: "0_intro_proteinSynthesis")</param>
    public void InstantiateGame(
        Func<string, (string json, string examples)> stateDataProvider,
        Func<string, string> characterDataProvider,
        Func<string, string> promptDataProvider,
        string parameterSetting = "strict",
        bool noReviser = true,
        string? initialStateId = null)
    {
        ParameterSetting = parameterSetting;
        NoReviser = noReviser;

        // Create systems
        ActionSystem = new ActionSystem();
        _stateLoader = new StateLoader(ActionSystem);

        // Load persona description
        PersonaDescription = LoadCharacterDescription(characterDataProvider, PeerTutor);

        // Load states
        var (introJson, introExamples) = stateDataProvider("0_intro_proteinSynthesis");
        var introState = _stateLoader.LoadState(introJson, introExamples, Username, GradeLevel, PeerTutor);

        // Update protein selection goal in intro state
        StateLoader.UpdateProteinSelectionGoal(introState);

        var (labJson, labExamples) = stateDataProvider("2_lab_reflection");
        var labReflectionState = _stateLoader.LoadState(labJson, labExamples, Username, GradeLevel, PeerTutor);

        AllStates = new Dictionary<string, GameState>
        {
            [introState.Id] = introState,
            [labReflectionState.Id] = labReflectionState
        };

        CurrentGameState = initialStateId != null && AllStates.ContainsKey(initialStateId)
            ? AllStates[initialStateId]
            : introState;

        // Build the prompt builder
        _promptBuilder = new PromptBuilder(
            promptDataProvider("INITIAL_PROMPT.txt"),
            promptDataProvider("EVAL_BASE.txt"),
            promptDataProvider("CRITERIA_FULL.txt"),
            promptDataProvider("CRITERIA_RESP_ONLY.txt"),
            promptDataProvider("REFLECTION_STRICT.txt"),
            promptDataProvider("REFLECTION_LENIENT.txt"));
    }

    /// <summary>
    /// Set the Anthropic API client for LLM calls.
    /// </summary>
    public void SetAnthropicClient(AnthropicClient client)
    {
        _anthropicClient = client;
    }

    /// <summary>
    /// Process a student input turn: call LLM, update goals, execute actions.
    /// Port of Game.process_steps() from testing_8.py:611-714.
    /// </summary>
    public async Task<GameResponse> ProcessStepsAsync(string userInput, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Store user message
            AddMessage("user", $"{Username}:: {userInput}");

            // Step 2: Prepare context
            var extraContext = PrepareExtraContext();
            var unmetGoals = GoalSystem.GetUnmetGoals(CurrentGameState);

            Console.WriteLine($"--- UNMET GOALS: {JsonSerializer.Serialize(unmetGoals)} ---");

            // Step 3: Build system prompt and call LLM
            DrafterOutput drafterOutput;
            if (_anthropicClient != null && _promptBuilder != null)
            {
                var evalCondition = EvalContextUtil.DetermineEvalContext(
                    unmetGoals, CurrentGameState.Available);

                var systemPrompt = _promptBuilder.BuildSystemPrompt(
                    PeerTutor, PersonaDescription,
                    Username, GradeLevel, StudentInterest, ChosenProtein,
                    Messages, userInput,
                    CurrentGameState.Description,
                    unmetGoals, CurrentGameState.Available,
                    StudentConceptLanguage,
                    ParameterSetting, LongTermReflections,
                    extraContext, evalCondition);

                drafterOutput = await _anthropicClient.SendMessageAsync(systemPrompt, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException(
                    "No LLM client configured. Call SetAnthropicClient() or use ProcessStepsWithMockAsync().");
            }

            return ProcessDrafterOutput(drafterOutput, userInput);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception in ProcessSteps: {e}");
            var errorResponse = new GameResponse
            {
                Message = $"{PeerTutor}:: I'm sorry, something went wrong. Could you try again?",
                GoalsMet = null,
                Action = null,
                StudentInterest = null
            };
            return errorResponse;
        }
    }

    /// <summary>
    /// Process a turn with a pre-built DrafterOutput (for mock/testing).
    /// </summary>
    public GameResponse ProcessStepsWithMock(string userInput, DrafterOutput mockOutput)
    {
        AddMessage("user", $"{Username}:: {userInput}");
        return ProcessDrafterOutput(mockOutput, userInput);
    }

    /// <summary>
    /// Core output processing: update goals, execute actions, unlock goals.
    /// Port of Game.process_graph_output() from testing_8.py:500-552.
    /// </summary>
    private GameResponse ProcessDrafterOutput(DrafterOutput output, string userInput)
    {
        var teacherResponse = output.Message;
        var chosenGoalForTurn = output.ChosenGoalForTurn;
        var actionCalled = output.Action;

        // Set student interest
        SetStudentInterest(output.StudentInterest);
        SetChosenProtein(output.ChosenProtein);

        // Format response
        var formattedResponse = $"{PeerTutor}:: {teacherResponse}";

        // Build goals_met from chosen goal
        var goalsMet = !string.IsNullOrEmpty(chosenGoalForTurn)
            ? new Dictionary<string, bool> { [chosenGoalForTurn] = true }
            : new Dictionary<string, bool>();

        // Inject protein action if protein goal was just met
        var proteinsListStr = $"['{string.Join("', '", ProteinData.ProteinsList)}']";
        var introduceProteinGoal = $"Introduce ONE of the following valid list of proteins based on the student's interest: {proteinsListStr}";

        if (goalsMet.TryGetValue(introduceProteinGoal, out var met) && met
            && CurrentGameState.Id == "0_intro_proteinSynthesis"
            && !string.IsNullOrEmpty(ChosenProtein)
            && ChosenProtein.ToLower() != "none" && ChosenProtein.ToLower() != "null")
        {
            var actionId = $"TO_PROTEIN_SYNTHESIS_LAB_{ChosenProtein.ToUpper()}";
            Console.WriteLine($"DEBUG: INJECTING ACTION {actionId}");
            ActionSystem.AddAction(AllStates, CurrentGameState.Id, actionId);
            CurrentGameState.UpdateActions(ActionSystem);
        }

        // Mark goals as met
        GoalSystem.MarkGoalsMet(CurrentGameState, goalsMet);
        CurrentGameState.UpdateActions(ActionSystem);

        // Merge phrase updates
        ConceptData.MergePhraseUpdates(StudentConceptLanguage, output.PendingPhraseUpdates);

        // Store assistant message
        AddMessage("assistant", formattedResponse);

        // Execute action if called
        if (!string.IsNullOrEmpty(actionCalled))
        {
            Console.WriteLine($">> Executing action: {actionCalled}");
            var (newState, addedResponse) = ActionSystem.TakeAction(
                actionCalled, CurrentGameState, AllStates, Messages);
            CurrentGameState = newState;
        }

        // Unlock new goals & update actions
        GoalSystem.UnlockGoals(CurrentGameState);
        CurrentGameState.UpdateActions(ActionSystem);

        // Failsafe protein selection
        if (CurrentGameState.Id == "0_intro_proteinSynthesis"
            && (string.IsNullOrEmpty(ChosenProtein) || !ProteinData.ProteinsList.Contains(ChosenProtein.ToLower()))
            && GoalSystem.AllGoalsMet(CurrentGameState)
            && CurrentGameState.Available.Count == 0)
        {
            var random = new Random();
            ChosenProtein = ProteinData.ProteinsList[random.Next(ProteinData.ProteinsList.Count)];
            Console.WriteLine($"****FAILSAFE: Auto-selected protein: {ChosenProtein}");
            var failsafeActionId = $"TO_PROTEIN_SYNTHESIS_LAB_{ChosenProtein.ToUpper()}";
            ActionSystem.AddAction(AllStates, CurrentGameState.Id, failsafeActionId);
            CurrentGameState.UpdateActions(ActionSystem);
        }

        if (GoalSystem.AllGoalsMet(CurrentGameState))
            Console.WriteLine("\nDEBUG: ALL GOALS MET");

        Step++;

        Console.WriteLine($"GOALS MET: {JsonSerializer.Serialize(goalsMet)}");

        return new GameResponse
        {
            Message = formattedResponse,
            GoalsMet = goalsMet,
            Action = actionCalled,
            StudentInterest = StudentInterest
        };
    }

    /// <summary>
    /// Prepare extra context for the prompt based on current goals.
    /// Port of Game.prepare_extra_context() from testing_8.py:354-418.
    /// </summary>
    public string PrepareExtraContext()
    {
        var unmetGoals = GoalSystem.GetUnmetGoals(CurrentGameState);
        var extraContext = "";

        // Check for introduce goal with advanced concept
        var introduceGoal = unmetGoals.FirstOrDefault(g => g.StartsWith($"{PeerTutor} introduces "));
        var connectGoal = unmetGoals.FirstOrDefault(g => g.StartsWith($"{PeerTutor} connects "));

        if (introduceGoal != null)
        {
            var conceptName = introduceGoal.Replace($"{PeerTutor} introduces ", "");
            if (ConceptData.AdvancedConcepts.Contains(conceptName))
            {
                extraContext += $@"
            SPECIAL INSTRUCTION:
            If you choose to focus on the unmet goal ""{introduceGoal}"", you must follow these critical rules when {PeerTutor} explains the concept:
            - **CRITICAL RULE:** Do NOT use any of the following scientific terms in your response: {JsonSerializer.Serialize(ConceptData.AdvancedConcepts)}.
            - Instead, you MUST explain the concept exclusively using simple, everyday language, drawing from the words used by the student: {JsonSerializer.Serialize(StudentConceptLanguage)}. You will introduce the formal scientific term when pursuing a subsequent goal, but NOT now.
            ";
            }
        }
        else if (connectGoal != null)
        {
            var conceptName = connectGoal.Replace($"{PeerTutor} connects ", "");
            if (ConceptData.AdvancedConcepts.Contains(conceptName))
            {
                extraContext += $@"
              SPECIAL INSTRUCTION:
              If you choose to focus on the unmet goal ""{connectGoal}"", you must clearly link the concept to the student's interest OR cultural background using accessible language BEFORE introducing the student the formal scientific vocabulary for the FIRST time: {conceptName}.
              ";
            }
        }

        // Protein choosing goal
        var proteinsListStr = $"['{string.Join("', '", ProteinData.ProteinsList)}']";
        var proteinChooseGoal = $"Introduce ONE of the following valid list of proteins based on the student's interest: {proteinsListStr}";
        if (CurrentGameState.Id == "0_intro_proteinSynthesis" && unmetGoals.Contains(proteinChooseGoal))
        {
            extraContext += "Choose one protein to introduce the student to and populate the `chosen_protein` field of the JSON you are returning with this protein. The value should be a string.";
        }

        // Lab reflection scaffolding
        if (CurrentGameState.Id == "2_lab_reflection")
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

    /// <summary>
    /// Generate the intro message for a new game.
    /// Port of make_intro_message() from testing_8.py:170-175.
    /// </summary>
    public string MakeIntroMessage()
    {
        return $"{PeerTutor}:: Hi there, {Username}! My name's {PeerTutor}, and I'll be your peer tutor today—so glad we get to hang out! Your body is like the busiest city you've ever seen, working 24/7 behind the scenes. Even right now, it's making new skin, growing hair, building muscles—without you ever telling it to. Isn't that so cool?";
    }

    private void SetStudentInterest(string? newInterest)
    {
        if (!string.IsNullOrEmpty(newInterest)
            && newInterest != "NONE" && newInterest != "null")
        {
            StudentInterest = newInterest;
        }
    }

    private void SetChosenProtein(string? protein)
    {
        if (!string.IsNullOrEmpty(protein)
            && ProteinData.ProteinsList.Contains(protein.ToLower()))
        {
            ChosenProtein = protein;
        }
    }

    private void AddMessage(string role, string content)
    {
        Messages.Add(new Dictionary<string, string>
        {
            ["role"] = role,
            ["content"] = content
        });
    }

    private string LoadCharacterDescription(Func<string, string> characterDataProvider, string peerTutor)
    {
        try
        {
            var json = characterDataProvider(peerTutor.ToLower());
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("description").GetString() ?? "";
        }
        catch
        {
            return $"{peerTutor} is a friendly peer tutor.";
        }
    }
}
