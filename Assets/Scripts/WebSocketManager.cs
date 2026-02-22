using UnityEngine;
using UnityEngine.UI;
// using System.Collections.Concurrent;
using System; 
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Yarn.Unity; 
using Yarn.Compiler; 
using static System.String;

using OpenAI;

// IMPORTANT: This interface should match the response passed down from
// the server.
[System.Serializable]
public class ResponseData
{
    public string action;
    public string message;
    public string next_state_id;
    public string[] next_state_characters;
    public string image_to_display;
    public string participant_id; 
}

public class AudioData 
{
    public string type; 
    public string text; 
    public string audio; 
}

// TODO: not sure if this should be a MonoBehaviour
public class WebSocketManager : MonoBehaviour {
    private static WebSocketManager instance = null;

    public static WebSocketManager Instance {
        get {
            return instance;
        }
    }

    private SocketConnection socketConnection;

    // Queue of messages to be run through dialogue runner
    private Queue<string> yarnQueue;  // text
    private Queue<AudioClip> audioQueue;  // corresponding audio 
    private string lastState; 
    private string[] lastStateCharacters; 
    public bool acceptResponse;

    private int seenImage = 0; 
    private int introMessageCount = 0; 
    private bool displayedLogTutorial = false; 
    private bool displayedAlbumTutorial = false; 
    private bool getGeneratedPid = false; 

    // Change characters + background for state changes
    // public TwoCharacterDisplay twoCharDisplay; 
    bool nextChangeBackground = false;  
    string condition = ""; 

    // Let user scroll through all messages in this section in last line container
    private LastLineScroll lastLineScroll; 

    private void Start() {
        instance = this;

        socketConnection = GetComponent<SocketConnection>();
        socketConnection.OnMessageReceived += HandleResponse;
        yarnQueue = GetComponent<MessageQueueCommands>().messagesQueue;
        audioQueue= GetComponent<MessageQueueCommands>().audioQueue;
        acceptResponse = true; 
        condition = GetComponent<SocketConnection>().condition; 
        lastLineScroll = GetComponent<LastLineScroll>(); 

        DontDestroyOnLoad(this.gameObject);
        // socketConnection.SendMessageToServer("Hello!");
    }

    private void HandleResponse(string response) {
        // check if this message should be ignored 
        if (acceptResponse == false) {
            acceptResponse = true; 
            return; 
        }

        // If this is an audio packet, process it so it can played 
        if (response.Contains("tts_chunk")) {
            AudioData audioData = JsonUtility.FromJson<AudioData>(response);
            processAudio(audioData.audio);
            return;
        }

        // Parse the JSON string to an object
        ResponseData responseData = JsonUtility.FromJson<ResponseData>(response);
        Debug.Log("From Json, action = " + responseData.action + "and message = " + responseData.message + "and next state = " + responseData.next_state_id + "and image_to_display is" + responseData.image_to_display); 

        // If server sent nothing, try resending player message
        if (responseData == null || IsNullOrWhiteSpace(responseData.message) || !responseData.message.Contains("::")) {
            SendPlayerMessage("$playerResponse");
            return;  
        }

        // Save generated participant id if necessary
        processPid(responseData.participant_id); 
        // Pull up visual aid if necessary
        processImage(responseData.image_to_display); 
        // Change background and characters if state has changed
        processState(responseData.next_state_id, responseData.next_state_characters, responseData);
        // Process message for dialogue runner
        processMessage(responseData.message, responseData.next_state_id); 
        // Add an action if necessary
        processAction(responseData.action); 
        Debug.Log("Final queue of responses: " + String.Join(" -> ", yarnQueue.ToArray()));
    }

