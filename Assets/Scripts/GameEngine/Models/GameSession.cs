using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Activities;
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
    /// <summary>Activity-owned values (e.g. the chosen protein), saved with the session.</summary>
    public Dictionary<string, string> ActivityValues { get; set; } = new();
    public List<string> LongTermReflections { get; set; } = new();

    // Settings
    public string ParameterSetting { get; set; } = "strict";

    // Systems (injected)
    [JsonIgnore]
    public ActionSystem ActionSystem { get; private set; } = null!;
    [JsonIgnore]
    public IActivity Activity { get; private set; } = null!;
    private StateLoader _stateLoader = null!;
    private PromptBuilder? _promptBuilder;
    private ITutorModel? _anthropicClient;

    public GameSession(string username, string gradeLevel = "", string peerTutor = "")
    {
        Username = username;
        GradeLevel = gradeLevel;
        PeerTutor = string.IsNullOrEmpty(peerTutor) ? "Yari" : peerTutor;
        ParticipantId = Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// Initialize the game with the activity's states and the tutor persona.
    /// Port of Game.instantiate_game() from testing_8.py:314-351.
    /// </summary>
    /// <param name="activity">Lesson-specific behavior and state list</param>
    /// <param name="stateDataProvider">Function that provides (stateJson, examplesText) for a given stateId</param>
    /// <param name="characterDataProvider">Function that provides character.json content for a given character name</param>
    /// <param name="promptDataProvider">Function that provides prompt template content for a given filename</param>
    /// <param name="parameterSetting">"strict" or "lenient"</param>
    /// <param name="initialStateId">Override initial state (default: the activity's intro state)</param>
    public void InstantiateGame(
        IActivity activity,
        Func<string, (string json, string examples)> stateDataProvider,
        Func<string, string> characterDataProvider,
        Func<string, string> promptDataProvider,
        string parameterSetting = "strict",
        string? initialStateId = null)
    {
        ParameterSetting = parameterSetting;
        Activity = activity;

        // Create systems
        ActionSystem = new ActionSystem(activity);
        _stateLoader = new StateLoader(ActionSystem);

        // Load persona description
        PersonaDescription = LoadCharacterDescription(characterDataProvider, PeerTutor);

        // Load states
        AllStates = new Dictionary<string, GameState>();
        foreach (var stateId in activity.StateIds)
        {
            var (json, examples) = stateDataProvider(stateId);
            var state = _stateLoader.LoadState(json, examples, Username, GradeLevel, PeerTutor);
            AllStates[state.Id] = state;
        }
        activity.PrepareStates(AllStates);

        CurrentGameState = initialStateId != null && AllStates.ContainsKey(initialStateId)
            ? AllStates[initialStateId]
            : AllStates[activity.IntroStateId];

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
    public void SetAnthropicClient(ITutorModel client)
    {
        _anthropicClient = client;
    }

    /// <summary>
    /// Process a student input turn: call LLM, update goals, execute actions.
    /// Port of Game.process_steps() from testing_8.py:611-714.
    /// </summary>
    public async Task<GameResponse> ProcessStepsAsync(string userInput, CancellationToken cancellationToken = default)
    {
        // Step 1: Store user message
        AddMessage("user", $"{Username}:: {userInput}");

        // Step 2: Prepare context
        var extraContext = PrepareExtraContext();
        var unmetGoals = GoalSystem.GetUnmetGoals(CurrentGameState);

        Console.WriteLine($"--- UNMET GOALS: {JsonConvert.SerializeObject(unmetGoals)} ---");

        // Step 3: Build system prompt and call LLM
        DrafterOutput drafterOutput;
        if (_anthropicClient != null && _promptBuilder != null)
        {
            var evalCondition = EvalContextUtil.DetermineEvalContext(
                unmetGoals, CurrentGameState.Available);

            var systemPrompt = _promptBuilder.BuildSystemPrompt(
                PeerTutor, PersonaDescription,
                Username, GradeLevel, StudentInterest,
                Messages, userInput,
                CurrentGameState.Description,
                unmetGoals, CurrentGameState.Available,
                StudentConceptLanguage,
                ParameterSetting, LongTermReflections,
                extraContext, evalCondition,
                Activity.AdvancedConcepts, Activity.FoundationalConcepts, Activity.PromptValues(this));

            drafterOutput = await _anthropicClient.SendMessageAsync(systemPrompt, Activity.ResponseFields, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                "No LLM client configured. Call SetAnthropicClient() or use ProcessStepsWithMock().");
        }

        var response = ProcessDrafterOutput(drafterOutput, userInput);
        cancellationToken.ThrowIfCancellationRequested();
        response.ReflectionCritique = drafterOutput.SummaryCritique ?? "";
        return response;
    }

    // Called only after the turn's audio preparation settles. The controller waits
    // for this work before drafting again, so the next prompt includes its memory.
    public async Task<GameResponse> ReflectOnTurnAsync(string critique, CancellationToken cancellationToken = default)
    {
        var response = new GameResponse();
        // Reflection informs future turns; it never rewrites this response.
        try
        {
            if (_anthropicClient == null) throw new InvalidOperationException("No LLM client configured.");
            var reflection = await _anthropicClient.ReflectAsync(PeerTutor, Messages,
                critique, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(reflection))
            {
                response.Reflection = reflection;
                LongTermReflections.Add(reflection);
                while (LongTermReflections.Count > 3) LongTermReflections.RemoveAt(0);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception e) { response.ReflectionError = e.GetType().Name; }
        return response;
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
        if (string.IsNullOrWhiteSpace(output.Message))
            throw new InvalidOperationException("The tutor returned an empty response.");
        var actionCalled = output.Action;

        // Set student interest
        SetStudentInterest(output.StudentInterest);

        // Format response
        var formattedResponse = $"{PeerTutor}:: {teacherResponse}";

        // Build goals_met from chosen goal
        var goalsMet = !string.IsNullOrEmpty(chosenGoalForTurn)
            ? new Dictionary<string, bool> { [chosenGoalForTurn] = true }
            : new Dictionary<string, bool>();

        // Activity-specific handling (e.g. offer the chosen protein's lab)
        Activity.OnResponse(this, output, goalsMet);

        // Mark goals as met
        GoalSystem.MarkGoalsMet(CurrentGameState, goalsMet);
        CurrentGameState.UpdateActions(ActionSystem);

        // Merge phrase updates
        ConceptData.MergePhraseUpdates(StudentConceptLanguage, output.PendingPhraseUpdates);

        // Store assistant message
        AddMessage("assistant", formattedResponse);

        // Never expose an action to the UI unless it is currently allowed.
        GoalSystem.UnlockGoals(CurrentGameState);
        CurrentGameState.UpdateActions(ActionSystem);
        if (!string.IsNullOrEmpty(actionCalled)
            && (!ActionSystem.CheckActionValid(actionCalled, CurrentGameState)
                || !ActionSystem.CheckActionAvailable(actionCalled, CurrentGameState)))
            actionCalled = null;

        // Execute only accepted actions.
        if (!string.IsNullOrEmpty(actionCalled))
        {
            Activity.OnActionTaken(this, actionCalled);
            Console.WriteLine($">> Executing action: {actionCalled}");
            var (newState, addedResponse) = ActionSystem.TakeAction(
                actionCalled, CurrentGameState, AllStates, Messages);
            CurrentGameState = newState;
        }

        // Unlock new goals & update actions
        GoalSystem.UnlockGoals(CurrentGameState);
        CurrentGameState.UpdateActions(ActionSystem);

        // Activity end-of-turn hook (e.g. failsafe protein selection)
        Activity.AfterTurn(this);

        if (GoalSystem.AllGoalsMet(CurrentGameState))
            Console.WriteLine("\nDEBUG: ALL GOALS MET");

        Step++;

        Console.WriteLine($"GOALS MET: {JsonConvert.SerializeObject(goalsMet)}");

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
            if (Activity.AdvancedConcepts.Contains(conceptName))
            {
                extraContext += $@"
            SPECIAL INSTRUCTION:
            If you choose to focus on the unmet goal ""{introduceGoal}"", you must follow these critical rules when {PeerTutor} explains the concept:
            - **CRITICAL RULE:** Do NOT use any of the following scientific terms in your response: {JsonConvert.SerializeObject(Activity.AdvancedConcepts)}.
            - Instead, you MUST explain the concept exclusively using simple, everyday language, drawing from the words used by the student: {JsonConvert.SerializeObject(StudentConceptLanguage)}. You will introduce the formal scientific term when pursuing a subsequent goal, but NOT now.
            ";
            }
        }
        else if (connectGoal != null)
        {
            var conceptName = connectGoal.Replace($"{PeerTutor} connects ", "");
            if (Activity.AdvancedConcepts.Contains(conceptName))
            {
                extraContext += $@"
              SPECIAL INSTRUCTION:
              If you choose to focus on the unmet goal ""{connectGoal}"", you must clearly link the concept to the student's interest OR cultural background using accessible language BEFORE introducing the student the formal scientific vocabulary for the FIRST time: {conceptName}.
              ";
            }
        }

        // Activity-specific instructions (e.g. protein choice, reflection scaffolding)
        extraContext += Activity.BuildExtraContext(this, unmetGoals);

        return extraContext;
    }

    private void SetStudentInterest(string? newInterest)
    {
        if (!string.IsNullOrEmpty(newInterest)
            && newInterest != "NONE" && newInterest != "null")
        {
            StudentInterest = newInterest;
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

    public string Save()
    {
        return JsonConvert.SerializeObject(new SessionState {
            Username = Username, GradeLevel = GradeLevel, PeerTutor = PeerTutor,
            PersonaDescription = PersonaDescription,
            ParticipantId = ParticipantId, Step = Step, CurrentStateId = CurrentGameState.Id,
            AllStates = AllStates, Messages = Messages, StudentConceptLanguage = StudentConceptLanguage,
            StudentInterest = StudentInterest, ActivityValues = ActivityValues,
            Reflections = LongTermReflections, ParameterSetting = ParameterSetting
        });
    }

    public void Restore(string json)
    {
        var state = JsonConvert.DeserializeObject<SessionState>(json)
            ?? throw new InvalidOperationException("Invalid session checkpoint.");
        Activity.UpgradeSession(state);
        if (state.Version != 1 || !state.AllStates.ContainsKey(state.CurrentStateId))
            throw new InvalidOperationException("Unsupported session checkpoint.");
        if (state.PeerTutor != PeerTutor)
            throw new InvalidOperationException("Checkpoint tutor does not match loaded persona.");
        Username = state.Username; GradeLevel = state.GradeLevel; ParticipantId = state.ParticipantId;
        if (!string.IsNullOrEmpty(state.PersonaDescription)) PersonaDescription = state.PersonaDescription;
        Step = state.Step; AllStates = state.AllStates; CurrentGameState = AllStates[state.CurrentStateId];
        Messages = state.Messages; StudentConceptLanguage = state.StudentConceptLanguage;
        StudentInterest = state.StudentInterest; ActivityValues = state.ActivityValues;
        LongTermReflections = state.Reflections; ParameterSetting = state.ParameterSetting;
        foreach (var item in AllStates.Values) item.UpdateActions(ActionSystem);
    }

    private string LoadCharacterDescription(Func<string, string> characterDataProvider, string peerTutor)
    {
        try
        {
            var json = characterDataProvider(peerTutor.ToLower());
            var character = JObject.Parse(json);
            return character.TryGetValue("description", out var description)
                ? (string?)description ?? ""
                : $"{peerTutor} is a friendly peer tutor.";
        }
        catch
        {
            return $"{peerTutor} is a friendly peer tutor.";
        }
    }
}
