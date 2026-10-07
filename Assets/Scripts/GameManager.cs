using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Activities;
using GameEngine.Dialogue;
using GameEngine.LLM;
using GameEngine.Models;
using GameEngine.Persistence;
using GameEngine.Services;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Yarn.Unity;

// Owns a local tutoring session. All Unity state changes occur on the main thread.
[RequireComponent(typeof(MessageQueueCommands))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameSession Session { get; private set; }
    public IActivity Activity { get; private set; }
    public bool IsInitialized => Session != null;
    public bool IsProcessing => operation != null && !operation.IsCompleted;
    public Checkpoint Saved { get; private set; }
    public string LastError { get; private set; }
    private ClientServices services;
    private AnthropicClient model;
    private CheckpointStore store;
    private MessageQueueCommands presenter;
    private CancellationTokenSource lifetime = new CancellationTokenSource();
    private CancellationTokenSource turnLifetime;
    private CancellationTokenSource speechLifetime;
    private Task operation;
    private Task speechOperation;
    private bool syncing;
    private float nextSync;
    private readonly Dictionary<string, string> data = new Dictionary<string, string>();
    private string storageRoot;
    private string lastInput;
    private GlobalInMemoryVariableStorage Vars => GlobalInMemoryVariableStorage.Instance;
    // Activities this app can run, selected by the Yarn variable $activity.
    private static readonly Dictionary<string, IActivity> Activities = new[] { (IActivity)new ProteinSynthesisActivity() }
        .ToDictionary(activity => activity.Id);
    private const string DefaultActivity = "protein";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        presenter = GetComponent<MessageQueueCommands>();
        storageRoot = Path.Combine(Application.persistentDataPath, "Mosaic");
        store = new CheckpointStore(Path.Combine(storageRoot, "sessions"));
    }

    private void Update()
    {
        if (!syncing && services != null && services.CanSync && Time.unscaledTime >= nextSync)
        {
            nextSync = Time.unscaledTime + 30;
            _ = FlushResearchAsync();
        }
    }

    [YarnCommand("initialize_session")]
    public IEnumerator InitializeSession()
    {
        turnLifetime?.Cancel(); speechLifetime?.Cancel();
        while (IsProcessing) yield return null;
        while (speechOperation != null && !speechOperation.IsCompleted) yield return null;
        presenter.Clear();
        operation = InitializeAsync();
        while (!operation.IsCompleted) yield return null;
        Vars.SetValue("$request_failed", LastError != null);
    }

    private string ReadVariable(string name, string fallback = "") => Vars.TryGetValue(name, out string value) ? value : fallback;

    private async Task InitializeAsync()
    {
        LastError = null;
        try
        {
            var participant = ReadVariable("$participant_id").Trim();
            if (participant.Length == 0) participant = Guid.NewGuid().ToString("N").Substring(0, 8);
            Vars.SetValue("$participant_id", participant);
            var activityId = ReadVariable("$activity", DefaultActivity);
            if (!Activities.TryGetValue(activityId, out var activity))
                throw new InvalidDataException("Unknown activity: " + activityId);
            Activity = activity;
            if (services == null)
            {
#if UNITY_EDITOR
                var configText = await ReadAsset("client-settings.json", lifetime.Token);
                var config = JsonConvert.DeserializeObject<ClientConfiguration>(configText) ?? new ClientConfiguration();
                // Private source configuration is ignored by Git; builds receive a generated copy.
                var local = Path.GetFullPath(Path.Combine(Application.dataPath, "../.local/config.json"));
                if (File.Exists(local)) config.ApplyOverrides(File.ReadAllText(local));
                if (string.IsNullOrWhiteSpace(config.anthropic_api_key)) config.anthropic_api_key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrWhiteSpace(config.openai_api_key)) config.openai_api_key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
#else
                var configText = await ReadAsset("managed-client-settings.json", lifetime.Token);
                var config = JsonConvert.DeserializeObject<ClientConfiguration>(configText) ?? new ClientConfiguration();
#endif
                var deviceSettings = Path.Combine(storageRoot, "client-settings.json");
                if (File.Exists(deviceSettings)) config.ApplyOverrides(File.ReadAllText(deviceSettings));
                services = new ClientServices(config);
                model = new AnthropicClient(services.TutorAsync);
                if (!services.CanSync) Debug.LogWarning("Supabase is not configured; research is saved locally only.");
            }
            await LoadData(Activity, lifetime.Token);
            var checkpoint = store.Load(participant, Activity);
            if (checkpoint == null && services.CanSync)
                checkpoint = await services.LoadCheckpointAsync(participant, Activity, lifetime.Token);
            checkpoint?.Validate(participant);
            var state = checkpoint == null ? null : JsonConvert.DeserializeObject<SessionState>(checkpoint.SessionJson);
            Session = CreateSession(state?.Username ?? ReadVariable("$player_name", "Student"),
                state?.GradeLevel ?? ReadVariable("$grade"), state?.PeerTutor ?? ReadVariable("$peer_tutor", "Yari"),
                ReadVariable("$initial_state", Activity.IntroStateId));
            speechOperation = null;
            Session.ParticipantId = participant;
            if (checkpoint != null) Session.Restore(checkpoint.SessionJson);
            Saved = checkpoint ?? new Checkpoint {
                ParticipantId = participant, ActivityId = Activity.Id, SessionJson = Session.Save(), Condition = ReadVariable("$condition", "treatment")
            };
            if (checkpoint == null) AddEvent("session_started", new { Session.Username, Session.GradeLevel, Session.PeerTutor, Saved.Condition });
            var selectedLab = ReadVariable("$requested_lab_action");
            if (!string.IsNullOrEmpty(selectedLab))
            {
                if (!Activity.IsLabAction(selectedLab) || Activity.DialogueNodeFor(selectedLab) == null)
                    throw new InvalidDataException("Selected lab is invalid.");
                // Explicit lab selection is a separate user entry point, not a model action.
                var fresh = CreateSession(Session.Username, Session.GradeLevel, Session.PeerTutor);
                Activity.SelectLab(Session, fresh, selectedLab);
                Saved.SessionJson = Session.Save(); Saved.Phase = "lab"; Saved.LabAction = selectedLab;
                Saved.Turn = null; Saved.PendingInput = null;
                AddEvent("lab_selected", new { action = selectedLab });
                Save();
                Vars.SetValue("$requested_lab_action", "");
            }
            Vars.SetValue("$player_name", Session.Username);
            Vars.SetValue("$grade", Session.GradeLevel);
            Vars.SetValue("$peer_tutor", Session.PeerTutor);
            Vars.SetValue("$condition", Saved.Condition);
            var destination = "PlayerResponding";
            if (Saved.Phase == "completed") destination = "EndGame";
            else if (Saved.Phase == "lab") destination = "ResumeLab";
            else if (Saved.Turn != null && !Saved.Turn.Finished)
            {
                Saved.Turn.PrepareResume();
                Present(Saved.Turn); destination = "AwaitingTutor";
            }
            else if (Saved.PendingInput != null)
            {
                lastInput = Saved.PendingInput; destination = "RetryPendingTurn";
            }
            else if (checkpoint == null)
            {
                lastInput = "(start conversation)"; destination = "RetryPendingTurn";
            }
            else if (Saved.Turn?.Options.Count > 0) { presenter.SetOptions(Saved.Turn.Options); destination = "GPTResponding"; }
            if (speechOperation == null && Saved.PendingReflection != null)
            {
                speechLifetime?.Dispose();
                speechLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                speechOperation = FinishReflectionAsync(Saved, Session, speechLifetime.Token);
            }
            Vars.SetValue("$resume_node", destination);
            Save();
        }
        catch (Exception e) { Fail(e); }
    }

    private GameSession CreateSession(string username, string grade, string tutor, string initialState = null)
    {
        var session = new GameSession(username, grade, tutor);
        var states = "Activities/" + Activity.Id + "/States/";
        session.InstantiateGame(Activity, id => (data[states + id + "/state.json"], data[states + id + "/examples.txt"]),
            name => data["Characters/" + name + "/character.json"], name => data["Prompts/" + name],
            initialStateId: initialState);
        session.SetAnthropicClient(model);
        return session;
    }

    [YarnCommand("send_player_message_local")]
    public void SendPlayerMessageLocal(string variableName) => Submit(ReadVariable(variableName));

    [YarnCommand("retry_turn")]
    public void RetryTurn() => Submit(lastInput ?? Saved?.PendingInput ?? "(start conversation)");

    public void Submit(string input)
    {
        if (IsProcessing) return;
        lastInput = input;
        operation = RunTurnAsync(input);
    }

    [YarnCommand("await_response")]
    public IEnumerator AwaitResponse()
    {
        while (IsProcessing) yield return null;
        Vars.SetValue("$request_failed", LastError != null);
    }

    private async Task RunTurnAsync(string input)
    {
        LastError = null;
        try
        {
            if (!IsInitialized) throw new InvalidOperationException("Session has not been initialized.");
            Saved.PendingInput = input; Save();
            turnLifetime?.Cancel(); turnLifetime?.Dispose();
            turnLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            // Preserve the previous turn's reflection before cloning its session.
            if (speechOperation != null) await speechOperation;
            turnLifetime.Token.ThrowIfCancellationRequested();
            // Work on a candidate. A failed/cancelled turn never partially advances the saved session.
            var candidate = CreateSession(Session.Username, Session.GradeLevel, Session.PeerTutor);
            candidate.Restore(Session.Save());
            var started = DateTime.UtcNow;
            var response = await candidate.ProcessStepsAsync(input, turnLifetime.Token);
            turnLifetime.Token.ThrowIfCancellationRequested();
            var turn = DialogueTurn.Create(response.Message, candidate.PeerTutor, response.Action);
            if (turn.Lines.Count == 0) throw new InvalidDataException("Tutor returned no dialogue.");
            var prior = Saved;
            var committed = Saved.Snapshot();
            committed.SessionJson = candidate.Save(); committed.Turn = turn; committed.PendingInput = null;
            committed.PendingReflection = new PendingReflection { TurnId = turn.Id, Critique = response.ReflectionCritique };
            Saved = committed;
            AddEvent("turn", new {
                step = candidate.Step, current_state = candidate.CurrentGameState.Id,
                user_message = input, agent_message = response.Message, goals_met = response.GoalsMet,
                action_called = response.Action, student_interest = candidate.StudentInterest,
                agent_response_time = (DateTime.UtcNow - started).TotalSeconds, condition = Saved.Condition
            });
            try { Save(); }
            catch { Saved = prior; throw; }
            Session = candidate;
            Present(turn);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested || turnLifetime?.IsCancellationRequested == true) { }
        catch (Exception e)
        {
            Fail(e);
            if (Saved != null) { AddEvent("turn_failed", new { error_type = e.GetType().Name }); TrySave(); }
        }
    }

    private void Present(DialogueTurn turn)
    {
        speechLifetime?.Cancel(); speechLifetime?.Dispose();
        speechLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        presenter.Present(turn);
        speechOperation = PrepareSpeechAsync(turn, speechLifetime.Token);
    }

    private async Task PrepareSpeechAsync(DialogueTurn turn, CancellationToken token)
    {
        var checkpoint = Saved;
        var session = Session;
        try
        {
            await TurnPreparation.RunAsync(turn, (line, ct) => GenerateLineSpeechAsync(turn, line, ct),
                ct => FinishReflectionAsync(checkpoint, session, ct), token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception e)
        {
            Debug.LogWarning("Speech preparation failed: " + e.GetType().Name);
            if (Saved?.Turn != turn || token.IsCancellationRequested) return;
            // An unexpected decoder/presenter failure must not leave pending lines stuck forever.
            foreach (var line in turn.Lines.Where(line => line.Speech == SpeechState.Pending))
                presenter.CompleteSpeech(turn, line, null);
            await FinishReflectionAsync(checkpoint, session, token);
        }
    }

    private async Task GenerateLineSpeechAsync(DialogueTurn turn, DialogueLine line, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!services.SpeechEnabled) { line.Speech = SpeechState.Skipped; return; }
        AudioClip clip = null;
        for (int attempt = 0; attempt < 2 && clip == null; attempt++)
        {
            try
            {
                var bytes = await services.SpeechAsync(line.Text, line.Speaker, token);
                token.ThrowIfCancellationRequested();
                if (Saved?.Turn != turn) return;
                clip = WavUtility.ToAudioClip(bytes, line.Id);
                if (clip == null) throw new InvalidDataException("Speech could not be decoded.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception e)
            {
                if (Saved?.Turn != turn) return;
                if (attempt == 0) await Task.Delay(1000, token);
                else
                {
                    Debug.LogWarning($"Speech failed for line {line.Id}: {e.Message}");
                    AddEvent("speech_failed", new { turn_id = turn.Id, line_id = line.Id, error_type = e.GetType().Name }); TrySave();
                }
            }
        }
        if (Saved?.Turn != turn || token.IsCancellationRequested) { if (clip != null) Destroy(clip); return; }
        presenter.CompleteSpeech(turn, line, clip); // Failure is terminal and permits text-only continuation.
    }

    private async Task FinishReflectionAsync(Checkpoint checkpoint, GameSession session, CancellationToken token)
    {
        var pending = checkpoint.PendingReflection;
        if (pending == null || Saved != checkpoint || Session != session || token.IsCancellationRequested) return;
        try
        {
            var result = await session.ReflectOnTurnAsync(pending.Critique, token);
            token.ThrowIfCancellationRequested();
            if (Saved != checkpoint || Session != session) return;
            AddEvent("reflection", new { turn_id = pending.TurnId, reflection = result.Reflection, error_type = result.ReflectionError });
            if (result.ReflectionError != null) Debug.LogWarning("Reflection failed: " + result.ReflectionError);
            checkpoint.PendingReflection = null;
            checkpoint.SessionJson = session.Save();
            Save();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { /* Saved pending work resumes next time. */ }
        catch (Exception e) { Debug.LogWarning("Reflection/save failed: " + e.Message); }
    }

    public void SaveProgress() => TrySave();

    public void BeginAction(string action)
    {
        if (Saved.Turn == null || Saved.Turn.Action != action) throw new InvalidOperationException("Action does not belong to this turn.");
        if (Activity.IsLabAction(action)) { Saved.Phase = "lab"; Saved.LabAction = action; }
        else if (Activity.IsCompletionAction(action)) Saved.Phase = "completed";
        AddEvent("action", new { action }); Save();
    }

    [YarnCommand("resume_lab")]
    public void ResumeLab()
    {
        var node = Activity.DialogueNodeFor(Saved.LabAction);
        if (node == null) throw new InvalidDataException("Saved lab action is invalid.");
        Vars.SetValue("$actionNode", node);
        GlobalDialogueManager.triggered = false;
    }

    [YarnCommand("complete_lab")]
    public void CompleteLab()
    {
        if (!IsInitialized) { LastError = "Start a tutoring session before completing the lab."; return; }
        Saved.Phase = "conversation"; Saved.LabAction = null;
        lastInput = Activity.LabCompletedMessage;
        Saved.PendingInput = lastInput;
        AddEvent("lab_completed", new { activity = Activity.Id, activity_values = Session.ActivityValues }); Save();
        Submit(lastInput);
    }

    private void AddEvent(string kind, object payload)
    {
        Saved.Outbox.Add(new ResearchEvent { session_id = Saved.SessionId, participant_id = Saved.ParticipantId, kind = kind, payload = payload });
        nextSync = 0;
    }
    private void Save() { if (Saved != null) store.Save(Saved, Activity); }
    private void TrySave() { try { Save(); } catch (Exception e) { Debug.LogError("Session save failed: " + e.GetType().Name); } }
    private void Fail(Exception e) { LastError = e.GetType().Name; Debug.LogError("Tutoring request failed: " + e.GetType().Name + ": " + e.Message); }

    private async Task FlushResearchAsync()
    {
        syncing = true;
        try
        {
            // Each activity saves in its own subfolder; legacy saves sit at the root.
            foreach (var path in Directory.GetFiles(Path.Combine(storageRoot, "sessions"), "*.json", SearchOption.AllDirectories))
            {
                var disk = JsonConvert.DeserializeObject<Checkpoint>(File.ReadAllText(path));
                if (disk == null || !Activities.TryGetValue(disk.ActivityId ?? DefaultActivity, out var activity)) continue;
                var checkpoint = Saved != null && disk.SessionId == Saved.SessionId ? Saved : disk;
                var count = await ResearchSync.FlushAsync(services, activity,
                    () => Saved != null && checkpoint.SessionId == Saved.SessionId ? Saved : checkpoint,
                    latest => store.Save(latest, activity), lifetime.Token);
                if (count > 0) Debug.Log($"Research synced: {count} response(s) to responses; saved game uploaded to games.");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { Debug.LogWarning("Research sync deferred; saved locally. " + e.Message); }
        finally { syncing = false; }
    }

    private async Task LoadData(IActivity activity, CancellationToken token)
    {
        var paths = new List<string>();
        foreach (var id in activity.StateIds)
            foreach (var file in new[] { "state.json", "examples.txt" }) paths.Add("Activities/" + activity.Id + "/States/" + id + "/" + file);
        foreach (var name in new[] { "alex", "benji", "isaiah", "jessica", "maya", "yari" }) paths.Add("Characters/" + name + "/character.json");
        foreach (var name in new[] { "INITIAL_PROMPT.txt", "EVAL_BASE.txt", "CRITERIA_FULL.txt", "CRITERIA_RESP_ONLY.txt", "REFLECTION_STRICT.txt", "REFLECTION_LENIENT.txt" }) paths.Add("Prompts/" + name);
        paths.RemoveAll(data.ContainsKey);
        var loaded = new Dictionary<string, string>();
        foreach (var path in paths) loaded[path] = await ReadAsset("GameData/" + path, token);
        foreach (var pair in loaded) data[pair.Key] = pair.Value;
    }

    private static async Task<string> ReadAsset(string relative, CancellationToken token)
    {
        var path = Application.streamingAssetsPath.TrimEnd('/') + "/" + relative;
        var url = path.Contains("://") ? path : new Uri(path).AbsoluteUri;
        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        var operation = request.SendWebRequest();
        while (!operation.isDone) { if (token.IsCancellationRequested) { request.Abort(); token.ThrowIfCancellationRequested(); } await Task.Yield(); }
        if (request.result != UnityWebRequest.Result.Success) throw new IOException("Unable to load " + relative);
        return request.downloadHandler.text;
    }

    private void OnApplicationPause(bool paused) { if (paused) TrySave(); }
    private void OnDestroy()
    {
        lifetime.Cancel(); turnLifetime?.Cancel(); speechLifetime?.Cancel(); TrySave();
        model?.Dispose(); services?.Dispose();
        if (Instance == this) Instance = null;
    }
}