    private void processMessage(string responseText, string next_state_id) {
        string line = responseText; 
        string name = "???";

        // Extract lines from response 
        var rawLines = responseText.Split(new string[] {"\n\n", "\n"}, StringSplitOptions.RemoveEmptyEntries); 


        List<string> responseLines = new List<string>(); 
        for (int i = 0; i < rawLines.Length; i++) {
            string thisLine = rawLines[i]; 
            int colonCount = Regex.Matches(thisLine, "::").Count;
            
            // if there are multiple lines in one, parse them out + add them individually
            if (colonCount > 1) {
                List<int> indices = new List<int>(); 
                int startIndex = 0;
                for (int j = 0; j < colonCount; j++) {
                    startIndex = thisLine.IndexOf("::", startIndex);
                    while (!Char.IsPunctuation(thisLine[startIndex])) {
                        startIndex--; 
                    }
                    indices.Add(startIndex + 1); // add index of start of this line
                }
                indices.Add(thisLine.Length - 1); // add index for end of this line 
                for (int k = 0; k < indices.Count; k++) {
                    responseLines.Add(thisLine.Substring(indices[k], indices[k + 1] - indices[k])); 
                }
            } else {
                responseLines.Add(thisLine); // if not multiple lines to parse, just add line directly
            }
        }

        List<string> gptOptions = new List<string>();

        // Parse each line into sentences and enqueue for Yarnspinner
        foreach (string responseLine in responseLines) {
            // Handle option 
            if (responseLine.StartsWith("->")) {
                line = responseLine.Substring(2).Trim(); 
                gptOptions.Add(line);  
                continue;  
            }
            
            // Handle dialogue line 
            if (responseLine.Contains("::")) {
                var lineParts = responseLine.Split("::", 2); 
                name = lineParts[0].Trim() + ": "; 
                line = lineParts[1]; 
                Debug.Log("Name:" + name + ", Line:" + line);
            } 
            // If there's no ::, assume it's the same speaker
            else {
                line = responseLine;
            }

            string[] sentences = Regex.Split(line, @"(?:(?<=[\.!\?][""])|(?<=[\.!\?]))\s+"); 
            foreach (string sentence in sentences) {
                // Only queue up non-empty strings
                if (sentence != "") {
                    string dialogueLine = name + sentence; 
                    yarnQueue.Enqueue(dialogueLine); 
                    Debug.Log("Enqueued message for the Yarn queue: " + dialogueLine); 
                }
            }
            Debug.Log("Queued up all sentences in Yarn queue for " + name); 
        }

        Debug.Log("Enqueue set message list on main thread"); 
        lastLineScroll.SetMessageList(yarnQueue); 
        
        // Run photo album tutorial after the first photo has been shown
        // if (seenImage == 1 && yarnQueue.Peek() == "VISUAL") { 
        //     if (displayedAlbumTutorial == false) {
        //         Queue<string> temp = new Queue<string>(); 
        //         while (yarnQueue.Count > 1) {
        //             temp.Enqueue(yarnQueue.Dequeue()); 
        //         }
        //         string lastQuestion = yarnQueue.Dequeue(); 
        //         while (temp.Count > 0) {
        //             yarnQueue.Enqueue(temp.Dequeue()); 
        //         }

        //         yarnQueue.Enqueue(lastQuestion); 
        //         yarnQueue.Enqueue("ALBUM_TUTORIAL"); 
        //         yarnQueue.Enqueue(lastQuestion); 
        //         displayedAlbumTutorial = true; 
        //     }
        // }
        
        // Run log tutorial as 1st line of 2nd message in state 0_intro
        // if (next_state_id == "0_intro" && introMessageCount == 2) {
        //     if (displayedLogTutorial == false) {
        //         Queue<string> temp = new Queue<string>(); 
        //         temp.Enqueue("LOG_TUTORIAL"); 
        //         while (yarnQueue.Count > 0) {
        //             temp.Enqueue(yarnQueue.Dequeue()); 
        //         }
        //         while (temp.Count > 0) {
        //             yarnQueue.Enqueue(temp.Dequeue()); 
        //         }
        //         displayedLogTutorial = true; 
        //     }
        // }

        Debug.Log("Options: " + String.Join("\n", gptOptions)); 
        // Load in options after all messages if there are any
        if (gptOptions.Count > 0) {
            for (int i = 0; i < gptOptions.Count; i++) {
                GlobalInMemoryVariableStorage.Instance.SetValue("$gptOption" + (i + 1).ToString(), gptOptions[i]); 
                Debug.Log("Option" + (i + 1).ToString() + "is " + gptOptions[i]); 
            }
        }
    }

