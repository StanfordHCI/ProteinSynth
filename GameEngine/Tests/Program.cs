using GameEngine.Models;
using GameEngine.LLM;
using GameEngine.Dialogue;
using GameEngine.Persistence;
using GameEngine.Services;
using System.Net;
using System.Text;

var count = 0;
void Check(bool condition, string description) { if (!condition) throw new Exception(description); count++; }
var root = Path.Combine(AppContext.BaseDirectory, "Data");
GameSession Create(ITutorModel? model = null)
{
    var game = new GameSession("Student", "9th", "Jessica");
    game.InstantiateGame(id => (File.ReadAllText(Path.Combine(root, "States", id, "state.json")),
        File.ReadAllText(Path.Combine(root, "States", id, "examples.txt"))),
        name => File.ReadAllText(Path.Combine(root, "Characters", name, "character.json")),
        name => File.ReadAllText(Path.Combine(root, "Prompts", name)));
    if (model != null) game.SetAnthropicClient(model);
    return game;
}
var game = Create();
var early = game.ProcessStepsWithMock("hello", new DrafterOutput { Message = "Hello.", Action = "TO_PROTEIN_SYNTHESIS_LAB" });
Check(early.Action == null, "Unavailable action must not be sent to the UI");
Check(game.CurrentGameState.Id == "0_intro_proteinSynthesis", "Early action must not change state");
var unknown = game.ProcessStepsWithMock("hello", new DrafterOutput { Message = "Hello.", Action = "UNKNOWN" });
Check(unknown.Action == null, "Unknown action must be rejected");
// Goal prerequisites are enforced even for an action that is registered in this state.
var lab = game.AllStates["2_lab_reflection"];
var (unchanged, _) = game.ActionSystem.TakeAction("ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL", lab, game.AllStates, game.Messages);
Check(lab.Actions.ContainsKey("ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL"), "Locked farewell must not execute side effects");
foreach (var goal in lab.Goals.Keys.ToArray()) lab.Goals[goal] = true;
lab.UnlockableGoals.Clear();
var (finished, _) = game.ActionSystem.TakeAction("ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL", lab, game.AllStates, game.Messages);
Check(!lab.Actions.ContainsKey("ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL"), "Unlocked farewell must execute");

var turn = DialogueTurn.Create("Jessica:: First sentence\nSecond sentence. Third!\n-> Yes\n-> No", "Jessica", null);
Check(turn.Lines.Count == 3 && turn.Options.Count == 2, "Text and audio use one segmentation excluding options");
Check(turn.Lines.Select(x => x.Id).Distinct().Count() == 3, "Line identifiers must be unique");
Check(!turn.Lines[0].CanPresent, "Pending audio must block advancement");
turn.Lines[0].Speech = SpeechState.Failed;
Check(turn.Lines[0].CanPresent, "Terminal audio failure must permit text fallback");
turn.Cursor = 1; turn.PrepareResume();
Check(turn.Cursor == 0 && turn.Lines.All(x => x.Speech == SpeechState.Pending), "Resume replays current line and regenerates audio");

var model = new FakeModel();
game = Create(model);
for (int i = 0; i < 4; i++)
{
    var drafted = await game.ProcessStepsAsync("Question " + i);
    Check(model.Reflections == i, "Drafting must return without starting reflection");
    await game.ReflectOnTurnAsync(drafted.ReflectionCritique);
}
Check(model.Drafts == 4 && model.Reflections == 4, "One draft and one reflection per turn; no reviser");
Check(game.LongTermReflections.Count == 3 && game.LongTermReflections.Last() == "Reflection 4", "Reflection memory bounded and updated");
var restored = Create(); restored.Restore(game.Save());
Check(restored.Step == 4 && restored.LongTermReflections.SequenceEqual(game.LongTermReflections), "Save/resume retains step and reflection memory");
Check(restored.Messages.Count == 8 && restored.ParticipantId == game.ParticipantId, "Save/resume retains history and participant identity");
model.FailReflection = true;
var response = await game.ProcessStepsAsync("Still answer");
response = await game.ReflectOnTurnAsync(response.ReflectionCritique);
Check(response.ReflectionError != null && game.Step == 5, "Reflection failure is explicit but does not discard successful dialogue");
model.FailReflection = false; model.Cancel = true;
try { await game.ProcessStepsAsync("cancel"); throw new Exception("Cancellation swallowed"); }
catch (OperationCanceledException) { count++; }
model.Cancel = false; model.TimeoutReflection = true;
response = await game.ProcessStepsAsync("Reflection timeout");
response = await game.ReflectOnTurnAsync(response.ReflectionCritique);
Check(response.ReflectionError == nameof(TaskCanceledException), "Reflection timeout must not lose a successful draft");

