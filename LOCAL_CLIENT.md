# Local tutoring client

The `UITesting` scene runs the tutoring state machine in Unity. `DialogueService`
hosts `GameManager`, `MessageQueueCommands`, `LastLineScroll`, and
`MainThreadDispatcher`; the presenter references the dialogue AudioSource.
The old `SocketConnection` and `WebSocketManager` scripts and paired text/audio
queues are gone. The Python server is not part of the current runtime.

## Runtime and scene setup

The supported lesson is the protein-synthesis introduction
(`0_intro_proteinSynthesis`), the AR lab, and the post-lab conversation
(`2_lab_reflection`), followed by the end screen. The six peer tutors are Alex,
Benji, Isaiah, Jessica, Maya, and Yari. The eight protein choices are lactase,
hemoglobin, insulin, myosin, keratin, immunoglobulins, tyrosinase, and cytokines.

Keep the enabled build scenes in their current order: `UITesting`,
`AvatarSelection`, and `ImageTargetTestCodon`. Start from `UITesting`; the other
scenes are loaded additively by the dialogue flow.

| Component | Required connection / responsibility |
| --- | --- |
| `GameManager` on `DialogueService` | Initialize/resume, submit/retry turns, save progress, and enter/complete labs. |
| `MessageQueueCommands` on `DialogueService` | Present lines/options and play clips through its assigned `audioSource`. |
| `LastLineScroll` on `DialogueService` | Transcript UI and the `hide_scroll` Yarn command. |
| `MainThreadDispatcher` on `DialogueService` | Execute queued transcript UI updates. |
| `GlobalInMemoryVariableStorage` beside `DialogueRunner` | Bind the shared Yarn variable store in `Awake`, before session initialization; only one may be active. |
| `AudioReadyLineView` on the dialogue line view | Reference the same `MessageQueueCommands` and guard both the Next button and direct/keyboard advancement. |

Yarn commands target named GameObjects. For example,
`<<hide_scroll DialogueService true>>` requires `LastLineScroll` on that exact
object. Renaming the object or leaving the component on an old WebSocket object
breaks command dispatch. Scripts using `[YarnCommand]` need `using Yarn.Unity;`.

The direct protein-selection UI (`RetryLab`) also goes through
`InitializeSession`, using `$requested_lab_action`. It loads or creates a session,
selects the requested protein, resets its post-lab state, and saves the lab phase
before entering the activity. It does not bypass session initialization.

## Turn and audio flow

1. Draft a response with Anthropic and commit the validated goals, actions, and
   conversation to a local checkpoint. There is no revision pass.
2. Split the response into dialogue lines once. Every turn and line has a stable
   UUID; options are excluded from speech.
3. Start **all remaining lines' OpenAI speech requests concurrently**. Each result
   stays attached to its original turn and line, regardless of completion order.
   Display the first line as soon as its audio is ready. Later audio can finish
   first but never changes the text order or unlocks the wrong Next button.
4. After **all audio attempts finish**, run private reflection. This starts after
   generation/decoding, not after playback. Failed audio permits text fallback;
   disabled speech skips generation but still runs reflection.
5. Save reflection memory and use it in the next tutor prompt. The next submitted
   input waits for the previous reflection before drafting. Reading, Next, and
   playback do not wait for reflection. Keep the last three successful reflections.

Reflection work is saved before audio starts. If the app closes during audio or
reflection, resume regenerates the interrupted and remaining lines and then runs
pending reflection. If the turn was already finished, resume runs the remaining
reflection directly. Reflection failures are recorded without discarding dialogue.
The separate post-lab learning/reflection conversation is also retained.

`Assets/Scripts/GameEngine/` contains the framework-independent engine, dialogue,
persistence, and HTTP transport. `GameEngine/GameEngine.csproj` compiles those same
files, using the data in `Assets/StreamingAssets/GameData/`.
`GameManager` owns Unity lifecycle, turn transactions, audio, and saves.
`MessageQueueCommands` owns presentation and the clips keyed by line ID.

### Provider requests and audio lifetime

The current model names and voice mapping are hardcoded in `AnthropicClient` and
`ClientServices`, rather than configurable through JSON:

| Request | Endpoint / model |
| --- | --- |
| Tutor draft and private reflection | Anthropic `/v1/messages`, `claude-haiku-4-5-20251001`. Drafts use the structured `respond` tool. |
| Generated speech | OpenAI `/v1/audio/speech`, `gpt-4o-mini-tts`, WAV response. |