    private void processState(string next_state_id, string[] next_state_characters, ResponseData responseData) {
        MainThreadDispatcher.Enqueue(() => condition = socketConnection.condition); 
        Debug.Log("processing state, current condition is " + condition); 
        if (nextChangeBackground == true) {
            // MainThreadDispatcher.Enqueue(() => twoCharDisplay.ChangeBackgroundScene(lastState, lastStateCharacters)); 
            nextChangeBackground = false; 
        }
        if (next_state_id != lastState) {
            // Don't show continue button instead of text box for the first prompt in the game 
            if (lastState != null) {
                // TODO: This is super hacky. Inject a single option during a state change so
                // the player doesn't have to type "yes" everytime
                responseData.message = responseData.message + "\n-> Okay!";
            }

            lastState = next_state_id;
            lastStateCharacters = next_state_characters;  
            nextChangeBackground = true; 

        } else {
            nextChangeBackground = false; 
        }

        // Track how many intro messages for running the log tutorial
        if (next_state_id == "0_intro") {
            introMessageCount += 1; 
        }
    }
    
    private void processAction(string action) {
        string nodeName = ""; 
        switch (action) 
        {
            case "REQUEST_PIC_FROM_STUDENT": 
                nodeName = "TakePhoto"; 
                break; 
            case "MAGIC_PORTAL":
                nodeName = "Portal";
                break;
            case "LEAVE_PORTAL":
                nodeName = "ExitPortal";
                break;
            case "COLLECT_ACORNS":
                nodeName = "SquirrelAR";
                break;
            case "REHOME_TINA":
                nodeName = "WoodpeckerAR";
                break;
            case "PLANT_OAK": 
                nodeName = "OakLifeAR1";
                break;
            case "END_GAME": 
                nodeName = "RollCredits";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB":
                nodeName = "ProteinSynthesisLab";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_INSULIN":
                nodeName = "ProteinSynthesisLabInsulin";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_LACTASE":
                nodeName = "ProteinSynthesisLabLactase";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_HEMOGLOBIN":
                nodeName = "ProteinSynthesisLabHemoglobin";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_MYOSIN":
                nodeName = "ProteinSynthesisLabMyosin";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_KERATIN":
                nodeName = "ProteinSynthesisLabKeratin";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_IMMUNOGLOBULINS":
                nodeName = "ProteinSynthesisLabImmunoglobulins";
                break;
            case "TO_PROTEIN_SYNTHESIS_LAB_CYTOKINES":
                nodeName = "ProteinSynthesisLabTyrosinase";
                break;
            case "ENCOURAGE_STUDENT_AND_BID_THEM_FAREWELL":
                nodeName = "EndGame";
                break;
        }

        if (nodeName != "") {
            yarnQueue.Enqueue("ACTION"); 
            GlobalInMemoryVariableStorage.Instance.SetValue("$actionNode", nodeName); 
        }
    }

    private void processImage(string image_to_display) {
        if (image_to_display != null && image_to_display != "") {
            yarnQueue.Enqueue("VISUAL"); 
            GlobalInMemoryVariableStorage.Instance.SetValue("$image_to_display", image_to_display);
            seenImage += 1; 
        }
    }

    private void processPid(string pid) {
        if (getGeneratedPid && !IsNullOrWhiteSpace(pid)) {
            GlobalInMemoryVariableStorage.Instance.SetValue("$participant_id", pid);
        }
    }

    private static int audioChunkCount = 0; // Track chunk count for debugging

