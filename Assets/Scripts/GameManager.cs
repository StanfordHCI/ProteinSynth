using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GameEngine.Models;
using GameEngine.LLM;
using GameEngine.Systems;

/// <summary>
/// Unity MonoBehaviour singleton that wraps GameSession.
/// Replaces SocketConnection + WebSocketManager as the game orchestrator.
/// Loads data from StreamingAssets, wires engine to Yarn dialogue/UI.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Settings")]
    public string playerName = "Student";
    public string gradeLevel = "9th";
    public string peerTutor = "Yari";

    [Header("Game Settings")]
    [Tooltip("strict or lenient")]
    public string parameterSetting = "strict";
    public bool noReviser = true;

    [Header("API Settings")]
    [Tooltip("Path to config.json in StreamingAssets (contains anthropic_api_key)")]
    public string configFileName = "config.json";

    // The core game session (pure C#, no Unity dependencies)
    public GameSession Session { get; private set; }

    // Reference to the WebSocketManager for feeding responses into the existing UI
    private WebSocketManager _webSocketManager;

    // State
    private bool _isProcessing;
    public bool IsProcessing => _isProcessing;
    public bool IsInitialized { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _webSocketManager = WebSocketManager.Instance;
    }

    /// <summary>
    /// Initialize the game session. Call this after avatar selection.
    /// </summary>
    public void InitializeGame(string playerName, string gradeLevel, string peerTutor)
    {
        this.playerName = playerName;
        this.gradeLevel = gradeLevel;
        this.peerTutor = peerTutor;

        Session = new GameSession(playerName, gradeLevel, peerTutor);

        Session.InstantiateGame(
            stateDataProvider: LoadStateData,
            characterDataProvider: LoadCharacterData,
            promptDataProvider: LoadPromptData,
            parameterSetting: parameterSetting,
            noReviser: noReviser
        );

        // Load API key and set up Claude client
        var configPath = Path.Combine(Application.streamingAssetsPath, configFileName);
        if (File.Exists(configPath))
        {
            var configJson = File.ReadAllText(configPath);
            var apiKey = ParseApiKey(configJson);
            if (!string.IsNullOrEmpty(apiKey))
            {
                Session.SetAnthropicClient(new AnthropicClient(apiKey));
                Debug.Log("GameManager: Anthropic client configured");
            }
            else
            {
                Debug.LogWarning("GameManager: No API key found in config.json");
            }
        }
        else
        {
            Debug.LogWarning($"GameManager: Config file not found at {configPath}");
        }

        IsInitialized = true;
        Debug.Log($"GameManager: Game initialized for {playerName} with {peerTutor}");
    }

    /// <summary>
    /// Process player input and feed the response into the existing UI pipeline.
    /// Call this instead of SocketConnection.SendMessageToServer().
    /// </summary>
    public void ProcessPlayerInput(string message)
    {
        if (_isProcessing)
        {
            Debug.LogWarning("GameManager: Already processing a message");
            return;
        }
        StartCoroutine(ProcessPlayerInputCoroutine(message));
    }

    private IEnumerator ProcessPlayerInputCoroutine(string message)
    {
        _isProcessing = true;

        // Run the async LLM call on a background thread
        Task<GameEngine.Models.GameResponse> task = null;
        var thread = new System.Threading.Thread(() =>
        {
            task = Session.ProcessStepsAsync(message);
            task.Wait();
        });
        thread.Start();

        // Wait for completion without blocking the main thread
        while (thread.IsAlive)
            yield return null;

        _isProcessing = false;

        if (task?.IsCompletedSuccessfully == true)
        {
            var response = task.Result;
            Debug.Log($"GameManager: Got response - Action: {response.Action}");

            // Feed the response into the existing WebSocketManager pipeline
            FeedResponseToUI(response);
        }
        else
        {
            Debug.LogError($"GameManager: LLM call failed - {task?.Exception?.Message}");
            var errorResponse = new GameEngine.Models.GameResponse
            {
                Message = $"{peerTutor}:: I'm sorry, something went wrong. Could you try again?",
                Action = null,
                GoalsMet = null
            };
            FeedResponseToUI(errorResponse);
        }
    }

    /// <summary>
    /// Feed a GameResponse into the existing WebSocketManager/Yarn pipeline.
    /// This replaces the WebSocket message handling with local data.
    /// </summary>
    private void FeedResponseToUI(GameEngine.Models.GameResponse response)
    {
        if (_webSocketManager == null)
        {
            Debug.LogWarning("GameManager: WebSocketManager not found");
            return;
        }

        // Build the JSON string that WebSocketManager.HandleResponse expects
        var responseJson = JsonUtility.ToJson(new WebSocketResponseWrapper
        {
            message = response.Message,
            action = response.Action ?? "",
            next_state_id = Session.CurrentGameState.Id,
            next_state_characters = Session.CurrentGameState.Characters.ToArray(),
            image_to_display = "",
            participant_id = Session.ParticipantId
        });

        // Feed into existing HandleResponse
        _webSocketManager.HandleResponse(responseJson);
    }

    // --- Data Loading from StreamingAssets ---

    private (string json, string examples) LoadStateData(string stateId)
    {
        var basePath = Path.Combine(Application.streamingAssetsPath, "GameData", "States", stateId);
        var json = File.ReadAllText(Path.Combine(basePath, "state.json"));
        var examplesPath = Path.Combine(basePath, "examples.txt");
        var examples = File.Exists(examplesPath) ? File.ReadAllText(examplesPath) : "";
        return (json, examples);
    }

    private string LoadCharacterData(string characterName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, "GameData", "Characters", characterName, "character.json");
        return File.ReadAllText(path);
    }

    private string LoadPromptData(string filename)
    {
        var path = Path.Combine(Application.streamingAssetsPath, "GameData", "Prompts", filename);
        return File.ReadAllText(path);
    }

    private string ParseApiKey(string configJson)
    {
        // Simple JSON parsing without System.Text.Json (use Unity's JsonUtility or manual parsing)
        try
        {
            var startIdx = configJson.IndexOf("\"anthropic_api_key\"");
            if (startIdx < 0) return null;
            var colonIdx = configJson.IndexOf(':', startIdx);
            var quoteStart = configJson.IndexOf('"', colonIdx + 1);
            var quoteEnd = configJson.IndexOf('"', quoteStart + 1);
            return configJson.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Yarn command: send player message to the game engine.
    /// Replaces WebSocketManager's send_player_message.
    /// </summary>
    [YarnCommand("send_player_message_local")]
    public void SendPlayerMessageLocal()
    {
        // Get the message from Yarn variable storage
        var storage = GlobalInMemoryVariableStorage.Instance;
        if (storage != null && storage.TryGetValue("$playerInput", out string playerInput))
        {
            ProcessPlayerInput(playerInput);
        }
    }

    /// <summary>
    /// Yarn command: send the first message to start the conversation.
    /// Replaces WebSocketManager's send_first_message.
    /// </summary>
    [YarnCommand("send_first_message_local")]
    public void SendFirstMessageLocal()
    {
        ProcessPlayerInput("(start conversation)");
    }
}

/// <summary>
/// Wrapper class matching the JSON shape WebSocketManager.HandleResponse expects.
/// Uses the same field types as the existing ResponseData class in WebSocketManager.cs.
/// </summary>
[System.Serializable]
public class WebSocketResponseWrapper
{
    public string message;
    public string action;
    public string next_state_id;
    public string[] next_state_characters;
    public string image_to_display;
    public string participant_id;
}