Jessica uses `sage`; Maya and Yari use `marin`; Benji uses `verse`; Alex and Isaiah
use `cedar` (also the fallback for other speaker names).

Each spoken line makes its own asynchronous HTTP request. There is no sequential
speech queue or application-level concurrency limit. The entire WAV response is
read before `WavUtility` decodes it and creates an AudioClip on Unity's main
thread. Audio is not streamed during download or cached to disk. Clips are held
in memory by line ID and destroyed when advancing past them, replacing the turn,
or destroying the presenter. Resume therefore requests audio again.

The first displayed line waits for its own audio attempt to settle, not for the
whole response. Next waits for the following line's audio readiness, not for the
current clip to finish playing; advancing stops the previous clip. A slow or
retried request can still delay its line. After the final line, reading can finish
while reflection runs, but submitting the next turn waits for that reflection.

Scripted AR lab voiceovers still use `AudioManager` and the bundled recordings
referenced by Yarn. They do not make these OpenAI requests.

## Managed-device configuration

The client calls Anthropic, OpenAI, and Supabase directly. There is no custom
server, gateway, Python process, Edge Function, or Supabase Auth session.

Keep private configuration in the ignored `.local/config.json`:

```json
{
  "supabase_url": "https://YOUR_PROJECT.supabase.co",
  "supabase_key": "YOUR_MANAGED_DEVICE_SUPABASE_KEY",
  "anthropic_api_key": "YOUR_ANTHROPIC_KEY",
  "openai_api_key": "YOUR_OPENAI_KEY",
  "speech_enabled": true
}
```

Managed devices can use the existing server's secret/service-role key directly.
`supabase_anon_key` remains a backward-compatible alias; `supabase_key` takes
precedence. Publishable/anon keys are also supported with the optional policies
below. Modern Supabase keys go in `apikey`; legacy JWT keys also go in
`Authorization: Bearer`. No signup, login, refresh, user ID, or `auth.json` is used.
Old `auth.json` files are ignored.

The Editor loads settings in this order:

1. `Assets/StreamingAssets/client-settings.json` (currently includes the study
   Supabase URL, a publishable key under `supabase_anon_key`, and speech enabled).
2. `.local/config.json`, overriding fields present in that file.
3. `ANTHROPIC_API_KEY` and `OPENAI_API_KEY` environment variables, only if those
   provider keys are still empty.
4. `Application.persistentDataPath/Mosaic/client-settings.json`, if present,
   overriding fields last. This override also applies in the Editor.

There are no Supabase environment-variable fallbacks in this loader.
`speech_enabled: false` allows deliberate text-only use without an OpenAI key.
For local-only Editor testing, explicitly override `supabase_url` to an empty
string; omitting it retains the versioned default. The console then reports that
research is local only. Drafting still requires an Anthropic key and network access.

For managed builds, `ValidateClientBuild` checks configuration, generates
`.local/build/managed-client-settings.json`, and includes it in the player through
Unity's additional StreamingAssets mechanism. Credentials stay out of versioned
Assets but **are included in the installed managed build**. An optional
`Application.persistentDataPath/Mosaic/client-settings.json` overrides settings on
a device. Players read the generated settings, not `.local/config.json` or runtime
environment variables. Rebuild/reinstall to change bundled settings, or use the
device override, then restart; settings are cached after services initialize.
Credentials are never serialized into research events or game saves.

Unlike local-only Editor testing, the build processor requires an HTTPS Supabase
URL and a recognized Supabase key, as well as the Anthropic key and an OpenAI key
when speech is enabled. These checks validate configuration shape, not live API
access, quotas, table permissions, or model availability. The processor rejects
credential source fields in versioned StreamingAssets; keep the private source in
`.local/config.json`.

## Supabase setup

The existing study project's live schema already has `created_at` as the primary
key of `responses`, and the client uses that key for duplicate-safe retry. No new
index or migration is required for that project. Retried events keep their original
timestamp and use `on_conflict=created_at` with `resolution=ignore-duplicates`.

For a **fresh project**, `supabase/migrations/20260929010000_direct_research.sql`
creates the same table and the `games` bucket without deleting existing data.
The managed server key needs no Auth configuration or anonymous access policy.
If choosing a publishable/anon key instead, `supabase/allow_anon_research.sql` grants
direct access without sign-in. Its optional policies permit response inserts and
reads plus `game_*.json` reads/updates in `games`; column grants limit new response
read access to the primary key, while any existing wider grants remain in effect.

The old experimental `mosaic_research_events` and `mosaic_checkpoints` tables are no
longer used. If they were created previously, they are left intact.

## Research format and upload reliability