    private void processAudio(string base64String) {
        try
        {
            audioChunkCount++;
            Debug.Log($"Processing audio chunk #{audioChunkCount}");
            
            // Decode the Base64 string to raw byte data
            byte[] audioData = Convert.FromBase64String(base64String);
            
            // Validate decoded data
            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError("Failed to decode base64 audio data or data is empty");
                return;
            }

            Debug.Log($"Decoded audio data: {audioData.Length} bytes");
            
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    AudioClip clip = WavUtility.ToAudioClip(audioData, $"tts_line_{audioChunkCount}");
                    audioQueue.Enqueue(clip);
                    Debug.Log($"Successfully queued audio clip: {clip.name} (Duration: {clip.length:F2}s, Total audio clips in queue: {audioQueue.Count})");
                }
                catch (Exception e)
                {
                    Debug.LogError("Error creating AudioClip: " + e);
                }
            });
        }
        catch (FormatException ex)
        {
            Debug.LogError($"Invalid base64 format: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing audio: {ex.Message}");
        }
    }

    // Sends player input to server
    [YarnCommand("send_player_message")]
    public void SendPlayerMessage(string yarnResponseVar) { 
        string playerResponse;
        GlobalInMemoryVariableStorage.Instance.TryGetValue(yarnResponseVar, out playerResponse);
        Debug.Log("Player's response: " + playerResponse); 

        socketConnection.SendMessageToServer(playerResponse); 
    }

    // Generate audio for a hard-coded line
    [YarnCommand("generate_audio")]
    public void GenerateAudio(string line) { 
        // Only proceed if server is connected
        if (socketConnection != null && socketConnection.condition == "Connected")
        {
            // Manually construct JSON string
            string jsonMessage = "{\"message\":\"" + "AUDIO:" + line + "\"}";
            Debug.Log("Sending JSON: " + jsonMessage); 
            socketConnection.SendMessageToServer(jsonMessage); 
            GetComponent<MessageQueueCommands>().WaitForAudio(); 
        }
        else
        {
            Debug.Log("Server not connected — skipping audio generation.");
        }
    }

    // Sends first message containing username + initial state to server
    [YarnCommand("send_first_message")]
    public void SendFirstMessage(string usernameVar, string initialStateVar, string participantIdVar, string conditionVar, string gradeVar, string tutorVar) { 
        string user, state, pid, condition, grade, peer_tutor;
        GlobalInMemoryVariableStorage.Instance.TryGetValue(usernameVar, out user);
        
        Debug.Log("Username variable: " + usernameVar);
        Debug.Log("Username: " + user);

        // Trim and remove new line characters from name input by user
        user = user.Replace("\n", String.Empty).Replace("\t", String.Empty).Replace("\r", String.Empty).Trim();
        GlobalInMemoryVariableStorage.Instance.SetValue($"{usernameVar}", user);

        GlobalInMemoryVariableStorage.Instance.TryGetValue(initialStateVar, out state);
        GlobalInMemoryVariableStorage.Instance.TryGetValue(participantIdVar, out pid);
        if (IsNullOrWhiteSpace(pid)) {
            pid = "";
            getGeneratedPid = true; 
        }

        GlobalInMemoryVariableStorage.Instance.TryGetValue(conditionVar, out condition);
        GlobalInMemoryVariableStorage.Instance.TryGetValue(gradeVar, out grade);
        GlobalInMemoryVariableStorage.Instance.TryGetValue(tutorVar, out peer_tutor);

        Debug.Log("Initial message - User: " + user + ", Initial state: " + state + ", Participant ID: " + pid + "Condition: " + condition + ", Grade: " + grade + ", Peer tutor: " + peer_tutor); 

        socketConnection.SendFirstMessageToServer(user, state, pid, condition, grade, peer_tutor); 
    }

    public void SendImage(byte[] bytes) {
        Debug.Log("in SendImage!");
        socketConnection.SendBinaryToServer(bytes); 
    }
}