using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using Yarn.Unity; 
using Yarn.Compiler; 

using OpenAI;

public class MessageQueueCommands : MonoBehaviour
{
    public Queue<string> messagesQueue = new Queue<string>(); // queue of message strings
    public Queue<AudioClip> audioQueue= new Queue<AudioClip>(); // list of corresponding audio files
    public AudioSource audioSource;

     // runs messages currently in queue through dialogue
    [YarnCommand("run_response")]
    public void RunResponse() {
        Debug.Log("Messages left in queue: " + messagesQueue.Count.ToString()); 
        
        string gptResponse = ""; 
    
        // reset gptResponse to empty to signal that all messages have been said
        if (messagesQueue.Count == 0) {
            GlobalInMemoryVariableStorage.Instance.SetValue("$gptResponse", "");
            return; 
        }
        
        gptResponse = messagesQueue.Dequeue();
        
        // TO-DO: make this more general, rn assumes the question/call-to-action is somewhere 
        // in the last 2 sentences and that last 2 sentences are said by same character
        if (messagesQueue.Count == 1) {
            // combine last 2 sentences if question is in 2nd to last sentence
            // if (gptResponse.Contains('?')) {
                string lastSentence = messagesQueue.Dequeue();
                // EXCEPTION: if we need to the run the album tutorial
                if (lastSentence == "ALBUM_TUTORIAL") {
                    messagesQueue.Enqueue(lastSentence); 
                }
                // Only combine if together they are less than 200 characters
                // else if ((gptResponse + lastSentence).Length <= 200 && gptResponse[0] == lastSentence[0]) {
                //     lastSentence = lastSentence.Substring(lastSentence.IndexOf(':') + 1); // remove name from last sentence
                //     gptResponse += lastSentence; 
                // }
                
                 else {
                    messagesQueue.Enqueue(lastSentence); 
                }
            }
        GlobalInMemoryVariableStorage.Instance.SetValue("$gptResponse", gptResponse);
    }

    // waits for messages to populate queue to continue dialogue runner
    [YarnCommand("wait_for_message")]
    public IEnumerator WaitForMessage() {
        yield return new WaitUntil(() => messagesQueue.Count > 0 && audioQueue.Count > 0);
        Debug.Log($"Ready to process: {messagesQueue.Count} text messages, {audioQueue.Count} audio clips");
    }

    // waits for audio chunk 
    [YarnCommand("wait_for_audio")]
    public IEnumerator WaitForAudio() {
        yield return new WaitUntil(() => audioQueue.Count > 0);
    }

    [YarnCommand("play_voiceover")]
    public IEnumerator PlayVoiceover() {
        Debug.Log($"Audio queue count: {audioQueue.Count}, Messages queue count: {messagesQueue.Count}");
        
        // Check if queues are synchronized
        if (audioQueue.Count != messagesQueue.Count) {
            Debug.LogWarning($"Queue mismatch! Audio: {audioQueue.Count}, Text: {messagesQueue.Count}");
        }
        
        if (audioQueue.Count == 0) {
            Debug.LogWarning("No audio clips in queue!");
            yield break;
        }

        AudioClip clip = audioQueue.Dequeue();
        Debug.Log($"Playing audio clip: {clip.name}, Duration: {clip.length:F2}s");
        
        if (clip != null) {
            audioSource.clip = clip;
            audioSource.Play();
            
            // Don't wait here - let the text display immediately while audio plays
            // The Yarn system will handle the timing
            Debug.Log($"Started playing: {clip.name}");
        }
    }
}

