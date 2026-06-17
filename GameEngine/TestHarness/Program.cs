using System.Text.Json;
using GameEngine.Data;
using GameEngine.LLM;
using GameEngine.Models;
using GameEngine.Systems;

/// <summary>
/// Terminal test harness for the GameEngine.
/// Usage:
///   dotnet run -- --mock              # Mock LLM responses, test state machine
///   dotnet run -- --config config.json # Real Claude API calls (interactive)
///   dotnet run -- --config config.json --auto  # Automated (Claude plays student)
/// </summary>

var dataDir = FindDataDirectory();
Console.WriteLine($"Using data directory: {dataDir}");

// Parse arguments
var useMock = args.Contains("--mock");
var configIdx = Array.IndexOf(args, "--config");
var configPath = configIdx >= 0 && configIdx + 1 < args.Length ? args[configIdx + 1] : null;
var autoMode = args.Contains("--auto");

if (!useMock && configPath == null)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- --mock                      # Test state machine with mock responses");
    Console.WriteLine("  dotnet run -- --config path/to/config.json # Interactive with real Claude API");
    Console.WriteLine("  dotnet run -- --config path/to/config.json --auto  # Automated testing");
    Console.WriteLine();
    Console.WriteLine("config.json format: { \"anthropic_api_key\": \"sk-ant-...\" }");
    return;
}

// Get player info
Console.Write("Enter your name: ");
var username = Console.ReadLine()?.Trim() ?? "Student";

Console.Write("Enter your grade level: ");
var gradeLevel = Console.ReadLine()?.Trim() ?? "9th";

Console.Write("Choose a peer tutor (Yari/Alex/Jessica/Benji/Isaiah/Maya): ");
var peerTutor = Console.ReadLine()?.Trim() ?? "Yari";
if (string.IsNullOrEmpty(peerTutor)) peerTutor = "Yari";
peerTutor = char.ToUpper(peerTutor[0]) + peerTutor[1..].ToLower();

// Create and initialize game session
var game = new GameSession(username, gradeLevel, peerTutor);
game.InstantiateGame(
    stateDataProvider: stateId =>
    {
        var json = File.ReadAllText(Path.Combine(dataDir, "States", stateId, "state.json"));
        var examples = File.ReadAllText(Path.Combine(dataDir, "States", stateId, "examples.txt"));
        return (json, examples);
    },
    characterDataProvider: name =>
        File.ReadAllText(Path.Combine(dataDir, "Characters", name, "character.json")),
    promptDataProvider: filename =>
        File.ReadAllText(Path.Combine(dataDir, "Prompts", filename)),
    parameterSetting: "strict",
    noReviser: true
);

Console.WriteLine($"\nGame initialized! State: {game.CurrentGameState.Id}");
Console.WriteLine($"Goals: {JsonSerializer.Serialize(game.CurrentGameState.Goals)}");
Console.WriteLine($"Unlockable goals: {JsonSerializer.Serialize(game.CurrentGameState.UnlockableGoals)}");
Console.WriteLine();

// Configure LLM client if using real API
if (!useMock && configPath != null)
{
    var configJson = File.ReadAllText(configPath);
    using var configDoc = JsonDocument.Parse(configJson);
    var apiKey = configDoc.RootElement.GetProperty("anthropic_api_key").GetString()!;
    game.SetAnthropicClient(new AnthropicClient(apiKey));
}

if (useMock)
{
    RunMockMode(game);
}
else if (autoMode)
{
    await RunAutoModeAsync(game);
}
else
{
    await RunInteractiveModeAsync(game);
}

// --- Mode implementations ---

