/*
    CodonTracker.cs file: this script is attached to the CodonManager GameObject. 
    - Handles tracking the current codons tracked on screen and corresponding amino acid chain produced. 
    - Updates every frame to check if students have reordered the cards or removed cards (sorts based on L-->R)
*/

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Yarn.Compiler;
using Yarn.Unity;

public class CodonTracker : MonoBehaviour
{
    // Singleton instance so other scripts can access this tracker globally
    public static CodonTracker instance;

    // Dictionary to store currently tracked codons and their associated GameObjects 
    private Dictionary<string, GameObject> activeCodons = new Dictionary<string, GameObject>();

    // Keeps track of the last known codon string to detect changes 
    private string lastCodonString = "";
    
    public bool transcriptionFinished = false;

    [Header("Game Objects")]
    // Assign the game objects in the inspector
    public GameObject floatingObject;
    public GameObject DNATargetObject;   
    public TemplateDNASpawner mRNA;
    public GameObject aminoAcidInput;

    [Header("3D Model Transforms")]
    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    [SerializeField] private float zOffset;
    [SerializeField] private float xRotation;
    [SerializeField] private float yRotation;
    [SerializeField] private float zRotation;


    // Dictionary for codon --> amino acid
    private readonly Dictionary<string, string> codonToAminoAcid = new Dictionary<string, string>()
    {
        { "AUG", "Met" }, // Start codon
        { "UGC", "Cys" },
        { "UAC", "Tyr" },
        { "UCU", "Ser" },
        { "GGU", "Gly" },
        { "ACA", "Thr" },

        /* Stop codons */
        { "UAA", "Stop" },
        { "UAG", "Stop" },
        { "UGA", "Stop" }
    };

    [Header("UI -- Text")]
    public TextMeshProUGUI topicTextUI;
    public TextMeshProUGUI codonTextUI;
    public TextMeshProUGUI aminoAcidTextUI;


    /**
        Function: Awake
        - Unity function to set up instance 
    */
    void Awake()
    {
        instance = this;
        aminoAcidInput.SetActive(false);

        // GlobalInMemoryVariableStorage.Instance.TryGetValue("$protein_synthesis_topic", out string topic);
        // if (topic != null)
        // {
        //     topicTextUI.text = "Topic: " + topic;
        // }
    }

    /**
        Function: Update
        - Unity function to check if codon string is changed every frame
    */
    void Update()
    {
        if (!transcriptionFinished) 
        {
            UpdateCodonStringIfChanged();
        }
    }


    /** 
        Function: RegisterCodon
        - ImageTarget is tracked, add codon to activeCodons dictionary
    */
    public void RegisterCodon(string codonName, GameObject obj)
    {
        if (!activeCodons.ContainsKey(codonName))
        {
            activeCodons[codonName] = obj;
        }
    }


    /** 
        Function: UnregisterCodon
        - ImageTarget is no longer tracked, remove codon to activeCodons dictionary
    */
    public void UnregisterCodon(string codonName)
    {
        if (activeCodons.ContainsKey(codonName))
        {
            activeCodons.Remove(codonName);
        }
    }


