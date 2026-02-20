/*
    CodonTracker.cs file: this script is attached to the CodonManager GameObject. 
    - Handles tracking the current codons tracked on screen and corresponding amino acid chain produced. 
    - Updates every frame to check if students have reordered the cards or removed cards (sorts based on L-->R)
*/

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using Yarn.Compiler;
using Yarn.Unity;
using Vuforia; 
using UnityEngine.XR.ARFoundation;

public class CodonTracker : MonoBehaviour
{
    // Singleton instance so other scripts can access this tracker globally
    public static CodonTracker instance;

    // Dictionary to store currently tracked codons and their associated GameObjects 
    private Dictionary<string, GameObject> activeCodons = new Dictionary<string, GameObject>();

    // Keeps track of the last known codon string to detect changes 
    private string lastCodonString = "";

    // Store initial mRNA transform for restoration
    private Vector3 initialmRNAPosition;
    private Quaternion initialmRNARotation;
    private Vector3 initialmRNAScale;
    private Transform initialmRNAParent;
    private bool hasStoredInitialmRNA = false;
    
    public bool transcriptionFinished = false;
    public bool animationDone = false;
    public bool ribosomeMoved = false;
    private bool shouldStartTranslation = false;
    public bool translationDone = false;
    private bool ribosomeFound = false;

    private bool introDone = false;
    private bool nucleusDialogue = false;

    [Header("Animation Settings")]
    [SerializeField] private Vector3 targetScaleMultiplier = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Game Objects")]
    public GameObject DNAObject;
    public GameObject TemplateStrand; 
    public GameObject CodingStrand;
    public GameObject mRNAObject;
    public GameObject mRNAPrefab; // Prefab for the original mRNA (needed to restore after flip)
    public GameObject mRNAReversePrefab;
    public GameObject mRNAReverseObject;
    public GameObject DNATargetObject;   
    public GameObject RibosomeTargetObject;   
    public TemplateDNASpawner mRNA;
    public tRNASpawner tRNASpawner;
    public GameObject aminoAcidInput;
    public GameObject endScene;
    public GameObject arCamera; 
    public GameObject transcriptionButton; 

    [Header("3D Model Transforms")]
    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    [SerializeField] private float zOffset;
    [SerializeField] private float xRotation;
    [SerializeField] private float yRotation;
    [SerializeField] private float zRotation;

    // Dictionary for codon --> amino acid
    [SerializeField] private AminoAcidData aminoAcidData;

    [Header("UI -- Text")]
    public TextMeshProUGUI topicTextUI;
    public TextMeshProUGUI codonTextUI;
    public TextMeshProUGUI aminoAcidTextUI;
    public TextMeshProUGUI aminoAcidInputCodonTextUI;
    public TodoList todoList;

    private ObserverBehaviour observer;