Each successfully committed tutor draft produces one row in **`responses`**, with
exactly the same six columns as the old server's `record_response`:

- `session_id`: stable research session UUID, also saved as `logging_id`.
- `created_at`: the turn's original UTC timestamp, unchanged on retries.
- `participant_id`: participant ID entered in Unity.
- `user_message`: submitted student text.
- `agent_message`: the whole formatted response, before sentence splitting.
- `agent_response_time`: seconds to generate the draft; excludes audio/reflection.

Rows are queued when the draft is saved, before speech or playback finishes.
The initial `(start conversation)` and lab-completion messages also produce tutor
turns, so `user_message` is not always literal student input. A failed draft
produces a diagnostic event, not a response row.

The saved game goes to **`games/game_<participant_id>.json`** using Storage upsert.
It contains the Python server's original fields, including snake-case states,
conversation history, goals/actions, student profile, chosen protein, and
reflections. An additional `unity_checkpoint` contains the precise dialogue cursor,
phase, pending input/reflection, research history, and upload outbox. Original
Python JSON saves can also be imported when no local save exists; those older
files lack the Unity line cursor and AR phase, so they resume the conversation.

Diagnostics (speech failures, actions, reflection results, session starts, lab
completion) live in the game's `unity_checkpoint.ResearchLog`. They are not fake
student-response rows. Pending events stay in `Outbox` until both their response
rows and the game upload succeed. If game upload fails after rows were inserted,
retrying uses the same timestamps and does not duplicate rows. Events added while
an upload is in flight stay queued.

New events trigger a sync on the next update; failed uploads retry at roughly
30-second intervals while the app runs. The console reports successful response
uploads and reports HTTP status/error codes on failure. Research remains saved
locally through outages and resumes uploading on the next launch. Game writes
are serialized on one client; as with the server, the same participant's cloud
file uses last-writer-wins, so use one active device per participant.

## Save, resume, and failures

Local checkpoints are stored in
`Application.persistentDataPath/Mosaic/sessions/<SHA256 of participant ID>.json`.
Writes use a flushed temporary file and atomic replacement; `.bak` is the previous
valid version. Entering the same participant ID resumes locally, or downloads its
saved game from Supabase if no local file exists. Without user Auth, managed
devices can resume another device's save using its participant ID. Invalid saves,
permission errors, and missing buckets are reported instead of being mistaken for
new participants. An actual missing game object starts a new session.

Participant IDs are trimmed but remain case-sensitive. An empty ID is replaced
with a generated eight-character ID. A local checkpoint takes precedence over
the cloud; the client does not compare timestamps or merge them. If an existing
local file cannot be read or validated, the store tries its `.bak`; it does not
silently fall back to a cloud save after both fail. Resuming restores the saved
name, grade, and tutor rather than replacing them with the newly entered values.

Legacy Python imports support only `0_intro_proteinSynthesis` and
`2_lab_reflection`. They must contain compatible state data, the matching
`participant_id`, and a valid UUID `logging_id`. Saves from other server scenes
are rejected. The Unity checkpoint and embedded session currently use version 1.

An interrupted line is replayed. An unfinished draft retries from the last
committed session without duplicating input in conversation history. A lab resumes
from the start of the hands-on activity; individual AR card positions and
animations are not checkpointed. Completed sessions return to the end screen.

HTTP attempts time out after 45 seconds. Each speech line gets up to two attempts,
with a one-second delay before its retry, independently of other lines. Continue
is disabled with “Preparing audio…” while the next line is pending. After both
attempts fail it becomes “Next (text only)”. Null clips never enter the clip map.
Cancellation and turn/line ownership checks reject late results. Restarting a
session or destroying the scene cancels outstanding audio and reflection work.
Pausing the application saves progress; it does not explicitly cancel requests.

AudioClip decoding/creation and scene changes remain on Unity's main thread.
StreamingAssets use UnityWebRequest, including Android packaged assets.
`link.xml` preserves serialized types and anonymous payloads for IL2CPP.

## iOS build and device setup

The project records Unity **2022.3.33f1**, Yarn Spinner **2.5.1**, Newtonsoft JSON
**3.2.1**, URP **14.0.11**, and Vuforia **10.29.6**. The Vuforia dependency is the
local archive `Packages/com.ptc.vuforia.engine-10.29.6.tgz`; it must be available
on the build machine. `Assets/csc.rsp` enables C# 10 and nullable annotations.

- Install Unity's iOS Build Support and use the three enabled scenes above.
  Run the normal Unity build so `ValidateClientBuild` packages private settings.
  Rebuilding only the existing Xcode export does not refresh those settings.
