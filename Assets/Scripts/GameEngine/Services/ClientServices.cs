using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GameEngine.Activities;
using GameEngine.Persistence;

namespace GameEngine.Services;

public class ClientConfiguration
{
    public string supabase_url = "";
    public string supabase_anon_key = ""; // Backward-compatible configuration alias.
    public string supabase_key = ""; // Managed-device key, loaded from private configuration.
    [JsonIgnore]
    public string SupabaseKey => string.IsNullOrWhiteSpace(supabase_key) ? supabase_anon_key : supabase_key;
    public bool speech_enabled = true;
    public string anthropic_api_key = ""; // Loaded from private configuration for managed devices.
    public string openai_api_key = "";

    public void ApplyOverrides(string json) => JsonConvert.PopulateObject(json, this);

    public void ValidateProviderKeys()
    {
        if (string.IsNullOrWhiteSpace(anthropic_api_key))
            throw new InvalidOperationException("Configure the Anthropic API key before starting a session.");
        if (speech_enabled && string.IsNullOrWhiteSpace(openai_api_key))
            throw new InvalidOperationException("Configure the OpenAI API key or disable speech.");
    }

    public static void ValidateSupabaseKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && (key.StartsWith("sb_publishable_") || key.StartsWith("sb_secret_"))) return;
        try
        {
            var parts = key.Split('.');
            if (parts.Length == 3)
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
                var role = (string?)JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)))["role"];
                if (role == "anon" || role == "service_role") return;
            }
        }
        catch (Exception) { /* Invalid JWT shape. */ }
        throw new InvalidOperationException("Configure a valid Supabase API key for this managed client.");
    }
}

// Managed-device client: calls provider APIs and Supabase Data/Storage APIs directly.
public sealed class ClientServices : IDisposable
{
    private readonly ClientConfiguration config;
    private readonly HttpClient http;
    private readonly bool hasSupabase;
    public bool CanSync => hasSupabase;
    public bool SpeechEnabled => config.speech_enabled;

    public ClientServices(ClientConfiguration config, HttpMessageHandler? handler = null)
    {
        this.config = config;
        http = new HttpClient(handler ?? new HttpClientHandler { MaxConnectionsPerServer = int.MaxValue });
        hasSupabase = !string.IsNullOrWhiteSpace(config.supabase_url);
        config.ValidateProviderKeys();
        if (hasSupabase)
        {
            RequireHttps(config.supabase_url);
            ClientConfiguration.ValidateSupabaseKey(config.SupabaseKey);
        }
    }

    private static void RequireHttps(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new InvalidOperationException("Service URLs must use HTTPS.");
    }

    public async Task<string> TutorAsync(string json, CancellationToken token)
    {
        using var request = JsonRequest("https://api.anthropic.com/v1/messages", json);
        request.Headers.Add("x-api-key", config.anthropic_api_key);
        request.Headers.Add("anthropic-version", "2023-06-01");
        return Encoding.UTF8.GetString(await SendAsync(request, token));
    }