// Exercise the actual intro -> protein lab -> scaffolded reflection -> farewell progression.
var complete = Create();
while (complete.CurrentGameState.Goals.Any(x => !x.Value))
{
    var goal = complete.CurrentGameState.Goals.First(x => !x.Value).Key;
    complete.ProcessStepsWithMock("Continue", new DrafterOutput { Message = "Learning together.", ChosenGoalForTurn = goal, ChosenProtein = "myosin" });
}
var labAction = complete.ProcessStepsWithMock("Let's do it", new DrafterOutput { Message = "Let's start the lab.", Action = "TO_PROTEIN_SYNTHESIS_LAB_MYOSIN" });
Check(labAction.Action == "TO_PROTEIN_SYNTHESIS_LAB_MYOSIN" && complete.CurrentGameState.Id == "2_lab_reflection", "Intro unlocks the chosen lab and reflection state");
while (complete.CurrentGameState.Goals.Any(x => !x.Value))
{
    var goal = complete.CurrentGameState.Goals.First(x => !x.Value).Key;
    complete.ProcessStepsWithMock("Reflecting", new DrafterOutput { Message = "Keep thinking.", ChosenGoalForTurn = goal });
}
Check(complete.ProcessStepsWithMock("Done", new DrafterOutput { Message = "Goodbye.", Action = "ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL" }).Action != null,
    "Farewell unlocks after completing the reflection goals");

using (var adapter = new AnthropicClient((json, token) => Task.FromResult("{\"content\":[{\"type\":\"tool_use\",\"name\":\"respond\",\"input\":{\"message\":\"Hello.\"}}]}")))
    Check((await adapter.SendMessageAsync("context")).Message == "Hello.", "Actual Claude adapter reads structured responses");
using (var adapter = new AnthropicClient((json, token) => Task.FromResult("{\"stop_reason\":\"max_tokens\",\"content\":[]}")))
{
    try { await adapter.SendMessageAsync("context"); throw new Exception("Truncation silently accepted"); }
    catch (InvalidDataException) { count++; }
}
ClientConfiguration.ValidateSupabaseKey("sb_publishable_test");
ClientConfiguration.ValidateSupabaseKey("sb_secret_test");
var serviceRole = "e30." + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"role\":\"service_role\"}")).TrimEnd('=') + ".signature";
ClientConfiguration.ValidateSupabaseKey(serviceRole);
foreach (var invalid in new[] { "", "not-a-key", "e30.e30.signature" })
{
    try { ClientConfiguration.ValidateSupabaseKey(invalid); throw new Exception("Invalid key accepted"); }
    catch (InvalidOperationException) { count++; }
}