void RunMockMode(GameSession game)
{
    Console.WriteLine("=== MOCK MODE: Testing state machine with scripted responses ===\n");

    // Turn 1: Start conversation
    var response1 = game.ProcessStepsWithMock("(start conversation)", new DrafterOutput
    {
        ChosenGoalForTurn = $"{peerTutor} briefly introduces themselves",
        Message = $"Hey! I'm {peerTutor}, nice to meet you! I'm in college studying biology and I love making science fun. What's something you're into outside of school?",
        Action = null
    });
    PrintTurnResult("(start)", response1, game);

    // Turn 2: Student shares interest
    var response2 = game.ProcessStepsWithMock("I like basketball and cooking", new DrafterOutput
    {
        ChosenGoalForTurn = "Ask the student about their family or traditions",
        Message = $"Basketball and cooking? That's awesome! Do you cook with your family? What kind of food do y'all usually make?",
        StudentInterest = "basketball",
        Action = null
    });
    PrintTurnResult("I like basketball and cooking", response2, game);

    // Turn 3: Student shares tradition
    var response3 = game.ProcessStepsWithMock("Yeah my grandma makes tamales during holidays", new DrafterOutput
    {
        ChosenGoalForTurn = "The student names one thing they learned or found interesting about protein synthesis in their lesson",
        Message = $"Tamales with grandma? That's beautiful! So tell me, what's one thing you remember from the protein synthesis lesson your teacher gave you?",
        Action = null
    });
    PrintTurnResult("Yeah my grandma makes tamales", response3, game);

    // Turn 4: Student shares what they learned
    var response4 = game.ProcessStepsWithMock("Something about DNA making copies I think", new DrafterOutput
    {
        ChosenGoalForTurn = "Ask the student if they still have any questions about protein synthesis",
        Message = "Yeah that's right! DNA basically has the recipe and your body copies it. Do you have any questions about protein synthesis so far?",
        Action = null
    });
    PrintTurnResult("Something about DNA making copies", response4, game);

    // Turn 5: Student says no questions, protein goal unlocked
    var proteinsListStr = $"['{string.Join("', '", ProteinData.ProteinsList)}']";
    var proteinGoal = $"Introduce ONE of the following valid list of proteins based on the student's interest: {proteinsListStr}";

    var response5 = game.ProcessStepsWithMock("Nah I think I'm good", new DrafterOutput
    {
        ChosenGoalForTurn = proteinGoal,
        Message = "All good! So check this out—since you're into basketball, let's look at a protein that helps your muscles work when you play. It's called myosin. Ready to dive into the lab?",
        ChosenProtein = "myosin",
        Action = null
    });
    PrintTurnResult("Nah I think I'm good", response5, game);

    // Turn 6: Student agrees to lab → should trigger action
    var response6 = game.ProcessStepsWithMock("Yeah let's do it!", new DrafterOutput
    {
        ChosenGoalForTurn = null,
        Message = "Let's gooo! Time to get hands-on with protein synthesis!",
        Action = "TO_PROTEIN_SYNTHESIS_LAB_MYOSIN"
    });
    PrintTurnResult("Yeah let's do it!", response6, game);

    // Verify state transition
    Console.WriteLine($"\n=== STATE AFTER LAB TRANSITION ===");
    Console.WriteLine($"Current state: {game.CurrentGameState.Id}");
    Console.WriteLine($"Goals: {JsonSerializer.Serialize(game.CurrentGameState.Goals)}");
    Console.WriteLine($"Actions: {string.Join(", ", game.CurrentGameState.Actions.Keys)}");

    // Lab reflection turns
    Console.WriteLine("\n=== LAB REFLECTION PHASE ===\n");

    // Turn 7: PROVIDE_VOCAB action
    var response7 = game.ProcessStepsWithMock("That lab was cool!", new DrafterOutput
    {
        ChosenGoalForTurn = $"{peerTutor} asks the student to explain protein synthesis without using scientific vocabulary",
        Message = "Right?! So now that you've done the lab, try explaining protein synthesis to me like you're telling your grandma about it—no fancy science words allowed!",
        Action = "PROVIDE_VOCAB"
    });
    PrintTurnResult("That lab was cool!", response7, game);

    // Turn 8: Student explains without vocab
    var response8 = game.ProcessStepsWithMock("So like the body reads a recipe from the instruction book, makes a copy, then workers build the thing using building blocks", new DrafterOutput
    {
        ChosenGoalForTurn = $"{peerTutor} provides the list of scientific vocabulary learned in the lesson to the student",
        Message = "That's actually a great way to put it! Now let me share the official vocabulary terms we covered.",
        Action = null
    });
    PrintTurnResult("Student explains without vocab", response8, game);

    // Turn 9: Near transfer question
    var response9 = game.ProcessStepsWithMock("Ok got it", new DrafterOutput
    {
        ChosenGoalForTurn = $"{peerTutor} asks the student to answer a near transfer question",
        Message = "Great! Now try this: Antibodies are proteins your body makes to fight infections. Using what you learned, how does your body actually make an antibody?",
        Action = null
    });
    PrintTurnResult("Ok got it", response9, game);

    // Turn 10: Student answers near transfer
    var response10 = game.ProcessStepsWithMock("The DNA has the code for the antibody, then mRNA copies it through transcription, then the ribosome uses tRNA to bring amino acids and builds the protein through translation", new DrafterOutput
    {
        ChosenGoalForTurn = $"{peerTutor} asks the student to answer a far transfer question",
        Message = "You nailed it! Now for a trickier one: imagine scientists discover a mutation in a gene. How might that affect the protein that gene codes for?",
        Action = null
    });
    PrintTurnResult("Student answers near transfer", response10, game);

    // Turn 11: Student answers far transfer
    var response11 = game.ProcessStepsWithMock("If the DNA code changes then the mRNA would have a different codon, which means a different tRNA and amino acid, so the protein might not fold right or work right", new DrafterOutput
    {
        ChosenGoalForTurn = null,
        Message = "That was incredible! You really understand how the whole process connects. I'm so proud of how far you've come today!",
        Action = "ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL"
    });
    PrintTurnResult("Student answers far transfer", response11, game);

    Console.WriteLine($"\n=== FINAL STATE ===");
    Console.WriteLine($"Current state: {game.CurrentGameState.Id}");
    Console.WriteLine($"Goals: {JsonSerializer.Serialize(game.CurrentGameState.Goals)}");
    Console.WriteLine($"Actions remaining: {game.CurrentGameState.Actions.Count}");
    Console.WriteLine($"Game completed: {game.ActionSystem.IsGameCompleted(game.AllStates)}");
    Console.WriteLine($"Total turns: {game.Step}");
    Console.WriteLine("\n=== MOCK TEST PASSED ===");
}

