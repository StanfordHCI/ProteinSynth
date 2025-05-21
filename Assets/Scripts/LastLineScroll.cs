using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

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
                if (!thisLine.Contains("VISUAL")) {
                    messageList.Add(thisLine); 
                }
                messageQueue.Enqueue(thisLine); 
            }
            // Get last index of last message
            if (messageList.Count > 0) {
                index = messageList.Count - 1; 
                Debug.Log("index of the last message is " + index.ToString()); 
            }
            // MainThreadDispatcher.Enqueue(() => incrementButton.GetComponent<CanvasGroup>().alpha = 0); 
            // MainThreadDispatcher.Enqueue(() => incrementButton.GetComponent<Button>().enabled = false); 
            Debug.Log("finished setting messageList"); 
        }
    }

    // Scroll forwards to the next message, up to last line
    public void incrementScroll() {
        Debug.Log("Attempting increment scroll");
        if (index + 1 < messageList.Count) {
            index += 1; 
            displayThisLine(); 
        }
    }

    // Scroll backwards to the last message, up to first line
    public void decrementScroll() {
        Debug.Log("Attempting decrement scroll");
        if (index - 1 >= 0) {
            index -= 1; 
            displayThisLine();
        }
    }

    private void displayThisLine() {
        string responseLine = messageList[index]; 
        Debug.Log("Incrementing scroll, showing next line " + responseLine);
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
            incrementButton.GetComponent<CanvasGroup>().alpha = 0;
            incrementButton.GetComponent<Button>().enabled = false;
        } else {
            incrementButton.GetComponent<CanvasGroup>().alpha = 1;
            incrementButton.GetComponent<Button>().enabled = true;
        }
        if (index == 0) {
            decrementButton.GetComponent<CanvasGroup>().alpha = 0;
            decrementButton.GetComponent<Button>().enabled = false;
        } else {
            decrementButton.GetComponent<CanvasGroup>().alpha = 1;
            decrementButton.GetComponent<Button>().enabled = true;
        }
    }
}