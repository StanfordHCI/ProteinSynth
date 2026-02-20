using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Yarn.Unity;

public class RetryLab : MonoBehaviour
{
    public enum ProteinType
    {
        Lactase = 0,
        Hemoglobin = 1,
        Insulin = 2,
        Myosin = 3,
        Keratin = 4,
        Immunoglobulins = 5,
        Tyrosinase = 6,
        Cytokines = 7,
    }

    [Header("Button References")]
    [Tooltip("Array of protein selection buttons. Order should match ProteinType enum (Lactase=0, Hemoglobin=1, etc.)")]
    [SerializeField] private Button[] proteinButtons = new Button[8];

    [Tooltip("The restart/start lab button. Will be disabled until a protein is selected.")]
    [SerializeField] private Button startButton;

    [Header("Visual Settings")]
    [Tooltip("Alpha value for unselected buttons (0-1). Lower = more greyed out")]
    [Range(0f, 1f)]
    [SerializeField] private float unselectedAlpha = 0.5f;

    [Header("Peer Tutor Dropdown")]
    [Tooltip("Optional dropdown for selecting peer tutor. Drag and drop the dropdown UI component here.")]
    [SerializeField] private TMP_Dropdown peerTutorDropdown;

    [Tooltip("List of available peer tutor names. Should match the names used in Yarn.")]
    [SerializeField] private string[] peerTutorNames = { "Yari", "Alex", "Jessica", "Benji", "Isaiah", "Maya" };

    private ProteinType selectedProtein = ProteinType.Lactase;
    private GlobalInMemoryVariableStorage yarnStorage;

    private void Start()
    {
        // Update visual states on start
        UpdateButtonVisuals();

        // Get Yarn variable storage (like AvatarSelection does)
        yarnStorage = GlobalInMemoryVariableStorage.Instance;

        // Setup peer tutor dropdown if assigned
        if (peerTutorDropdown != null)
        {
            SetupPeerTutorDropdown();
        }
    }

    /// <summary>
    /// Called by buttons to set the selected protein.
    /// Assign this function to button OnClick events.
    /// </summary>
    /// <param name="proteinIndex">The index of the protein in the ProteinType enum (0-7)</param>
    public void SetSelectedProtein(int proteinIndex)
    {
        if (proteinIndex >= 0 && proteinIndex < System.Enum.GetValues(typeof(ProteinType)).Length)
        {
            selectedProtein = (ProteinType)proteinIndex;
            Debug.Log("Selected protein: " + selectedProtein);
            UpdateButtonVisuals();
        }
        else
        {
            Debug.LogWarning("Invalid protein index: " + proteinIndex);
        }
    }

    /// <summary>
    /// Alternative method to set protein by name (useful for direct string input)
    /// </summary>
    /// <param name="proteinName">Name of the protein (e.g., "Lactase", "Hemoglobin")</param>
    public void SetSelectedProteinByName(string proteinName)
    {
        if (System.Enum.TryParse<ProteinType>(proteinName, true, out ProteinType protein))
        {
            selectedProtein = protein;
            Debug.Log("Selected protein: " + selectedProtein);
            UpdateButtonVisuals();
        }
        else
        {
            Debug.LogWarning("Invalid protein name: " + proteinName);
        }
    }

    /// <summary>
    /// Starts the lab with the currently selected protein.
    /// Jumps to the protein-specific Yarn node which handles all prerequisites.
    /// </summary>
    public void StartLabWithSelectedProtein()
    {
        // Stop any currently running dialogue first
        GlobalDialogueManager.StopDialogue();

        // Get the protein-specific node name (e.g., "ProteinSynthesisLabLactase")
        string nodeName = GetYarnNodeName(selectedProtein);

        // Start the dialogue node (which handles scene loading, variable setting, and sequence setting)
        GlobalDialogueManager.StartDialogue(nodeName);
    }

    /// <summary>
    /// Gets the DNA sequence for the given protein type based on the Yarn file.
    /// </summary>
    private string GetSequenceForProtein(ProteinType protein)
    {
        switch (protein)
        {
            case ProteinType.Lactase:
                return "TACCCGGTGACCGAC";
            case ProteinType.Hemoglobin:
                return "TACCACGTGGACTGA";
            case ProteinType.Insulin:
                return "TACCTCGACAGAUGG";
            case ProteinType.Myosin:
                return "TACCACGTGCCGACC";
            case ProteinType.Keratin:
                return "TACCGGCCGGACACC";
            case ProteinType.Immunoglobulins:
                return "TACCTCCACGACTGA";
            case ProteinType.Tyrosinase:
                return "TACCCGGTGCTCAGA";
            case ProteinType.Cytokines:
                return "TACTGACACGACCCG";
            default:
                return "TACCCGGTGACCGAC"; // Default to Lactase
        }
    }

