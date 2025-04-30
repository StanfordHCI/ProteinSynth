/*
    CodonTracker.cs file: this script is attached to the CodonManager GameObject. 
    - Handles tracking the current codons tracked on screen and corresponding amino acid chain produced. 
    - Updates every frame to check if students have reordered the cards or removed cards (sorts based on L-->R)
*/

using System.Collections.Generic;
using UnityEngine;

public class CodonTracker : MonoBehaviour
{
    /* Singleton instance so other scripts can access this tracker globally */
    public static CodonTracker instance;

    /* Dictionary to store currently tracked codons and their associated GameObjects */
    private Dictionary<string, GameObject> activeCodons = new Dictionary<string, GameObject>();

    /* Keeps track of the last known codon string to detect changes */
    private string lastCodonString = "";

    /* Dictionary mapping RNA codons to their corresponding amino acid names */
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

    /**
        Function: Awake
        - Unity function to set up instance 
    */
    void Awake()
    {
        instance = this;
    }

    /**
        Function: Update
        - Unity function to check if codon string is changed every frame
    */
    void Update()
    {
        UpdateCodonStringIfChanged();
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
}
