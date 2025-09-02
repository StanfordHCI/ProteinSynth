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
    public bool animationDone = false;

    [Header("Game Objects")]
    // Assign the game objects in the inspector
    public GameObject DNAObject;
    public GameObject mRNAObject; 
    public GameObject DNATargetObject;   
    public GameObject RibosomeTargetObject;   
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
    public TextMeshProUGUI aminoAcidInputCodonTextUI;


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
        if (transcriptionFinished && animationDone)
        {
            mRNAonRibosome();
        }
        DNAonNucleus();

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
    }

    private void DNAonNucleus() 
    {
        // ----------------------------
        // FOLLOW THE NUCLEUS
        // ----------------------------

        if (DNATargetObject != null && DNATargetObject.activeInHierarchy)
        {
            // Offset the floating object slightly above the target
            Vector3 hoverPosition = DNATargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);

            DNAObject.transform.position = Vector3.Lerp(DNAObject.transform.position, hoverPosition, Time.deltaTime * 10f);
            DNAObject.transform.rotation = DNATargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

            if (!DNAObject.activeSelf)
            {
                DNAObject.SetActive(true);
            }
        }
        else
        {
            if (DNAObject.activeSelf)
                DNAObject.SetActive(false);
        }
    }

    private void mRNAonRibosome() 
    {
        if (RibosomeTargetObject != null && RibosomeTargetObject.activeInHierarchy && mRNAObject != null)
        {
            // Offset the mRNA slightly above the ribosome target
            Vector3 hoverPosition = RibosomeTargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);

            mRNAObject.transform.position = Vector3.Lerp(mRNAObject.transform.position, hoverPosition, Time.deltaTime * 10f);
            mRNAObject.transform.rotation = RibosomeTargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

            if (!mRNAObject.activeSelf)
            {
                mRNAObject.SetActive(true);
            }
        }
        else
        {
            if (mRNAObject != null && mRNAObject.activeSelf)
            {
                mRNAObject.SetActive(false);
            }
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

    if (DNAObject == null || mRNA == null)
    {
        Debug.LogWarning("Could not find 3-5 strand under DNA target.");
        return;
    }
    
    // --- Always use the 3-5 template strand
    Transform threeToFive = DNAObject.transform.Find("3-5");
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
        // ❌ Incomplete transcription
        Debug.Log("Not finished transcribing all cards.");
        GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionUnsuccessful");
    }
    else if (studentStrand == expectedStrand)
    {
        // ✅ Successful transcription
        if (mRNA != null && mRNAObject != null)
        {
            // --- Preserve world transform before detaching
            Vector3 worldPos = mRNAObject.transform.position;
            Quaternion worldRot = mRNAObject.transform.rotation;
            Vector3 worldScale = mRNAObject.transform.lossyScale;

            // --- Detach from DNA while preserving world transform
            mRNAObject.transform.SetParent(null, true);

            // --- Reapply preserved transform to prevent shrinking
            mRNAObject.transform.position = worldPos;
            mRNAObject.transform.rotation = worldRot;
            mRNAObject.transform.localScale = worldScale;
        }

        transcriptionFinished = true;
        Debug.Log("Finished transcribing correctly.");
        MoveToRibosome();
    }
    else
    {
        // ❌ Incorrect transcription
        Debug.Log("Not correct. Try again.");
        GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionIncorrect");
    }
}

    public void MoveToRibosome()
    {
        if (mRNAObject != null && RibosomeTargetObject != null)
        {
            Debug.Log("Moving");
            StartCoroutine(MoveMRNAToRibosome());
        }
    }

    private System.Collections.IEnumerator MoveMRNAToRibosome()
    {
        float duration = 8f; // how long the movement takes
        float elapsed = 0f;

        Vector3 startPos = mRNAObject.transform.position;
        Quaternion startRot = mRNAObject.transform.rotation;

        // Apply the same offset and rotation adjustments as mRNAonRibosome
        Vector3 targetPos = RibosomeTargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);
        Quaternion targetRot = RibosomeTargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            mRNAObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mRNAObject.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            Debug.Log(mRNAObject.transform.position);

            yield return null;
        }
        animationDone = true;
        
        // Start amino acid input when animation is complete
        StartAminoAcidInput();
    }


    public void EnterDNATutorial() {
        GlobalDialogueManager.StartDialogue("ProteinSynthesisDNATutorial");
    }

    public void EndLab() {
        GlobalDialogueManager.StartDialogue("ProteinSynthesisReflection");
    }

    public void StartAminoAcidInput() {
        // TODO: Hardcoded for testing only
        lastCodonString = "AUG-UAC-UGC-UCU-GGU";
        aminoAcidInputCodonTextUI.text = lastCodonString;
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