    void Awake()
    {
        instance = this;
        aminoAcidInput.SetActive(false);
        endScene.SetActive(false);
        observer = DNATargetObject.GetComponent<ObserverBehaviour>();
        if (observer) {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        if (DNAObject != null) {
            DNAObject.SetActive(false);
        }

        // Store initial mRNA transform for later restoration
        if (mRNAObject != null && !hasStoredInitialmRNA)
        {
            initialmRNAPosition = mRNAObject.transform.position;
            initialmRNARotation = mRNAObject.transform.rotation;
            initialmRNAScale = mRNAObject.transform.localScale;
            initialmRNAParent = mRNAObject.transform.parent;
            hasStoredInitialmRNA = true;
        }
    }

    void Update()
    {
        if (!transcriptionFinished) 
        {
            UpdateCodonStringIfChanged();
        }

        if (transcriptionFinished && ribosomeFound && !GlobalDialogueManager.runner.IsDialogueRunning && !ribosomeMoved) 
        {
            ribosomeMoved = true;
            GlobalDialogueManager.runner.Stop();
            MoveToRibosome();
        }
        else if (transcriptionFinished && !ribosomeFound && !GlobalDialogueManager.runner.IsDialogueRunning && !ribosomeMoved)
        {
            GlobalDialogueManager.StartDialogue("ScanRibosome");
        }

        if (transcriptionFinished && animationDone)
        {
            mRNAonRibosome();
            DNATargetObject.SetActive(false);
        }

        if (introDone == true)
        {
            DNAonNucleus();
        }
    }

    public void SetRibosomeFound()
    {
        ribosomeFound = true;
        GlobalDialogueManager.runner.VariableStorage.SetValue("$finished_scanning_ribosome", true);
    }

    [YarnCommand("set_intro_done")]
    public void SetIntroDone()
    {
        introDone = true;
    }

    [YarnCommand("hide_todo")]
    public void HideToDo()
    {
        todoList.gameObject.SetActive(false);
    }

    [YarnCommand("show_todo")]
    public void ShowToDo()
    {
        todoList.gameObject.SetActive(true);
    }

    [YarnCommand("set_sequence")]
    public void SetSequence(string templateSequence)
    {
        TemplateDNASpawner templateStrand = TemplateStrand.GetComponent<TemplateDNASpawner>();
        TemplateDNASpawner codingStrand = CodingStrand.GetComponent<TemplateDNASpawner>();

        // Set the template sequence
        templateStrand.defaultSequence = templateSequence.ToUpper();

        // Build the complementary (coding) strand
        char[] codingChars = new char[templateSequence.Length];
        for (int i = 0; i < templateSequence.Length; i++)
        {
            switch (templateSequence[i])
            {
                case 'A': codingChars[i] = 'T'; break;
                case 'T': codingChars[i] = 'A'; break;
                case 'C': codingChars[i] = 'G'; break;
                case 'G': codingChars[i] = 'C'; break;
                default: codingChars[i] = 'N'; break; // unknown base
            }
        }

        string codingSequence = new string(codingChars);

        // Assign to coding strand
        codingStrand.defaultSequence = codingSequence;

        // Spawn the nucleotides
        templateStrand.SpawnTemplateSequenceInstant();
        codingStrand.SpawnTemplateSequenceInstant();
    }

    [YarnCommand("start_trna")]
    public void StartTRNA()
    {
        Transform templateStrand = TemplateStrand.transform;
        if (templateStrand != null)
        {
            TemplateDNASpawner dnaSpawner = templateStrand.GetComponent<TemplateDNASpawner>();
            if (dnaSpawner != null)
            {
                string dnaSequence = dnaSpawner.defaultSequence;

                // Convert DNA to mRNA (replace all T with U)
                string mRNAComp = dnaSequence.Replace('T', 'U');

                // Make sure tRNASpawner is assigned before calling it
                if (tRNASpawner != null)
                {
                    tRNASpawner.StartSpawning(mRNAComp);
                }
                else
                {
                    Debug.LogWarning("tRNASpawner reference is missing!");
                }
            }
            else
            {
                Debug.LogWarning("TemplateDNASpawner component not found on 'template'.");
            }
        }
        else
        {
            Debug.LogWarning("'template' Transform not found under DNAObject.");
        }
    }

    public void RegisterCodon(string codonName, GameObject obj)
    {
        if (!activeCodons.ContainsKey(codonName))
        {
            activeCodons[codonName] = obj;
        }
    }

    public void UnregisterCodon(string codonName)
    {
        if (activeCodons.ContainsKey(codonName))
        {
            activeCodons.Remove(codonName);
        }
    }

    private void UpdateCodonStringIfChanged()
    {
        List<GameObject> sorted = new List<GameObject>(activeCodons.Values);
        sorted.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        string fullCodon = "";
        foreach (GameObject obj in sorted)
        {
            string[] parts = obj.name.Split('_');
            string codon = parts.Length > 1 ? parts[1] : obj.name;
            fullCodon += codon + "-";
        }

        fullCodon = fullCodon.TrimEnd('-');

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
    
    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
        DNAObject.SetActive(isTracked);
    }
   
    private void DNAonNucleus() 
    {
        if (!DNAObject.activeSelf)
            return;

        Vector3 hoverPosition = DNATargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);

        DNAObject.transform.position = Vector3.Lerp(DNAObject.transform.position, hoverPosition, Time.deltaTime * 10f);
        DNAObject.transform.rotation = DNATargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

        if (!nucleusDialogue)
        {
            nucleusDialogue = true;
            todoList.CheckoffToDo("scan_nucleus");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisDNATutorial");
        }
    }