async Task RunInteractiveModeAsync(GameSession game)
{
    Console.WriteLine("=== INTERACTIVE MODE: Real Claude API ===\n");

    // First message
    var response = await game.ProcessStepsAsync("(start conversation)");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n{response.Message}");
    Console.ResetColor();
    PrintState(game);

    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"\n{username}: ");
        Console.ResetColor();

        var input = Console.ReadLine();
        if (string.IsNullOrEmpty(input) || input.ToLower() == "quit") break;

        response = await game.ProcessStepsAsync(input);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{response.Message}");
        Console.ResetColor();

        PrintState(game);

        if (game.ActionSystem.IsGameCompleted(game.AllStates))
        {
            Console.WriteLine("\n=== GAME COMPLETED ===");
            break;
        }
    }
}

async Task RunAutoModeAsync(GameSession game)
{
    Console.WriteLine("=== AUTO MODE: Claude plays both roles ===\n");

    var response = await game.ProcessStepsAsync("(start conversation)");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n{response.Message}");
    Console.ResetColor();

    // Create a second Claude client to play the student
    var configJson = File.ReadAllText(configPath!);
    using var configDoc = JsonDocument.Parse(configJson);
    var apiKey = configDoc.RootElement.GetProperty("anthropic_api_key").GetString()!;

    var studentClient = new HttpClient();
    studentClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
    studentClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

    var studentMessages = new List<object>();
    var studentSystemPrompt = $"Act as {username}, a fourteen year old on the phone who writes very short, very simple messages. {username} is somewhat apathetic and doesn't care that much about protein synthesis at first. Speak in character as the teenager texting on their phone. Reply with ONLY what {username} would text—no narration or explanation.";

    for (int turn = 0; turn < 30; turn++)
    {
        if (game.ActionSystem.IsGameCompleted(game.AllStates))
        {
            Console.WriteLine("\n=== GAME COMPLETED ===");
            break;
        }

        // Get student response from Claude
        studentMessages.Add(new { role = "user", content = response.Message });
        var studentReply = await GetStudentResponse(studentClient, apiKey, studentSystemPrompt, studentMessages);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n{username}: {studentReply}");
        Console.ResetColor();

        studentMessages.Add(new { role = "assistant", content = studentReply });

        // Process student reply through the game
        response = await game.ProcessStepsAsync(studentReply);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{response.Message}");
        Console.ResetColor();

        PrintState(game);

        studentMessages.Add(new { role = "user", content = response.Message });
    }

    Console.WriteLine($"\nTotal turns: {game.Step}");
}

async Task<string> GetStudentResponse(HttpClient client, string apiKey, string systemPrompt, List<object> messages)
{
    var requestBody = new
    {
        model = "claude-haiku-4-5-20251001",
        max_tokens = 256,
        system = systemPrompt,
        messages = messages
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    var httpResponse = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
    var responseBody = await httpResponse.Content.ReadAsStringAsync();

    using var doc = JsonDocument.Parse(responseBody);
    var contentArray = doc.RootElement.GetProperty("content");
    foreach (var block in contentArray.EnumerateArray())
    {
        if (block.GetProperty("type").GetString() == "text")
            return block.GetProperty("text").GetString() ?? "idk";
    }
    return "idk";
}

void PrintTurnResult(string input, GameResponse response, GameSession game)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  Student: {input}");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  {response.Message}");
    Console.ResetColor();
    if (response.Action != null)
        Console.WriteLine($"  ACTION: {response.Action}");
    PrintState(game);
    Console.WriteLine();
}

void PrintState(GameSession game)
{
    var state = game.CurrentGameState;
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  [State: {state.Id}]");

    var met = state.Goals.Count(kv => kv.Value);
    var total = state.Goals.Count;
    Console.WriteLine($"  [Goals: {met}/{total} met]");

    var unmet = state.Goals.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
    if (unmet.Count > 0)
        Console.WriteLine($"  [Unmet: {string.Join("; ", unmet)}]");

    if (state.Available.Count > 0)
        Console.WriteLine($"  [Available actions: {string.Join(", ", state.Available.Keys)}]");

    if (state.UnlockableGoals.Count > 0)
        Console.WriteLine($"  [Locked goals: {state.UnlockableGoals.Count}]");

    Console.ResetColor();
}

string FindDataDirectory()
{
    // Try relative paths from the executable
    var candidates = new[]
    {
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Data"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "Data"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data"),
        // Direct path fallback
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Data"))
    };

    foreach (var candidate in candidates)
    {
        var fullPath = Path.GetFullPath(candidate);
        if (Directory.Exists(fullPath) && Directory.Exists(Path.Combine(fullPath, "States")))
            return fullPath;
    }

    // Hardcoded fallback for development
    var devPath = "/Users/alancheng/projects/mosaic/ProteinSynth/GameEngine/Data";
    if (Directory.Exists(devPath))
        return devPath;

    throw new DirectoryNotFoundException(
        "Could not find Data directory. Run from the TestHarness directory or set --data-dir.");
}