// Deliberately finish speech out of order: all requests must start immediately,
// and reflection must wait for the last result, including a terminal failure.
var parallelTurn = DialogueTurn.Create("Jessica:: One. Two. Three.", "Jessica", null);
var gates = parallelTurn.Lines.ToDictionary(line => line.Id, _ => new TaskCompletionSource<int>());
var done = parallelTurn.Lines.ToDictionary(line => line.Id, _ => new TaskCompletionSource<bool>());
var startedIds = new List<string>();
var audioByLine = new Dictionary<string, int>();
var reflected = 0;
var preparation = TurnPreparation.RunAsync(parallelTurn, async (line, token) => {
    startedIds.Add(line.Id);
    var value = await gates[line.Id].Task;
    token.ThrowIfCancellationRequested();
    audioByLine[line.Id] = value;
    line.Speech = value < 0 ? SpeechState.Failed : SpeechState.Ready;
    done[line.Id].SetResult(true);
}, token => { reflected++; return Task.CompletedTask; }, default);
Check(startedIds.SequenceEqual(parallelTurn.Lines.Select(line => line.Id)), "All split lines start before waiting for any audio");
gates[parallelTurn.Lines[2].Id].SetResult(3);
await done[parallelTurn.Lines[2].Id].Task.WaitAsync(TimeSpan.FromSeconds(2));
Check(!parallelTurn.CanContinue && reflected == 0, "A later clip must not unlock the pending first line or start reflection");
gates[parallelTurn.Lines[0].Id].SetResult(1);
await done[parallelTurn.Lines[0].Id].Task.WaitAsync(TimeSpan.FromSeconds(2));
Check(parallelTurn.CanContinue && reflected == 0, "First line can display before later speech and reflection finish");
parallelTurn.Cursor = 0;
Check(!parallelTurn.CanContinue, "Next stays blocked for exactly the next line");
gates[parallelTurn.Lines[1].Id].SetResult(-1);
await preparation.WaitAsync(TimeSpan.FromSeconds(2));
Check(reflected == 1 && parallelTurn.CanContinue, "Reflection follows all speech attempts; failed speech permits text fallback");
Check(audioByLine[parallelTurn.Lines[0].Id] == 1 && audioByLine[parallelTurn.Lines[2].Id] == 3,
    "Reverse completion order cannot swap line/audio identities");
using (var cancel = new CancellationTokenSource())
{
    var pending = new TaskCompletionSource<bool>();
    var callbacks = 0;
    var abandoned = TurnPreparation.RunAsync(DialogueTurn.Create("One. Two.", "Jessica", null), async (line, ct) => {
        await pending.Task; ct.ThrowIfCancellationRequested(); callbacks++;
    }, ct => { callbacks++; return Task.CompletedTask; }, cancel.Token);
    cancel.Cancel(); pending.SetResult(true);
    try { await abandoned; throw new Exception("Cancelled work succeeded"); }
    catch (OperationCanceledException) { count++; }
    Check(callbacks == 0, "Late results from cancelled audio cannot update presentation or start reflection");
}
var textOnly = DialogueTurn.Create("No audio.", "Jessica", null);
await TurnPreparation.RunAsync(textOnly, (line, ct) => { line.Speech = SpeechState.Skipped; return Task.CompletedTask; },
    ct => { reflected++; return Task.CompletedTask; }, default);
Check(textOnly.CanContinue && reflected == 2, "Text-only mode still runs reflection after speech is skipped");

