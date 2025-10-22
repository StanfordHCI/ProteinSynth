using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using Yarn.Unity;

public class AvatarSelection : MonoBehaviour
{
    private int index = 0; 
    private List<GameObject> avatars = new List<GameObject>();

    [SerializeField] GameObject characterOptions; 
    [SerializeField] TextMeshProUGUI nameText; 
    [SerializeField] TextMeshProUGUI descriptionText; 
    
    private RectTransform containerRect;
    [SerializeField] float scrollDuration = 0.05f; // seconds
    private bool isAnimating = false;

    private string[] names = {
        "Yari",
        "Alex",
        "Jessica", 
        "Benji",
    };

    private GlobalInMemoryVariableStorage storage; // syncing with Yarn

    private string[] descriptions = {
        "A vibrant 9th grader who proudly identifies as Afro-Latina, with Dominican and Puerto Rican roots. She's charismatic and can make any dry biology lesson come alive!",

        "A thoughtful 9th grader who reps his roots as a first-gen Mexican American. He doesn’t talk a lot during lessons, but when it comes time to help a classmate he’s all in.",

        "A thoughtful 9th grader who confidently reps her South Korean roots. She remembers feeling lost at first, so now she makes sure no one else feels left behind.",
        
        "A lively 9th grader who proudly wears his Singaporean heritage on his sleeve. His energy pulls everyone in, and he’s on a mission to make learning feel like an epic team project.",

    };


    [SerializeField] GameObject incrementButton; 
    [SerializeField] GameObject decrementButton; 

    void Awake()
    {
        // Get the avatars from the scene
        containerRect = characterOptions.GetComponent<RectTransform>();
        foreach (Transform child in containerRect) {
                avatars.Add(child.gameObject);
        }     
    }

    void Start()
    {
        storage = GlobalInMemoryVariableStorage.Instance;
        changeAvatarChoice();   
    }
    
    public void changeAvatarChoice() {
        // Update carousel to show this avatar
        StartCoroutine(AnimateScroll());

        // Change text content to match next avatar
        nameText.text = names[index];
        descriptionText.text = descriptions[index];

        // Update character in Yarn
        storage.SetValue("$peer_tutor", names[index]);

        // Highlight this avatar in the carousel 
        for (int i = 0; i < avatars.Count; i++) {
            CanvasGroup group = avatars[i].GetComponent<CanvasGroup>();
            if (i != index)
            {
                group.alpha = 0.2f;
            }
            else
            {
                group.alpha = 1.0f;
            }
        }

        // Disable buttons when they reach very beginning or very end of avatar list
        if (index == avatars.Count - 1) {
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

    // Scroll forwards to next avatar
    public void incrementScroll() {
        if (index < avatars.Count) {
            index += 1;
            changeAvatarChoice(); 
        }
    }

    // Scroll backwards to prev avatar
    public void decrementScroll() {
        if (index > 0) {
            index -= 1;
            changeAvatarChoice();
        }
    }

    IEnumerator AnimateScroll() {
        isAnimating = true;

        Vector2 startPos = containerRect.anchoredPosition;
        Vector2 endPos = new Vector2((-index * 1400) + 3010, startPos.y);

        float elapsed = 0f;
        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / scrollDuration);
            containerRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        containerRect.anchoredPosition = endPos;
        isAnimating = false;
    }
}