    /// <summary>
    /// Unloads the "Simulated Content Scene" and "Simulated Environment Scene" if they exist.
    /// </summary>
    private void UnloadSimulatedScenes()
    {
        string[] sceneNames = { "Simulated Content Scene", "Simulated Environment Scene" };
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                foreach (string sceneName in sceneNames)
                {
                    if (scene.name == sceneName)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                        Debug.Log("Unloaded " + sceneName);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the Yarn node name for the given protein type.
    /// </summary>
    private string GetYarnNodeName(ProteinType protein)
    {
        return "ProteinSynthesisLab" + protein.ToString();
    }

    /// <summary>
    /// Gets the currently selected protein.
    /// </summary>
    public ProteinType GetSelectedProtein()
    {
        return selectedProtein;
    }

    /// <summary>
    /// Toggles the visibility of the RetryLab GameObject.
    /// </summary>
    public void ToggleVisibility()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    /// <summary>
    /// Updates the visual state of all buttons based on selection.
    /// Selected button gets full opacity, others are greyed out.
    /// </summary>
    private void UpdateButtonVisuals()
    {
        int selectedIndex = (int)selectedProtein;

        for (int i = 0; i < proteinButtons.Length; i++)
        {
            if (proteinButtons[i] == null) continue;

            bool isSelected = (i == selectedIndex);
            UpdateButtonOpacity(proteinButtons[i], isSelected);
        }
    }

    /// <summary>
    /// Updates the opacity of a button and its children (Image, Backdrop, Text (TMP)) based on selection state.
    /// </summary>
    private void UpdateButtonOpacity(Button button, bool isSelected)
    {
        float targetAlpha = isSelected ? 1f : unselectedAlpha;

        // Update Image
        Transform imageTransform = button.transform.Find("Image");
        if (imageTransform != null)
        {
            Image imageComponent = imageTransform.GetComponent<Image>();
            if (imageComponent != null)
            {
                Color color = imageComponent.color;
                color.a = targetAlpha;
                imageComponent.color = color;
            }
        }

        // Update Backdrop
        Transform backdropTransform = button.transform.Find("Backdrop");
        if (backdropTransform != null)
        {
            Image backdropImage = backdropTransform.GetComponent<Image>();
            if (backdropImage != null)
            {
                Color color = backdropImage.color;
                color.a = targetAlpha;
                backdropImage.color = color;
            }
        }

        // Update Text (TMP)
        Transform textTransform = button.transform.Find("Text (TMP)");
        if (textTransform != null)
        {
            // Try TextMeshProUGUI first (since it's named "Text (TMP)")
            TMPro.TextMeshProUGUI tmpText = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                Color textColor = tmpText.color;
                textColor.a = targetAlpha;
                tmpText.color = textColor;
            }
            else
            {
                // Fallback to regular Text component
                Text textComponent = textTransform.GetComponent<Text>();
                if (textComponent != null)
                {
                    Color textColor = textComponent.color;
                    textColor.a = targetAlpha;
                    textComponent.color = textColor;
                }
            }
        }
    }

    /// <summary>
    /// Sets up the peer tutor dropdown with options and event listeners.
    /// </summary>
    private void SetupPeerTutorDropdown()
    {
        if (peerTutorDropdown == null)
        {
            Debug.LogWarning("RetryLab: Peer tutor dropdown is null!");
            return;
        }

        // Create dropdown options
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        // Add all peer tutor names
        foreach (string tutorName in peerTutorNames)
        {
            options.Add(new TMP_Dropdown.OptionData(tutorName));
        }

        // Set options
        peerTutorDropdown.options = options;

        // Add listener for when selection changes
        peerTutorDropdown.onValueChanged.AddListener(OnPeerTutorDropdownChanged);

        // Set initial value from Yarn if available
        if (yarnStorage != null)
        {
            string currentTutor = "";
            if (yarnStorage.TryGetValue("$peer_tutor", out currentTutor))
            {
                SetSelectedPeerTutor(currentTutor);
            }
        }
    }

    /// <summary>
    /// Called when the peer tutor dropdown value changes.
    /// Updates the $peer_tutor variable in Yarn (same pattern as AvatarSelection).
    /// </summary>
    /// <param name="index">The index of the selected option</param>
    private void OnPeerTutorDropdownChanged(int index)
    {
        if (index >= 0 && index < peerTutorNames.Length && yarnStorage != null)
        {
            // Set the Yarn variable directly, just like AvatarSelection does
            yarnStorage.SetValue("$peer_tutor", peerTutorNames[index]);
        }
    }

    /// <summary>
    /// Sets the dropdown to show the specified tutor name.
    /// </summary>
    /// <param name="tutorName">The name of the tutor to select</param>
    private void SetSelectedPeerTutor(string tutorName)
    {
        if (peerTutorDropdown == null) return;

        // Find the index of the tutor name
        for (int i = 0; i < peerTutorNames.Length; i++)
        {
            if (peerTutorNames[i] == tutorName)
            {
                peerTutorDropdown.value = i;
                return;
            }
        }

        Debug.LogWarning($"RetryLab: Tutor name '{tutorName}' not found in peerTutorNames array.");
    }
}