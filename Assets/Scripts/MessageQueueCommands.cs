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
        yield return new WaitUntil(() => messagesQueue.Count > 0);
    }
}

