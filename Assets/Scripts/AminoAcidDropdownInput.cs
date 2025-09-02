using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class AminoAcidDropdownInput : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Dropdown[] aminoAcidDropdowns = new TMP_Dropdown[5];
    [SerializeField] private Button validateButton;
    [SerializeField] private Button checkCodonButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Image validationIndicator;
    [SerializeField] private TextMeshProUGUI sequenceDisplayText;
    [SerializeField] private TextMeshProUGUI codonText;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;
    [SerializeField] private Color neutralColor = Color.gray;
    
    [Header("Settings")]
    [SerializeField] private bool autoValidate = true;
    [SerializeField] private bool showFullNames = true;
    
    // Dictionary of valid amino acid three-letter codes with full names
    private readonly Dictionary<string, string> aminoAcidData = new Dictionary<string, string>()
    {
        { "ALA", "Alanine" }, { "ARG", "Arginine" }, { "ASN", "Asparagine" }, { "ASP", "Aspartic acid" },
        { "CYS", "Cysteine" }, { "GLU", "Glutamic acid" }, { "GLN", "Glutamine" }, { "GLY", "Glycine" },
        { "HIS", "Histidine" }, { "ILE", "Isoleucine" }, { "LEU", "Leucine" }, { "LYS", "Lysine" },
        { "MET", "Methionine" }, { "PHE", "Phenylalanine" }, { "PRO", "Proline" }, { "SER", "Serine" },
        { "THR", "Threonine" }, { "TRP", "Tryptophan" }, { "TYR", "Tyrosine" }, { "VAL", "Valine" }
    };
    
    // Dictionary for codon --> amino acid (matching CodonTracker)
    private readonly Dictionary<string, string> codonToAminoAcid = new Dictionary<string, string>()
    {
        { "AUG", "MET" }, // Start codon
        { "UGC", "CYS" },
        { "UAC", "TYR" },
        { "UCU", "SER" },
        { "GGU", "GLY" },
        { "ACA", "THR" },
        { "UAA", "Stop" },
        { "UAG", "Stop" },
        { "UGA", "Stop" }
    };
    
    private string[] selectedAminoAcids = new string[5];
    private bool isValid = false;
    
    // Events
    public System.Action<string[]> OnValidSequenceEntered;
    public System.Action<string> OnSelectionChanged;
    public System.Action<bool> OnCodonMatchResult;
    
    void Start()
    {
        SetupDropdowns();
        SetupValidationButton();
        UpdateCodonCheckButton();
        UpdateDisplay();
    }
    
    void SetupDropdowns()
    {
        // Create dropdown options
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        // Add empty option first
        options.Add(new TMP_Dropdown.OptionData("Select"));
        
        // Add all amino acids (only 3-letter codes)
        foreach (var aminoAcid in aminoAcidData.OrderBy(x => x.Key))
        {
            options.Add(new TMP_Dropdown.OptionData(aminoAcid.Key));
        }
        
        // Setup each dropdown
        for (int i = 0; i < aminoAcidDropdowns.Length; i++)
        {
            if (aminoAcidDropdowns[i] != null)
            {
                int dropdownIndex = i; // Capture for closure
                
                // Set options
                aminoAcidDropdowns[i].options = new List<TMP_Dropdown.OptionData>(options);
                aminoAcidDropdowns[i].value = 0; // Start with empty selection
                
                // Add listener
                aminoAcidDropdowns[i].onValueChanged.AddListener((value) => OnDropdownChanged(dropdownIndex, value));
                
                // Initialize array
                selectedAminoAcids[i] = "";
            }
        }
    }
    
    void SetupValidationButton()
    {
        if (validateButton != null)
        {
            validateButton.interactable = selectedAminoAcids.Any(x => !string.IsNullOrEmpty(x));
        }
    }
    
    void UpdateCodonCheckButton()
    {
        if (checkCodonButton != null)
        {
            // Enable button only when all 5 amino acids are selected
            bool allSelected = selectedAminoAcids.All(x => !string.IsNullOrEmpty(x));
            checkCodonButton.interactable = allSelected;
            
            // Add listener if not already added
            checkCodonButton.onClick.RemoveAllListeners();
            checkCodonButton.onClick.AddListener(CheckCodonMatch);
        }
    }
    
    void OnDropdownChanged(int dropdownIndex, int selectedValue)
    {
        // Get the selected amino acid code
        if (selectedValue == 0)
        {
            selectedAminoAcids[dropdownIndex] = ""; // Empty selection
        }
        else
        {
            // Get the 3-letter code directly from the option text
            string optionText = aminoAcidDropdowns[dropdownIndex].options[selectedValue].text;
            selectedAminoAcids[dropdownIndex] = optionText;
        }
        
        UpdateDisplay();
        UpdateCodonCheckButton();
        
        // Auto-validate if enabled
        if (autoValidate)
        {
            ValidateSelection();
        }
        
        // Trigger selection changed event
        OnSelectionChanged?.Invoke(GetCurrentSequenceString());
    }
    
    void UpdateDisplay()
    {
        if (sequenceDisplayText != null)
        {
            // Show current sequence with full names vertically
            List<string> displayLines = new List<string>();
            displayLines.Add("Sequence:");
            
            for (int i = 0; i < selectedAminoAcids.Length; i++)
            {
                if (!string.IsNullOrEmpty(selectedAminoAcids[i]))
                {
                    string fullName = aminoAcidData[selectedAminoAcids[i]];
                    displayLines.Add($"{i + 1}. {selectedAminoAcids[i]} ({fullName})");
                }
                else
                {
                    displayLines.Add($"{i + 1}. ___");
                }
            }
            
            if (selectedAminoAcids.All(x => string.IsNullOrEmpty(x)))
            {
                sequenceDisplayText.text = "Sequence:\n(none selected)";
            }
            else
            {
                sequenceDisplayText.text = string.Join("\n", displayLines);
            }
        }
    }
    
    public void ValidateSelection()
    {
        // Check if all dropdowns have selections
        int selectedCount = selectedAminoAcids.Count(x => !string.IsNullOrEmpty(x));
        
        if (selectedCount == 0)
        {
            UpdateVisualFeedback(false, "Please select amino acids");
            return;
        }
        
        if (selectedCount < 5)
        {
            UpdateVisualFeedback(false, $"Please select all 5 amino acids ({selectedCount}/5 selected)");
            return;
        }
        
        // All amino acids selected - validate
        List<string> fullNames = new List<string>();
        foreach (string code in selectedAminoAcids)
        {
            if (aminoAcidData.ContainsKey(code))
            {
                fullNames.Add(aminoAcidData[code]);
            }
        }
        
        string successMessage = "All five amino acids selected! Now press the \"Check amino acids\" button.";
        UpdateVisualFeedback(true, successMessage);
        
        // Trigger success event
        OnValidSequenceEntered?.Invoke(selectedAminoAcids);
    }
    
    void UpdateVisualFeedback(bool valid, string message)
    {
        isValid = valid;
        
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = valid ? validColor : invalidColor;
        }
    }
    
    // Public methods for external access
    public bool IsValid => isValid;
    public string[] GetSelectedAminoAcids() => selectedAminoAcids.ToArray();
    public string GetCurrentSequenceString() => string.Join("-", selectedAminoAcids.Where(x => !string.IsNullOrEmpty(x)));
    
    public void ClearSelection()
    {
        for (int i = 0; i < aminoAcidDropdowns.Length; i++)
        {
            if (aminoAcidDropdowns[i] != null)
            {
                aminoAcidDropdowns[i].value = 0;
            }
            selectedAminoAcids[i] = "";
        }
        UpdateDisplay();
        UpdateVisualFeedback(false, "Please select amino acids");
    }
    
    public void SetSelection(string[] aminoAcids)
    {
        for (int i = 0; i < Mathf.Min(aminoAcids.Length, selectedAminoAcids.Length); i++)
        {
            string code = aminoAcids[i].ToUpper();
            if (aminoAcidData.ContainsKey(code))
            {
                selectedAminoAcids[i] = code;
                
                // Find the corresponding dropdown option
                for (int j = 1; j < aminoAcidDropdowns[i].options.Count; j++)
                {
                    if (aminoAcidDropdowns[i].options[j].text == code)
                    {
                        aminoAcidDropdowns[i].value = j;
                        break;
                    }
                }
            }
        }
        UpdateDisplay();
        ValidateSelection();
    }
    
    public void CheckCodonMatch()
    {
        if (codonText == null)
        {
            Debug.LogWarning("CodonText is not assigned!");
            return;
        }
        
        // Get codon sequence from the text component
        string codonSequence = codonText.text.Trim().ToUpper();
        
        // Parse codons (assuming format like "AUG-UGC-UAC-UCU-GGU" or "AUGUGCUACUCUGGU")
        string[] codons = ParseCodonSequence(codonSequence);
        
        if (codons.Length != 5)
        {
            if (feedbackText != null)
            {
                feedbackText.text = $"Expected 5 codons, found {codons.Length}";
                feedbackText.color = invalidColor;
            }
            OnCodonMatchResult?.Invoke(false);
            return;
        }
        
        // Check if selected amino acids match the codons
        bool isMatch = true;
        List<string> mismatches = new List<string>();
        
        for (int i = 0; i < 5; i++)
        {
            string expectedAminoAcid = GetAminoAcidFromCodon(codons[i]);
            string selectedAminoAcid = selectedAminoAcids[i];
            
            if (expectedAminoAcid != selectedAminoAcid)
            {
                isMatch = false;
                mismatches.Add($"Position {i + 1}: Expected {expectedAminoAcid} (from {codons[i]}), got {selectedAminoAcid}");
            }
        }
        
        // Update feedback
        if (feedbackText != null)
        {
            if (isMatch)
            {
                feedbackText.text = "Perfect match! Your amino acid sequence matches the codon sequence.";
                feedbackText.color = validColor;
                GlobalDialogueManager.StartDialogue("ProteinSynthesisAminoAcidInputSuccessful");
            }
            else
            {
                // feedbackText.text = "Mismatch found:\n" + string.Join("\n", mismatches);
                feedbackText.text = "The amino acids don't match the codons. Double check your codon chart and try again!";
                feedbackText.color = invalidColor;
            }
        }
        
        OnCodonMatchResult?.Invoke(isMatch);
    }
    
    string[] ParseCodonSequence(string codonSequence)
    {
        // Remove any spaces and convert to uppercase
        codonSequence = codonSequence.Replace(" ", "").ToUpper();
        
        // Try parsing with hyphens first
        if (codonSequence.Contains("-"))
        {
            return codonSequence.Split('-');
        }
        
        // If no hyphens, assume continuous string and split into 3-character chunks
        List<string> codons = new List<string>();
        for (int i = 0; i < codonSequence.Length; i += 3)
        {
            if (i + 2 < codonSequence.Length)
            {
                codons.Add(codonSequence.Substring(i, 3));
            }
        }
        
        return codons.ToArray();
    }
    
    string GetAminoAcidFromCodon(string codon)
    {
        if (codonToAminoAcid.TryGetValue(codon, out string aminoAcid))
        {
            return aminoAcid;
        }
        
        return "Unknown";
    }
    
    // Helper method to get all available amino acid codes
    public List<string> GetAvailableAminoAcids() => aminoAcidData.Keys.ToList();
    
    // Helper method to get amino acid name from code
    public string GetAminoAcidName(string code)
    {
        return aminoAcidData.TryGetValue(code.ToUpper(), out string name) ? name : "Unknown";
    }
    
    // Toggle between showing full names or just codes
    public void SetShowFullNames(bool show)
    {
        showFullNames = show;
        SetupDropdowns(); // Refresh dropdown options
    }
}