    /** 
        Function: UpdateCodonStringIfChanged
        - Sorts tracked codons from left to right 
        - Makes a list of codons that are currently tracked 
        - If there is an update to the tracking, then print the codon string and amino acid string (call TranslatetoAminoAcid function)
    */
    private void UpdateCodonStringIfChanged()
    {
        // Sort codons by horizontal (x) position
        List<GameObject> sorted = new List<GameObject>(activeCodons.Values);
        sorted.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        // Build codon string
        string fullCodon = "";
        foreach (GameObject obj in sorted)
        {
            string[] parts = obj.name.Split('_');
            string codon = parts.Length > 1 ? parts[1] : obj.name;
            fullCodon += codon + "-";
        }

        fullCodon = fullCodon.TrimEnd('-');

        // Only update if the string has changed
        if (fullCodon != lastCodonString)
        {
            lastCodonString = fullCodon;

            string aminoAcidChain = TranslateToAminoAcids(fullCodon);

            Debug.Log("Codons: " + fullCodon);
            Debug.Log("Amino Acids: " + aminoAcidChain);

            if (codonTextUI != null)
                codonTextUI.text = "Codons: " + fullCodon;

            if (aminoAcidTextUI != null)
                aminoAcidTextUI.text = "Amino Acids: " + aminoAcidChain;

            if (mRNA != null)
                UpdateStrand();
        }

        // ----------------------------
        // FOLLOW THE mRNA TRACKER
        // ----------------------------

        if (DNATargetObject != null && DNATargetObject.activeInHierarchy)
        {
            // Offset the floating object slightly above the target
            Vector3 hoverPosition = DNATargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);

            floatingObject.transform.position = Vector3.Lerp(floatingObject.transform.position, hoverPosition, Time.deltaTime * 10f);

            // Match the mRNA target's rotation with 90-degree correction
            floatingObject.transform.rotation = DNATargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

            if (!floatingObject.activeSelf)
            {
                floatingObject.SetActive(true);
            }
        }
        else
        {
            if (floatingObject.activeSelf)
                floatingObject.SetActive(false);
        }
    }



    /** 
        Function: TranslateToAnimoAcid
        - Accepts the codon stirng (i.e AUG-UGC-XXX) and returns a string of amino acids 
          in the form (Met-Cys-XXX). 
        - Translation stops at a stop codon if one is encountered. 
    */
    private string TranslateToAminoAcids(string codonString)
    {
        string[] codons = codonString.Split('-');
        List<string> aminoAcids = new List<string>();

        foreach (var codon in codons)
        {
            // looks for codon in codonToAminoAcid dictionary
            if (codonToAminoAcid.TryGetValue(codon, out string aminoAcid))
            {
                // adds corresponding amino acid to list
                aminoAcids.Add(aminoAcid);

                // found a stop codon --> stop translating!
                if (aminoAcid == "Stop")
                    break;
            }
            else
            {
                aminoAcids.Add("???"); // no codon found in dict.
            }
        }

        // returns string of amino acids separated by "-"
        return string.Join("-", aminoAcids);
    }

    public void UpdateStrand() {
        mRNA.ClearAllChildren();
        bool success = mRNA.SpawnTemplateSequence(lastCodonString.Replace("-", string.Empty));
    }

    public void FinishTranscription()
    {
        Debug.Log("Finished Transcription");

        if (floatingObject == null || mRNA == null)
        {
            Debug.LogWarning("Could not find 3-5 strand under DNA target.");
            return;
        }
        
        // --- Always use the 3-5 template strand
        Transform threeToFive = floatingObject.transform.Find("3-5");
        if (threeToFive == null)
        {
            Debug.LogWarning("Could not find 3-5 strand under DNA target.");
            return;
        }

        TemplateDNASpawner templateSpawner = threeToFive.GetComponent<TemplateDNASpawner>();
        if (templateSpawner == null)
        {
            Debug.LogWarning("3-5 strand missing TemplateDNASpawner.");
            return;
        }

        // --- Get DNA sequence from the template strand
        string dnaSequence = templateSpawner.defaultSequence;

        // --- Transcribe: DNA (T) → RNA (U)
        string expectedStrand = dnaSequence.Replace("T", "U");

        // --- Student’s current built sequence (strip dashes)
        string studentStrand = lastCodonString.Replace("-", string.Empty);

        if (studentStrand.Length != expectedStrand.Length) 
        {
            Debug.Log("Not finished transcribing all cards.");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionUnsuccessful");
        }
        else if (studentStrand == expectedStrand)
        {
            // ✅ Successful transcription
            if (mRNA != null && mRNA.transform.parent != null)
            {
                mRNA.transform.parent.SetParent(null); // detach the parent from its own parent
            }
            transcriptionFinished = true;
            Debug.Log("Finished transcribing correctly.");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionSuccessful");
        }
        else
        {
            // ❌ Incorrect transcription
            Debug.Log("Not correct. Try again.");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionIncorrect");
        }
    }

    public void EnterDNATutorial() {
        GlobalDialogueManager.StartDialogue("ProteinSynthesisDNATutorial");
    }

    public void EndLab() {
        GlobalDialogueManager.StartDialogue("ProteinSynthesisReflection");
    }

    public void StartAminoAcidInput() {
        GlobalDialogueManager.StartDialogue("ProteinSynthesisAminoAcidInput");
    }

    [YarnCommand("ToggleAminoAcidInput")]
    public void ToggleAminoAcidInput(bool show) {
        if (show) {
            aminoAcidInput.SetActive(true);
        } else {
            aminoAcidInput.SetActive(false);
        }
    }
}
