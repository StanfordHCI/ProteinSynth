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
    [SerializeField] float avatarSpacing = 1400f; // spacing between avatars
    private bool isAnimating = false;
    private float initialOffset = 0f;

    private string[] names = {
        "Yari",
        "Alex",
        "Jessica", 
        "Benji",
        "Isaiah",
        "Maya",
    };

    private GlobalInMemoryVariableStorage storage; // syncing with Yarn

    private string[] descriptions = {
        "A vibrant 10th grader who proudly identifies as Afro-Latina, with Dominican and Puerto Rican roots. She's charismatic and can make any dry biology lesson come alive!",

        "A thoughtful 10th grader who reps his roots as a first-gen Mexican American. He doesn’t talk a lot during lessons, but when it comes time to help a classmate he’s all in.",

        "A thoughtful 10th grader who confidently reps her South Korean roots. She remembers feeling lost at first, so now she makes sure no one else feels left behind.",
        
        "A lively 10th grader who proudly wears his Singaporean heritage on his sleeve. His energy pulls everyone in, and he’s on a mission to make learning feel like an epic team project.",

        "A humble 11th grader who proudly identifies as Black. He's a great listener and a natural leader.", 

        "A stylish 12th grader who identifies as African American. She loves breaking down science concepts and sharing her knowledge with others.", 

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
        
        // Randomize the character order (shuffle names, descriptions, and avatars together)
        ShuffleCharacters();
        
        // Calculate initial offset based on container's starting position
        // This centers the first avatar (index 0)
        initialOffset = containerRect.anchoredPosition.x;
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
        if (index < avatars.Count - 1) {
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
        // Calculate end position dynamically based on index and spacing
        Vector2 endPos = new Vector2(initialOffset - (index * avatarSpacing), startPos.y);

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

    /// <summary>
    /// Sets the peer tutor by name. Useful for dropdown integration.
    /// Updates both the carousel display and the Yarn variable.
    /// </summary>
    /// <param name="tutorName">The name of the peer tutor to select</param>
    public void SetPeerTutorByName(string tutorName)
    {
        // Find the index of the tutor name
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == tutorName)
            {
                index = i;
                changeAvatarChoice();
                return;
            }
        }

        Debug.LogWarning($"AvatarSelection: Tutor name '{tutorName}' not found in names array.");
    }

    // Shuffle the character order lists and reorder children options in UI accordingly
    private void ShuffleCharacters()
    {
        if (names.Length != descriptions.Length || names.Length != avatars.Count)
        {
            Debug.LogError("Names, descriptions, and avatars must have the same length!");
            return;
        }

        for (int i = names.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            
            // Swap names
            string tempName = names[i];
            names[i] = names[randomIndex];
            names[randomIndex] = tempName;
            
            // Swap descriptions (keep them paired with names)
            string tempDesc = descriptions[i];
            descriptions[i] = descriptions[randomIndex];
            descriptions[randomIndex] = tempDesc;
            
            GameObject tempAvatar = avatars[i];
            avatars[i] = avatars[randomIndex];
            avatars[randomIndex] = tempAvatar;
        }

        // Reorder the child GameObjects in the scene hierarchy to match the shuffled order
        for (int i = 0; i < avatars.Count; i++)
        {
            avatars[i].transform.SetSiblingIndex(i);
        }

        avatars.Clear();
        foreach (Transform child in containerRect)
        {
            avatars.Add(child.gameObject);
        }
    }
}