var directory = Path.Combine(Path.GetTempPath(), "mosaic-checkpoint-test-" + Guid.NewGuid());
try
{
    var store = new CheckpointStore(directory);
    restored.ParticipantId = "../../unsafe";
    var checkpoint = new Checkpoint { ParticipantId = "../../unsafe", SessionJson = restored.Save(), Turn = turn, Phase = "lab", LabAction = "TO_PROTEIN_SYNTHESIS_LAB_MYOSIN" };
    checkpoint.Outbox.Add(new ResearchEvent { kind = "turn", participant_id = checkpoint.ParticipantId, session_id = checkpoint.SessionId, payload = new { user_message = "Question", agent_message = "Jessica:: Answer.", agent_response_time = 1.25 } });
    store.Save(checkpoint);
    var loaded = store.Load(checkpoint.ParticipantId)!;
    Check(Path.GetDirectoryName(store.PathFor(checkpoint.ParticipantId)) == directory, "Participant ID cannot escape save directory");
    Check(loaded.Phase == "lab" && loaded.LabAction == checkpoint.LabAction, "AR resume phase preserved");
    Check(loaded.Outbox.Single().id == checkpoint.Outbox.Single().id, "Research retry keeps idempotency key across restarts");
    store.Save(checkpoint);
    File.WriteAllText(store.PathFor(checkpoint.ParticipantId), "broken");
    Check(store.Load(checkpoint.ParticipantId)!.SessionId == checkpoint.SessionId, "Recover previous atomic checkpoint if current file corrupt");
    checkpoint.PendingReflection = new PendingReflection { TurnId = turn.Id, Critique = "Critique to preserve" };
    store.Save(checkpoint);
    Check(store.Load(checkpoint.ParticipantId)!.PendingReflection?.Critique == "Critique to preserve", "Interrupted reflection is saved for resumption");
    // A leftover auth file must be irrelevant, even if corrupt/expired.
    File.WriteAllText(Path.Combine(directory, "auth.json"), "obsolete corrupt authentication");
    var handler = new FakeHttp();
    var config = new ClientConfiguration { supabase_url = "https://study.supabase.co", supabase_key = "sb_secret_test",
        anthropic_api_key = "private-tutor", openai_api_key = "private-speech" };
    using var services = new ClientServices(config, handler);
    await services.TutorAsync("{}", default);
    Check((await services.SpeechAsync("Hello", "Jessica", default)).Length == 4, "Speech bytes return directly from OpenAI");
    await services.SendEventAsync(checkpoint.Outbox[0], default);
    await services.SendEventAsync(checkpoint.Outbox[0], default);
    Check(handler.Events.Count == 2 && handler.Events[0] == handler.Events[1] && handler.Rows.Count == 1,
        "Retried responses keep stable server-schema fields and conflict identity");
    await services.SyncCheckpointAsync(checkpoint, default);
    var cloud = await services.LoadCheckpointAsync(checkpoint.ParticipantId, default);
    Check(cloud?.SessionId == checkpoint.SessionId && cloud?.Turn?.Id == checkpoint.Turn.Id, "Game bucket recovers exact Unity checkpoint");
    Check(cloud?.PendingReflection?.TurnId == turn.Id, "Cloud resume retains pending reflection");
    Check(handler.LastReadPath.Contains(Uri.EscapeDataString("game_" + checkpoint.ParticipantId + ".json")),
        "Storage object follows server naming and escapes participant input");
    Check(handler.Providers.SequenceEqual(new[] { "api.anthropic.com/v1/messages", "api.openai.com/v1/audio/speech" }), "Inference goes directly to the providers");
    Check(handler.Paths.All(path => !path.Contains("/auth/v1/") && !path.Contains("mosaic_")), "Research never calls Auth or the replaced tables");
    Check(File.ReadAllText(Path.Combine(directory, "auth.json")) == "obsolete corrupt authentication", "No authentication file is read or rewritten");

    var legacy = System.Text.Json.Nodes.JsonNode.Parse(handler.SavedGame)!;
    foreach (var key in new[] { "finished_scenes", "logging_id", "messages", "participant_id", "username", "grade_level", "curr_game_state_id",
        "all_states", "step", "condition", "peer_tutor", "persona_description", "student_profile", "chosen_protein", "current_scene_id",
        "scene_description", "available_actions", "student_concept_language", "parameter_setting", "reflections", "no_reviser" })
        Check(legacy[key] != null || key == "chosen_protein", "Server save contract includes " + key);
    Check(legacy["all_states"]![restored.CurrentGameState.Id]!["unlockable_goals"] != null, "Nested states use Python snake_case fields");
    legacy.AsObject().Remove("unity_checkpoint");
    var imported = ServerGameFormat.Deserialize(legacy.ToJsonString(), checkpoint.ParticipantId);
    var importedSession = Create(); importedSession.Restore(imported.SessionJson);
    Check(importedSession.Step == restored.Step && importedSession.Messages.Count == restored.Messages.Count,
        "Existing Python JSON saves import history and step");
    Check(importedSession.LongTermReflections.SequenceEqual(restored.LongTermReflections), "Python import preserves reflection memory");
    Check(imported.SessionId == checkpoint.SessionId, "Python import keeps the research session identifier");
    try { ServerGameFormat.Deserialize(handler.SavedGame, "wrong participant"); throw new Exception("Wrong participant accepted"); }
    catch (InvalidDataException) { count++; }

    var diagnostic = new ResearchEvent { kind = "speech_failed", session_id = checkpoint.SessionId, participant_id = checkpoint.ParticipantId };
    checkpoint.Outbox.Add(diagnostic);
    handler.FailStorage = true;
    try { await ResearchSync.FlushAsync(services, () => checkpoint, store.Save, default); throw new Exception("Upload failure swallowed"); }
    catch (ServiceRequestException e) { Check(e.Message.Contains("HTTP 503"), "Sync failures expose HTTP status"); }
    Check(checkpoint.Outbox.Count == 2 && checkpoint.ResearchLog.Count == 0, "Storage failure leaves response and diagnostics durable for retry");
    handler.FailStorage = false;
    handler.BeforeUpload = () => {
        checkpoint = checkpoint.Snapshot();
        checkpoint.Outbox.Add(new ResearchEvent { kind = "action", session_id = checkpoint.SessionId, participant_id = checkpoint.ParticipantId });
    };
    var sent = await ResearchSync.FlushAsync(services, () => checkpoint, store.Save, default);
    Check(sent == 1 && handler.Rows.Count == 1, "Retry after partial failure cannot duplicate response rows");
    Check(checkpoint.Outbox.Count == 1 && checkpoint.Outbox[0].kind == "action", "Events from a newly committed checkpoint survive an in-flight upload");
    Check(checkpoint.ResearchLog.Count == 2 && checkpoint.ResearchLog.Any(item => item.id == diagnostic.id), "Diagnostics persist in game history rather than fake response rows");
    var synced = ServerGameFormat.Deserialize(handler.SavedGame, checkpoint.ParticipantId);
    Check(synced.Outbox.Count == 0 && synced.ResearchLog.Count == 2, "Cloud snapshot includes acknowledged diagnostics without resending them");
    handler.FailRead = HttpStatusCode.Forbidden;
    try { await services.LoadCheckpointAsync(checkpoint.ParticipantId, default); throw new Exception("Read denial mistaken for missing save"); }
    catch (ServiceRequestException) { count++; }
    handler.FailRead = null; handler.SavedGame = "";
    Check(await services.LoadCheckpointAsync(checkpoint.ParticipantId, default) == null, "Missing storage object allows a new participant");
    handler.LegacyMissing = true;
    Check(await services.LoadCheckpointAsync(checkpoint.ParticipantId, default) == null, "Legacy HTTP 400 object-not-found also allows a new participant");
    var legacyConfig = new ClientConfiguration { supabase_url = config.supabase_url, supabase_anon_key = serviceRole,
        anthropic_api_key = config.anthropic_api_key, openai_api_key = config.openai_api_key };
    using (var legacyServices = new ClientServices(legacyConfig, new FakeHttp { Key = serviceRole }))
        await legacyServices.SendEventAsync(checkpoint.ResearchLog.First(item => item.kind == "turn"), default);
    count++;

}
finally { Directory.Delete(directory, true); }
var settings = new ClientConfiguration { supabase_url = "https://study.supabase.co", supabase_anon_key = "sb_publishable_test" };
settings.ApplyOverrides("{\"anthropic_api_key\":\"private-tutor\",\"speech_enabled\":false}");
settings.ValidateProviderKeys();
Check(settings.supabase_url == "https://study.supabase.co" && !settings.speech_enabled, "Private overrides preserve public Supabase settings and allow explicit text-only mode");
settings.speech_enabled = true;
try { settings.ValidateProviderKeys(); throw new Exception("Missing speech key accepted"); }
catch (InvalidOperationException) { count++; }
Console.WriteLine($"PASS: {count} local-engine regression checks");