- The current bundle identifier is `com.crafts.arlabs` and the deployment target
  is iOS 15.0. Configure the appropriate development team and signing in Xcode.
  Keep the app identity consistent when testing local save/resume across updates.
- Test on a physical device with working Vuforia configuration and camera access.
  The project already includes the camera usage description. Generated speech is
  playback only; this client does not record microphone input.
- The device needs HTTPS access to Anthropic, OpenAI when speech is enabled, and
  the configured Supabase project. There is no local server address to configure.
- Keep `Assets/Scripts/GameEngine/link.xml` in the build: JSON serialization uses
  reflection under IL2CPP. The preservation entries target `Assembly-CSharp`.

For the previously observed Xcode **“Unexpected duplicate tasks”** error involving
`GameAssembly`, inspect its build phases for duplicate references to the IL2CPP
shell script that produces `libGameAssembly.a`. The repaired export has one such
phase. That repair is in the generated Xcode project, not a Unity post-build fix
in this repository. Use a fresh export directory when regenerating the project;
if the error recurs, inspect the full Xcode diagnostic before removing anything.

## Troubleshooting

### “We couldn't load your session”

This is the generic Yarn message for **any exception during initialization**. It
does not prove a connection failure or that the participant ID is missing. Find
the preceding Unity/device log beginning
`Tutoring request failed: <exception type>: <message>`.

| Underlying log / condition | What to check |
| --- | --- |
| Missing provider key, invalid Supabase key, or non-HTTPS service URL | Effective configuration, including the device override. These checks run even when resuming an existing local save. |
| `Unable to load managed-client-settings.json` or a `GameData/...` file | Generated StreamingAssets and lesson data in the installed build; rebuild through Unity. |
| HTTP 401/403 or a permission error | Supabase project/key and access to the `games` bucket. A publishable key needs the optional policies. |
| Missing bucket or another Storage error | Correct project and bucket. Only an object-not-found response (`NoSuchKey` or `Object not found`, HTTP 400/404) is treated as a new participant. |
| Invalid/unsupported checkpoint, malformed JSON, or unsupported saved scene | Local file and backup, or the cloud JSON if no local file exists; check participant identity, UUID, versions, and supported states. |
| Timeout or transport failure | Device connectivity and the exception details. Cloud lookup occurs only when no local checkpoint exists. |

Preserve the original save while investigating. Retry repeats initialization; it
does not repair incompatible data. A provider failure during the subsequent tutor
draft instead displays “We couldn't reach your tutor. Your progress is saved.”

### No research responses appearing

Check for `Research synced: N response(s) to responses; saved game uploaded to
games.` or `Research sync deferred; saved locally.` in the console. The success
message is emitted only when `N > 0`; a game-only sync is not logged as a success.
Inspect the local checkpoint's `Outbox` for pending turns, and confirm that the
dashboard is showing the same Supabase project as the effective configuration.

Use a committed tutor turn to test `responses`; initialization and diagnostic
events alone do not create rows. Check `games/game_<participant_id>.json` for the
save and diagnostic history. When using a publishable key, verify both table
insert access and Storage read/insert/update access. A failed game upload leaves
the outbox pending even if response rows have already arrived.

### Dialogue/audio or Yarn errors

- “Preparing audio…” means the following line is still pending. Each request can
  take up to 45 seconds per attempt; two failed attempts fall back to text. Check
  `Speech failed for line <id>` for the provider or decoding error.
- A delay before a new response can include the prior turn's private reflection,
  the next draft request, and the first line's audio request. Reflection does not
  gate Next within the current response.
- `hide_scroll` dispatch errors indicate that `DialogueService` is missing
  `LastLineScroll` or has stale scene bindings. A `YarnCommand` compile error
  requires checking `using Yarn.Unity;` and the Yarn package, not API credentials.

## Verification

From the `ProteinSynth` project root, with the .NET 8 SDK:

```sh
dotnet run --project GameEngine/Tests/Tests.csproj
```

The suite checks goal/action progression, concurrent speech with deliberately
reversed completion order, Next gating, terminal failures, cancellation, deferred
reflection, save/resume, Python JSON compatibility, direct API-key transport,
server response columns, Storage paths, and retries after partial upload failure.
Fake HTTP tests send no student data and make no live provider requests.

C# compilation and automated tests do not replace a Unity/device run. Test typed
and option input, rapid Next presses, slow/failed TTS, quit/resume during audio and
reflection, lab selection/completion, and Supabase uploads after an outage.