    public async Task<byte[]> SpeechAsync(string text, string tutor, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(config.openai_api_key))
            throw new InvalidOperationException("Speech API key is not configured.");
        var voice = tutor == "Jessica" ? "sage" : tutor == "Maya" || tutor == "Yari" ? "marin"
            : tutor == "Benji" ? "verse" : "cedar";
        using var request = JsonRequest("https://api.openai.com/v1/audio/speech", JsonConvert.SerializeObject(new {
            model = "gpt-4o-mini-tts", voice, input = text, response_format = "wav"
        }));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.openai_api_key);
        return await SendAsync(request, token);
    }

    public async Task SendEventAsync(ResearchEvent item, CancellationToken token)
    {
        if (!hasSupabase) throw new InvalidOperationException("Research sync requires Supabase configuration.");
        if (item.kind != "turn") throw new ArgumentException("Only tutor turns belong in the responses table.");
        var payload = JObject.FromObject(item.payload);
        // Exactly the server's six columns. The event timestamp is stable across retries.
        var row = new JObject {
            ["session_id"] = item.session_id, ["participant_id"] = item.participant_id,
            ["created_at"] = item.created_at,
            ["user_message"] = payload["user_message"] ?? throw new InvalidDataException("Research turn is missing input."),
            ["agent_message"] = payload["agent_message"] ?? throw new InvalidDataException("Research turn is missing output."),
            ["agent_response_time"] = payload["agent_response_time"]
        };
        using var request = JsonRequest(config.supabase_url.TrimEnd('/')
            + "/rest/v1/responses?on_conflict=created_at", row.ToString(Formatting.None));
        Authorize(request);
        request.Headers.Add("Prefer", "resolution=ignore-duplicates,return=minimal");
        await SendAsync(request, token);
    }

    public async Task SyncCheckpointAsync(Checkpoint checkpoint, IActivity activity, CancellationToken token)
    {
        if (!hasSupabase) throw new InvalidOperationException("Research sync requires Supabase configuration.");
        using var request = JsonRequest(GameUrl(checkpoint.ParticipantId, false, activity), ServerGameFormat.Serialize(checkpoint, activity));
        Authorize(request);
        request.Headers.Add("x-upsert", "true");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        await SendAsync(request, token);
    }

    public async Task<Checkpoint?> LoadCheckpointAsync(string participant, IActivity activity, CancellationToken token)
    {
        if (!hasSupabase) return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, GameUrl(participant, true, activity));
        Authorize(request);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        try
        {
            return ServerGameFormat.Deserialize(Encoding.UTF8.GetString(await SendAsync(request, token)), participant, activity);
        }
        catch (ServiceRequestException e) when (e.ObjectNotFound) { return null; }
    }

    // games/game_<participant>.json for the legacy location, games/<folder>/game_<participant>.json otherwise.
    private string GameUrl(string participant, bool download, IActivity activity) => config.supabase_url.TrimEnd('/')
        + "/storage/v1/object/" + (download ? "authenticated/" : "")
        + "games/" + (activity.SaveFolder == null ? "" : Uri.EscapeDataString(activity.SaveFolder) + "/")
        + Uri.EscapeDataString("game_" + participant + ".json");

    private void Authorize(HttpRequestMessage request)
    {
        request.Headers.Add("apikey", config.SupabaseKey);
        // Legacy keys are JWTs. Publishable/secret keys use only apikey; neither
        // requires a signed-in user, refresh token, or /auth/v1 request.
        if (!config.SupabaseKey.StartsWith("sb_"))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.SupabaseKey);
    }

    private static HttpRequestMessage JsonRequest(string url, string body) => new HttpRequestMessage(HttpMethod.Post, url) {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private async Task<byte[]> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        using var response = await http.SendAsync(request, timeout.Token);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        timeout.Token.ThrowIfCancellationRequested();
        if (!response.IsSuccessStatusCode)
            throw new ServiceRequestException(response.StatusCode, bytes);
        return bytes;
    }

    public void Dispose() => http.Dispose();
}

// Include status and machine-readable error code, without logging response bodies,
// credentials, or student messages. Storage may report a missing object as HTTP 400.
public sealed class ServiceRequestException : HttpRequestException
{
    public bool ObjectNotFound { get; }
    public ServiceRequestException(HttpStatusCode status, byte[] body)
        : base($"Service returned HTTP {(int)status} ({ReadCode(body)}).")
    {
        var error = Parse(body);
        ObjectNotFound = (status == HttpStatusCode.NotFound || status == HttpStatusCode.BadRequest)
            && ((string?)error?["code"] == "NoSuchKey"
                || string.Equals((string?)error?["message"], "Object not found", StringComparison.OrdinalIgnoreCase));
    }
    private static JObject? Parse(byte[] body)
    {
        try { return JObject.Parse(Encoding.UTF8.GetString(body)); }
        catch (JsonException) { return null; }
    }
    private static string ReadCode(byte[] body)
    {
        var error = Parse(body);
        var code = (string?)error?["code"] ?? (error?["error"] is JObject detail ? (string?)detail["type"] : null);
        return code != null && System.Text.RegularExpressions.Regex.IsMatch(code, @"\A[A-Za-z0-9_]{1,80}\z")
            ? code : "request failed";
    }
}