class FakeModel : ITutorModel
{
    public int Drafts, Reflections;
    public bool Cancel, FailReflection, TimeoutReflection;
    public Task<DrafterOutput> SendMessageAsync(string prompt, CancellationToken token = default)
    {
        if (Cancel) throw new OperationCanceledException();
        Drafts++;
        return Task.FromResult(new DrafterOutput { Message = "A complete answer." });
    }
    public Task<string> ReflectAsync(string tutor, List<Dictionary<string, string>> messages, string critique, CancellationToken token)
    {
        Reflections++;
        if (TimeoutReflection) throw new TaskCanceledException();
        if (FailReflection) throw new IOException("unavailable");
        return Task.FromResult("Reflection " + Reflections);
    }
}

class FakeHttp : HttpMessageHandler
{
    public string Key = "sb_secret_test";
    public bool FailStorage, LegacyMissing;
    public HttpStatusCode? FailRead;
    public Action? BeforeUpload;
    public List<string> Events = new(), Providers = new(), Paths = new();
    public HashSet<string> Rows = new();
    public string LastReadPath = "", SavedGame = "";
    private static HttpResponseMessage Result(string json, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(json) };
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync();
        if (request.RequestUri!.Host == "api.anthropic.com")
        {
            if (request.Headers.GetValues("x-api-key").Single() != "private-tutor" || request.Headers.GetValues("anthropic-version").Single() != "2023-06-01") throw new Exception("Incorrect Anthropic credentials");
            if (request.Headers.Contains("apikey") || request.Headers.Authorization != null) throw new Exception("Supabase credentials leaked to provider");
            Providers.Add(request.RequestUri.Host + request.RequestUri.AbsolutePath);
            return Result("{\"content\":[]}");
        }
        if (request.RequestUri.Host == "api.openai.com")
        {
            if (request.Headers.Authorization?.Parameter != "private-speech" || request.Headers.Contains("apikey")) throw new Exception("Incorrect OpenAI credentials");
            using var payload = System.Text.Json.JsonDocument.Parse(body);
            if (payload.RootElement.GetProperty("voice").GetString() != "sage" || payload.RootElement.GetProperty("response_format").GetString() != "wav") throw new Exception("Speech format changed");
            Providers.Add(request.RequestUri.Host + request.RequestUri.AbsolutePath);
            return Result("RIFF");
        }
        if (request.RequestUri.Host != "study.supabase.co") throw new Exception("Custom server dependency remains");
        if (!request.Headers.TryGetValues("apikey", out var keys) || keys.Single() != Key) throw new Exception("Missing Supabase API key");
        if (request.Headers.Authorization?.Parameter != (Key.StartsWith("sb_") ? null : Key)) throw new Exception("Incorrect direct-key authorization");
        if (request.Headers.Contains("x-api-key") || body.Contains("private-tutor") || body.Contains("private-speech")) throw new Exception("Provider credentials leaked to research");
        var path = request.RequestUri.AbsolutePath;
        Paths.Add(path);
        if (path == "/rest/v1/responses" && request.Method == HttpMethod.Post)
        {
            if (!request.Headers.GetValues("Prefer").Single().Contains("resolution=ignore-duplicates")
                || request.RequestUri.Query != "?on_conflict=created_at") throw new Exception("Research write is not idempotent");
            using var row = System.Text.Json.JsonDocument.Parse(body);
            var fields = row.RootElement.EnumerateObject().Select(x => x.Name).OrderBy(x => x);
            if (!fields.SequenceEqual(new[] { "session_id", "participant_id", "created_at", "user_message", "agent_message", "agent_response_time" }.OrderBy(x => x)))
                throw new Exception("Research schema differs from original server");
            Events.Add(body);
            Rows.Add(row.RootElement.GetProperty("created_at").GetString()!);
            return Result("");
        }
        if (path.StartsWith("/storage/v1/object/authenticated/games/") && request.Method == HttpMethod.Get)
        {
            LastReadPath = path;
            if (FailRead != null) return Result("{\"code\":\"AccessDenied\"}", FailRead.Value);
            return string.IsNullOrEmpty(SavedGame)
                ? (LegacyMissing ? Result("{\"statusCode\":\"404\",\"message\":\"Object not found\"}", HttpStatusCode.BadRequest)
                    : Result("{\"code\":\"NoSuchKey\"}", HttpStatusCode.NotFound))
                : Result(SavedGame);
        }
        if (path.StartsWith("/storage/v1/object/games/") && request.Method == HttpMethod.Post)
        {
            if (request.Headers.GetValues("x-upsert").Single() != "true" || request.Content!.Headers.ContentType?.MediaType != "application/json")
                throw new Exception("Saved games must upsert JSON");
            if (FailStorage) return Result("{\"code\":\"ServiceUnavailable\"}", HttpStatusCode.ServiceUnavailable);
            SavedGame = body;
            BeforeUpload?.Invoke(); BeforeUpload = null;
            return Result("{}");
        }
        throw new Exception("Unexpected Supabase endpoint: " + path);
    }
}
