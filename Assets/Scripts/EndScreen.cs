using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndScreen : MonoBehaviour
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
    [SerializeField] private Button restartButton;

    [Header("Visual Settings")]
    [Tooltip("Alpha value for unselected buttons (0-1). Lower = more greyed out")]
    [Range(0f, 1f)]
    [SerializeField] private float unselectedAlpha = 0.5f;

    private ProteinType selectedProtein = ProteinType.Lactase;

    private void Start()
    {
        // Update visual states on start
        UpdateButtonVisuals();
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
    /// Resets the lab state, sets the new sequence, and starts the dialogue.
    /// </summary>
    public void StartLabWithSelectedProtein()
    {
        // Stop any currently running dialogue first
        GlobalDialogueManager.StopDialogue();

        // Get the sequence for the selected protein
        string sequence = GetSequenceForProtein(selectedProtein);

        // Reset the lab for the new protein (cleans up, resets state, sets sequence, resets todos)
        if (CodonTracker.instance != null)
        {
            CodonTracker.instance.ResetLabForNewProtein(sequence);
        }

        // Set the protein synthesis topic variable in Yarn
        if (GlobalDialogueManager.runner != null && GlobalDialogueManager.runner.VariableStorage != null)
        {
            GlobalDialogueManager.runner.VariableStorage.SetValue("$protein_synthesis_topic", selectedProtein.ToString().ToLower());
        }

        // Start the dialogue node (ProteinSynthesisLab which will guide through the lab)
        GlobalDialogueManager.StartDialogue("ProteinSynthesisLab");
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
}
