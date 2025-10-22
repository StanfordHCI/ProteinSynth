using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using Yarn.Unity;


public class LastLineScroll : MonoBehaviour
{
    private Queue<string> tempQueue = new Queue<string>(); 
    private List<string> messageList; 
    private int index; 
    private Dictionary<string, Sprite> portraitsDict;
    private Sprite portrait; 

    [SerializeField] TextMeshProUGUI lastLineText; 
    [SerializeField] TextMeshProUGUI lastLineName; 
    [SerializeField] Image lastLineImage; 

    [SerializeField] GameObject incrementButton; 
    [SerializeField] GameObject decrementButton; 

    private void Awake() {
        portraitsDict = new Dictionary<string, Sprite>(); 
        
        // Loading in character portrait sprites from Resources folder.
        Sprite[] sprites = Resources.LoadAll<Sprite>("portraits/");
        foreach (Sprite s in sprites)
        {
            portraitsDict[s.name] = s;
        }
        Debug.Log("Loaded portraits for last line scroll"); 
    }

    private void Start() {
        messageList = new List<string>(); 
    }

    // Get list of messages of this section 
    public void SetMessageList(Queue<string> messageQueue) {
        Debug.Log("clearing messageList"); 
        messageList.Clear();
        if (messageQueue != null) {
            while (messageQueue.Count > 0) {
                tempQueue.Enqueue(messageQueue.Dequeue()); 
            }
            while (tempQueue.Count > 0) {
                string thisLine = tempQueue.Dequeue(); 
                // if (!thisLine.Contains("VISUAL")) {
                messageList.Add(thisLine); 
                // }
                messageQueue.Enqueue(thisLine); 
            }
            // Get index of last message
            if (messageList.Count > 0) {
                index = messageList.Count - 1; 
                Debug.Log("index of the last message is " + index.ToString()); 
            }  

            hideScroll(false); // Show scroll buttons
            Debug.Log("finished setting messageList, disabling increment button"); 
        }
    }

    // Scroll forwards to the next message, up to last line
    public void incrementScroll() {        
        if (index + 1 < messageList.Count) {
            index += 1; 
            displayThisLine(); 
        } else {
            Debug.Log("cannot increment, already at last message");
        }
    }

    // Scroll backwards to the last message, up to first line
    public void decrementScroll() {
        if (index - 1 >= 0) {
            index -= 1; 
            displayThisLine();
        } else {
            Debug.Log("cannot decrement, already at first message");
        }
    }

    private void displayThisLine() {       
        string responseLine = messageList[index]; 
        Debug.Log("displaying line " + index + " of " + (messageList.Count - 1) + ": " + responseLine);
        // Parse out elements of string
        var lineParts = responseLine.Split(":", 2); 
        string thisName = lineParts[0]; 
        string thisLine = lineParts[1]; 

        lastLineText.GetComponent<TextMeshProUGUI>().text = thisLine; 
        lastLineName.GetComponent<TextMeshProUGUI>().text = thisName; 
        if (thisName != null) {
            if (portraitsDict.TryGetValue(thisName, out portrait)) {
                lastLineImage.GetComponent<Image>().sprite = portrait; 
            }
        } 

        // Disable buttons when they reach very beginning or very end of messages, otherwise enable
        if (index == messageList.Count - 1) {
            incrementButton.GetComponent<CanvasGroup>().alpha = 0.05f;
            incrementButton.GetComponent<Button>().enabled = false;
        } else {
            incrementButton.GetComponent<CanvasGroup>().alpha = 1;
            incrementButton.GetComponent<Button>().enabled = true;
        }
        if (index == 0) {
            decrementButton.GetComponent<CanvasGroup>().alpha = 0.05f;
            decrementButton.GetComponent<Button>().enabled = false;
        } else {
            decrementButton.GetComponent<CanvasGroup>().alpha = 1;
            decrementButton.GetComponent<Button>().enabled = true;
        }
    }

    [YarnCommand("hide_scroll")]
    public void hideScroll(bool hidden) {
        if (hidden) {
            MainThreadDispatcher.Enqueue(() => decrementButton.GetComponent<CanvasGroup>().alpha = 0f); 
            MainThreadDispatcher.Enqueue(() => decrementButton.GetComponent<Button>().enabled = false); 
            MainThreadDispatcher.Enqueue(() => incrementButton.GetComponent<CanvasGroup>().alpha = 0f); 
            MainThreadDispatcher.Enqueue(() => incrementButton.GetComponent<Button>().enabled = false); 
        } else {
            MainThreadDispatcher.Enqueue(() => decrementButton.GetComponent<CanvasGroup>().alpha = 1.0f); 
            MainThreadDispatcher.Enqueue(() => decrementButton.GetComponent<Button>().enabled = true); 
            MainThreadDispatcher.Enqueue(() => incrementButton.GetComponent<CanvasGroup>().alpha = 0.05f); 
            MainThreadDispatcher.Enqueue(() => incrementButton.GetComponent<Button>().enabled = false); 
        }
    }
}