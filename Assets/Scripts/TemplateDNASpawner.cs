using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TemplateDNASpawner : MonoBehaviour
{
    public bool autoSpawn = false;

    [Header("Template DNA Settings")]
    [Tooltip("Template strand (5' -> 3')")]
    public string defaultDnaSequence = "TACGGCATTAGCTACGGC"; 

    [Header("DNA Base Prefabs")]
    public GameObject prefabA;
    public GameObject prefabT;
    public GameObject prefabC;
    public GameObject prefabG;
    public GameObject prefabA_Reverse;
    public GameObject prefabT_Reverse;
    public GameObject prefabC_Reverse;
    public GameObject prefabG_Reverse;

    [Header("Codon Parents (each should have 3 child spawn points)")]
    public List<Transform> forwardCodonParents;  
    public List<Transform> reverseCodonParents;  

    private Camera camera;

    private void Start()
    {
        if (camera == null)
            camera = Camera.main;

        if (autoSpawn)
            SpawnTemplateDNA();
    }

    public void SpawnTemplateDNA()
    {
        SpawnTemplateDNA(defaultDnaSequence);
    }

    public bool SpawnTemplateDNA(string dnaSequence)
    {
        // safety check: sequence must be divisible by 3
        if (dnaSequence.Length % 3 != 0)
        {
            Debug.LogWarning("DNA sequence length must be a multiple of 3 (codons).");
            return false;
        }

        int codonCount = dnaSequence.Length / 3;

        if (forwardCodonParents.Count != codonCount || reverseCodonParents.Count != codonCount)
        {
            Debug.LogWarning("Codon parent count does not match codon count!");
            return false;
        }

        // Loop through codons
        for (int codonIndex = 0; codonIndex < codonCount; codonIndex++)
        {
            string codon = dnaSequence.Substring(codonIndex * 3, 3);

            // Get the 3 children under each codon parent
            Transform[] forwardPoints = GetChildSpawnPoints(forwardCodonParents[codonIndex]);
            Transform[] reversePoints = GetChildSpawnPoints(reverseCodonParents[codonIndex]);

            for (int baseIndex = 0; baseIndex < 3; baseIndex++)
            {
                char baseChar = codon[baseIndex];

                // --- Forward base ---
                GameObject prefab = GetPrefab(baseChar, false);
                if (prefab != null && forwardPoints.Length > baseIndex)
                {
                    Quaternion rotation = Quaternion.Euler(0f, 90f, -180f);
                    Instantiate(prefab, forwardPoints[baseIndex].position, rotation, forwardPoints[baseIndex]);
                }

                // --- Reverse base (complement) ---
                char complement = GetComplement(baseChar);
                GameObject reversePrefab = GetPrefab(complement, true);
                if (reversePrefab != null && reversePoints.Length > baseIndex)
                {
                    Quaternion rotation = Quaternion.identity; 
                    Instantiate(reversePrefab, reversePoints[baseIndex].position, rotation, reversePoints[baseIndex]);
                }
            }
        }
        return true;
    }

    private Transform[] GetChildSpawnPoints(Transform parent)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("spawn_point"))
            {
                children.Add(child);
            }
        }

        // Sort by Unity's default numbering (spawn_point, spawn_point (1), spawn_point (2))
        children.Sort((a, b) => a.name.CompareTo(b.name));

        return children.ToArray();
    }

    private GameObject GetPrefab(char baseChar, bool reverse)
    {
        switch (baseChar)
        {
            case 'A': return reverse ? prefabA_Reverse : prefabA;
            case 'T': return reverse ? prefabT_Reverse : prefabT;
            case 'C': return reverse ? prefabC_Reverse : prefabC;
            case 'G': return reverse ? prefabG_Reverse : prefabG;
            default: return null;
        }
    }

    private char GetComplement(char baseChar)
    {
        switch (baseChar)
        {
            case 'A': return 'T';
            case 'T': return 'A';
            case 'C': return 'G';
            case 'G': return 'C';
            default: return 'N';
        }
    }
}