    private void mRNAonRibosome() 
    {
        GameObject strand = mRNAReverseObject != null ? mRNAReverseObject : mRNAObject;

        if (RibosomeTargetObject != null && RibosomeTargetObject.activeInHierarchy && strand != null)
        {
            Vector3 hoverPosition = RibosomeTargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);

            strand.transform.position = Vector3.Lerp(strand.transform.position, hoverPosition, Time.deltaTime * 10f);
            strand.transform.rotation = RibosomeTargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

            if (!strand.activeSelf)
                strand.SetActive(true);
        }
        else
        {
            if (strand != null && strand.activeSelf)
            {
                strand.SetActive(false);
            }
        }
    }

    private string TranslateToAminoAcids(string codonString)
    {
        if (aminoAcidData == null)
        {
            Debug.LogWarning("AminoAcidData reference not assigned!");
            return "";
        }

        string[] codons = codonString.Split('-');
        List<string> aminoAcids = new List<string>();

        foreach (var codon in codons)
        {
            string aminoAcid = aminoAcidData.GetAminoAcidNameFromCodon(codon);

            if (aminoAcid != null)
            {
                aminoAcids.Add(aminoAcid);

                if (aminoAcid == "Stop")
                    break;
            }
            else
            {
                aminoAcids.Add("???");
            }
        }

        return string.Join("-", aminoAcids);
    }

    public void UpdateStrand() {
        // Let SpawnTemplateSequence handle clearing logic based on sequence comparison
        bool success = mRNA.SpawnTemplateSequence(lastCodonString.Replace("-", string.Empty));
    }

    public void FinishTranscription()
    {
        Debug.Log("Finished Transcription");

        if (DNAObject == null || mRNA == null)
        {
            Debug.LogWarning("Could not find coding strand under DNA target.");
            return;
        }
        Transform codingStrand = CodingStrand.transform;
        if (codingStrand == null)
        {
            Debug.LogWarning("Could not find coding strand under DNA target.");
            return;
        }

        TemplateDNASpawner dnaSpawner = codingStrand.GetComponent<TemplateDNASpawner>();
        if (dnaSpawner == null)
        {
            Debug.LogWarning("template strand missing TemplateDNASpawner.");
            return;
        }

        string dnaSequence = dnaSpawner.defaultSequence;
        string expectedStrand = dnaSequence.Replace("T", "U");
        string studentStrand = lastCodonString.Replace("-", string.Empty);

        if (studentStrand.Length != expectedStrand.Length) 
        {
            Debug.Log("Not finished transcribing all cards.");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionUnsuccessful");
        }
        else if (studentStrand == expectedStrand)
        {
            transcriptionFinished = true;
            Debug.Log("Finished transcribing correctly.");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionSuccessful");
            todoList.CheckoffToDo("arrange_cards");
            todoList.CheckoffToDo("finish_transcription");
            transcriptionButton.SetActive(false); 
        }
        else
        {
            Debug.Log("Not correct. Try again.");
            GlobalDialogueManager.StartDialogue("ProteinSynthesisTranscriptionIncorrect");
        }
    }

    public void MoveToRibosome()
    {
        if (!ribosomeFound) 
        {
            Debug.Log("Ribosome not found yet.");
            return;
        }

        todoList.CheckoffToDo("scan_ribosome");

        if (mRNAObject != null) {
            FlipAndResizeMRNA();
        }

        if (mRNAReverseObject != null && RibosomeTargetObject != null)
        {
            Debug.Log("Moving reverse mRNA strand");
            StartCoroutine(MoveMRNAToRibosome());
        }
    }

    private void FlipAndResizeMRNA() 
    {
        if (mRNAObject == null || mRNAReversePrefab == null) {
            Debug.LogWarning("mRNAObject or mRNAReversePrefab missing!");
            return;
        }

        // Save world transform of mRNA
        Vector3 pos = mRNAObject.transform.position;
        Quaternion rot = mRNAObject.transform.rotation;
        Vector3 scale = mRNAObject.transform.lossyScale;

        // Hide the original mRNA instead of destroying it
        mRNAObject.SetActive(false);

        // Spawn reverse strand
        mRNAReverseObject = Instantiate(mRNAReversePrefab, pos, rot);
        mRNAReverseObject.transform.localScale = scale;
        mRNAReverseObject.transform.SetParent(null, true);

        // Find the child "mRNA spawner"
        Transform spawnerChild = mRNAReverseObject.transform.Find("mRNA spawner");
        if (spawnerChild != null) {
            // Update TemplateDNASpawner
            TemplateDNASpawner reverseSpawner = spawnerChild.GetComponent<TemplateDNASpawner>();
            if (reverseSpawner != null) {
                reverseSpawner.defaultSequence = lastCodonString.Replace("-", string.Empty);
                reverseSpawner.ClearAllChildren();
                reverseSpawner.SpawnTemplateSequence(reverseSpawner.defaultSequence);
                Debug.Log("Set reverse mRNA defaultSequence to " + reverseSpawner.defaultSequence);
            }

            // Reassign tRNASpawner to the one attached to this child
            tRNASpawner = spawnerChild.GetComponent<tRNASpawner>();
            if (tRNASpawner != null) {
                Debug.Log("tRNASpawner reassigned to reverse strand.");
            } else {
                Debug.LogWarning("mRNA spawner child has no tRNASpawner component.");
            }
        } else {
            Debug.LogWarning("mRNAReverseObject missing child 'mRNA spawner'.");
        }

        Debug.Log("Spawned mRNA reverse strand.");
    }

    private System.Collections.IEnumerator MoveMRNAToRibosome()
    {
        float duration = 8f;
        float elapsed = 0f;

        Vector3 startPos = mRNAReverseObject.transform.position;
        Quaternion startRot = mRNAReverseObject.transform.rotation;
        Vector3 startScale = mRNAReverseObject.transform.localScale;

        Vector3 targetPos = RibosomeTargetObject.transform.position + new Vector3(xOffset, yOffset, zOffset);
        Quaternion targetRot = RibosomeTargetObject.transform.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);
        Vector3 targetScale = Vector3.Scale(startScale, targetScaleMultiplier);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            mRNAReverseObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mRNAReverseObject.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            mRNAReverseObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        animationDone = true;

        // Start amino acid input when animation is complete
        StartAminoAcidInput();
    }

    public void EnterDNATutorial() 
    {
        GlobalDialogueManager.runner.VariableStorage.SetValue("$finished_scanning_nucleus", true);

    }

    // public void EndLab() {
    //     GlobalDialogueManager.StartDialogue("ProteinSynthesisReflection");
    // }

    /// <summary>
    /// Comprehensive reset function that cleans up all spawned 3D models, resets state, sets new sequence, and resets todo list.
    /// </summary>
    public void ResetLabForNewProtein(string newSequence)
    {
        Debug.Log("CodonTracker: Resetting lab for new protein with sequence: " + newSequence);

        // Clean up all spawned 3D models
        CleanupSpawnedObjects();

        // Reset all state flags
        ResetStateFlags();

        // Reset UI elements
        ResetUI();

        // Reset todo list
        ResetTodoList();

        // Reactivate initial objects
        ReactivateInitialObjects();

        // Set the new sequence
        SetSequence(newSequence);

        Debug.Log("CodonTracker: Lab reset complete. Ready for new protein.");
    }

    /// <summary>
    /// Cleans up all spawned 3D models that were created after initial state.
    /// </summary>
    private void CleanupSpawnedObjects()
    {
        // Clear active codons dictionary and destroy any remaining objects immediately
        foreach (var kvp in activeCodons)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }
        activeCodons.Clear();

        // Destroy only the reverse mRNA (spawned object)
        // Don't destroy the original mRNAObject - we'll just hide/show it
        if (mRNAReverseObject != null)
        {
            DestroyImmediate(mRNAReverseObject);
            mRNAReverseObject = null;
        }

        // Clean up tRNA spawner - find and destroy all tRNA objects immediately
        if (tRNASpawner != null && tRNASpawner.spawnParent != null)
        {
            // Destroy all children of spawnParent that are tRNAs
            for (int i = tRNASpawner.spawnParent.childCount - 1; i >= 0; i--)
            {
                Transform child = tRNASpawner.spawnParent.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        // Clean up amino acid holder if it exists
        if (tRNASpawner != null && tRNASpawner.aminoAcidHolder != null)
        {
            for (int i = tRNASpawner.aminoAcidHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = tRNASpawner.aminoAcidHolder.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        // Clean up mRNA spawner children (DNA bases on mRNA)
        if (mRNA != null && mRNA.spawnParent != null)
        {
            mRNA.ClearAllChildren();
        }

        // Clean up template and coding strand spawners (clear any spawned bases)
        if (TemplateStrand != null)
        {
            TemplateDNASpawner templateSpawner = TemplateStrand.GetComponent<TemplateDNASpawner>();
            if (templateSpawner != null && templateSpawner.spawnParent != null)
            {
                templateSpawner.ClearAllChildren();
            }
        }
        if (CodingStrand != null)
        {
            TemplateDNASpawner codingSpawner = CodingStrand.GetComponent<TemplateDNASpawner>();
            if (codingSpawner != null && codingSpawner.spawnParent != null)
            {
                codingSpawner.ClearAllChildren();
            }
        }
    }

    /// <summary>
    /// Resets all state flags to initial values.
    /// </summary>
    private void ResetStateFlags()
    {
        transcriptionFinished = false;
        animationDone = false;
        ribosomeMoved = false;
        shouldStartTranslation = false;
        translationDone = false;
        ribosomeFound = false;
        introDone = false;
        nucleusDialogue = false;
        lastCodonString = "";
    }

    /// <summary>
    /// Resets UI elements to initial state.
    /// </summary>
    private void ResetUI()
    {
        // Hide amino acid input and end scene
        if (aminoAcidInput != null)
        {
            aminoAcidInput.SetActive(false);
            
            // Reset the AminoAcidDropdownInput component to clear all selections
            AminoAcidDropdownInput dropdownInput = aminoAcidInput.GetComponent<AminoAcidDropdownInput>();
            if (dropdownInput != null)
            {
                dropdownInput.ClearSelection();
            }
        }
        if (endScene != null)
        {
            endScene.SetActive(false);
        }

        // Reset transcription button
        if (transcriptionButton != null)
        {
            transcriptionButton.SetActive(true);
        }

        // Clear text displays
        if (codonTextUI != null)
        {
            codonTextUI.text = "";
        }
        if (aminoAcidTextUI != null)
        {
            aminoAcidTextUI.text = "";
        }
        if (aminoAcidInputCodonTextUI != null)
        {
            aminoAcidInputCodonTextUI.text = "";
        }
    }

    /// <summary>
    /// Resets the todo list by clearing all active todos.
    /// </summary>
    private void ResetTodoList()
    {
        if (todoList != null && todoList.layoutGroup != null)
        {
            // Destroy all todo items
            for (int i = todoList.layoutGroup.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = todoList.layoutGroup.transform.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // Clear the active todos dictionary using reflection or a public method
            // Since activeTodos is private, we'll need to add a public method to TodoList
            todoList.ResetAllTodos();
        }
    }

    /// <summary>
    /// Reactivates initial objects that should be visible at the start.
    /// </summary>
    private void ReactivateInitialObjects()
    {
        // Reactivate DNA target object
        if (DNATargetObject != null)
        {
            DNATargetObject.SetActive(true);
        }

        // Deactivate DNA object initially (will be activated when nucleus is scanned)
        if (DNAObject != null)
        {
            DNAObject.SetActive(false);
        }

        // Reactivate ribosome target object
        if (RibosomeTargetObject != null)
        {
            RibosomeTargetObject.SetActive(true);
        }

        // Restore original mRNA - show it and reset its position
        if (mRNAObject != null)
        {
            // Show the mRNA (it was hidden when flipped)
            mRNAObject.SetActive(true);
            
            // Reset to initial position if we stored it
            if (hasStoredInitialmRNA)
            {
                mRNAObject.transform.position = initialmRNAPosition;
                mRNAObject.transform.rotation = initialmRNARotation;
                mRNAObject.transform.localScale = initialmRNAScale;
                if (initialmRNAParent != null)
                {
                    mRNAObject.transform.SetParent(initialmRNAParent, true);
                }
            }
            
            // Restore the mRNA spawner reference
            Transform spawnerChild = mRNAObject.transform.Find("mRNA spawner");
            if (spawnerChild != null)
            {
                mRNA = spawnerChild.GetComponent<TemplateDNASpawner>();
                tRNASpawner = spawnerChild.GetComponent<tRNASpawner>();
                Debug.Log("Restored original mRNA and spawner references.");
            }
        }
    }

    /// <summary>
    /// Cleans up all spawned 3D models and resets the tracker state for a new lab run.
    /// </summary>
    public void CleanupAndReset()
    {
        CleanupSpawnedObjects();
        ResetStateFlags();
        Debug.Log("CodonTracker: Cleanup complete.");
    }

    public void StartAminoAcidInput() {
        aminoAcidInputCodonTextUI.text = lastCodonString;
        GlobalDialogueManager.StartDialogue("ProteinSynthesisAminoAcidInput");
    }

    [YarnCommand("ToggleAminoAcidInput")]
    public void ToggleAminoAcidInput(bool show) {
        aminoAcidInput.SetActive(show);
    }

    [YarnCommand("ToggleEndScene")]
    public void ToggleEndScene(bool show) {
        Debug.Log("ToggleEndScene: " + show);
        endScene.SetActive(show);
    }

    /// <summary>
    /// Toggles the end scene after a 2 second delay. Can be called from other scripts.
    /// </summary>
    public void ToggleEndSceneDelayed(bool show) {
        StartCoroutine(ToggleEndSceneAfterDelay(show, 2f));
    }

    [YarnCommand("ToggleEndSceneDelayed")]
    public void ToggleEndSceneDelayedCommand(bool show) {
        ToggleEndSceneDelayed(show);
    }

    private IEnumerator ToggleEndSceneAfterDelay(bool show, float delay) {
        yield return new WaitForSeconds(delay);
        ToggleEndScene(show);
    }

    [YarnCommand("StartTranslation")]
    public void StartTranslation() {
        shouldStartTranslation = true;
    }

    // [YarnCommand("enable_camera")]
    // public void EnableCamera(bool enable)
    // {
    //     if (enable) {
    //         Debug.Log("Enabling AR camera and restarting Vuforia tracking...");
    //         // Start the reset coroutine first, then activate camera when ready
    //         StartCoroutine(EnableCameraWithReset());
    //     } else {
    //         Debug.Log("Disabling AR camera");
    //         arCamera.SetActive(false);
    //     }
    // }

    // private IEnumerator EnableCameraWithReset()
    // {
    //     // First ensure Vuforia is initialized
    //     while (VuforiaApplication.Instance.IsInitialized == null || VuforiaApplication.Instance.IsInitialized == false)
    //     {
    //         Debug.Log("Waiting for Vuforia to initialize before enabling camera...");
    //         yield return null;
    //     }

    //     // Wait a frame to ensure everything is stable
    //     yield return new WaitForEndOfFrame();

    //     // Now activate the camera
    //     arCamera.SetActive(true);
    //     Debug.Log("AR camera activated");

    //     // Wait a moment for camera to initialize
    //     yield return new WaitForSeconds(0.1f);

    //     // Then reset Vuforia tracking
    //     yield return StartCoroutine(ResetVuforiaWhenReady());
    // } 


    // private IEnumerator ResetVuforiaWhenReady()
    // {
    //     // Wait until Vuforia has actually started
    //     // Fix: Wait while Vuforia is NOT initialized (null or false)
    //     while (VuforiaApplication.Instance.IsInitialized == null || VuforiaApplication.Instance.IsInitialized == false)
    //     {
    //         Debug.Log("Waiting for Vuforia to initialize...");
    //         yield return null;
    //     }

    //     Debug.Log("Vuforia is initialized, proceeding with reset...");

    //     // Wait a frame to ensure everything is ready
    //     yield return new WaitForEndOfFrame();

    //     var devicePose = VuforiaBehaviour.Instance.DevicePoseBehaviour;
    //     if (devicePose != null && devicePose.enabled)
    //     {
    //         Debug.Log("Restarting Vuforia tracking safely before AR activity");
    //         devicePose.Reset();
            
    //         // Wait a moment after reset to ensure tracking restarts properly
    //         yield return new WaitForSeconds(0.5f);
    //         Debug.Log("Vuforia tracking reset complete");
    //     }
    //     else
    //     {
    //         Debug.LogWarning("DevicePoseBehaviour not available or disabled.");
    //     }
    // }
}
