using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AminoAcidGenerator : MonoBehaviour
{
    private TemplateDNASpawner dnaSpawner;

    // Anticodon (tRNA) → (amino acid abbreviation, color)
    private readonly Dictionary<string, (string abbreviation, Color color)> anticodonToAminoAcid =
        new Dictionary<string, (string, Color)>()
    {
        { "UAC", ("Met", Color.green) },   // Matches mRNA AUG
        { "ACG", ("Cys", Color.yellow) },  // Matches mRNA UGC
        { "AUG", ("Tyr", Color.magenta) }, // Matches mRNA UAC
        { "AGA", ("Ser", Color.cyan) },    // Matches mRNA UCU
        { "CCA", ("Gly", Color.blue) },    // Matches mRNA GGU
        { "UGU", ("Thr", Color.red) },     // Matches mRNA ACA

        // Stop codons (mRNA UAA, UAG, UGA → anticodons AUU, AUC, ACU)
        { "AUU", ("Stop", Color.black) },
        { "AUC", ("Stop", Color.black) },
        { "ACU", ("Stop", Color.black) }
    };

    void Start()
    {
        dnaSpawner = GetComponent<TemplateDNASpawner>();
        if (dnaSpawner == null)
        {
            Debug.LogError("No TemplateDNASpawner found on this object!");
            return;
        }

        GenerateAminoAcid();
    }

    private void GenerateAminoAcid()
    {
        string anticodon = dnaSpawner.defaultSequence;
        if (string.IsNullOrEmpty(anticodon) || anticodon.Length != 3)
        {
            Debug.LogWarning("defaultSequence must be exactly 3 letters (an anticodon).");
            return;
        }

        if (anticodonToAminoAcid.TryGetValue(anticodon, out var aaInfo))
        {
            // Find AminoAcid child of this spawner's parent
            Transform aaTransform = transform.parent.Find("AminoAcid");
            if (aaTransform == null)
            {
                Debug.LogWarning("No AminoAcid child found under parent of tRNA spawner.");
                return;
            }

            // Change sphere color
            Renderer sphereRenderer = aaTransform.GetComponent<Renderer>();
            if (sphereRenderer != null)
            {
                sphereRenderer.material.color = aaInfo.color;
            }

            // Change text on the TMP label
            TextMeshPro label = aaTransform.GetComponentInChildren<TextMeshPro>();
            if (label != null)
            {
                label.text = aaInfo.abbreviation;
            }

            Debug.Log($"Anticodon {anticodon} → {aaInfo.abbreviation}");
        }
        else
        {
            Debug.LogWarning($"Unknown anticodon: {anticodon}");
        }
    }
}
