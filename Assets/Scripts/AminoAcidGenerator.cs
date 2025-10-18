using UnityEngine;
using TMPro;

public class AminoAcidGenerator : MonoBehaviour
{
    private AminoAcidData aminoAcidData;
    private TemplateDNASpawner dnaSpawner;

    void Start()
    {
        dnaSpawner = GetComponent<TemplateDNASpawner>();

        if (dnaSpawner == null)
        {
            Debug.LogError("[AminoAcidGenerator] No TemplateDNASpawner found on this object!");
            return;
        }

        GameObject codonManagerObj = GameObject.Find("CodonManager");
        if (codonManagerObj == null)
        {
            Debug.LogError("[AminoAcidGenerator] Could not find GameObject named 'CodonManager' in the scene!");
            return;
        }

        aminoAcidData = codonManagerObj.GetComponent<AminoAcidData>();
        if (aminoAcidData == null)
        {
            Debug.LogError("[AminoAcidGenerator] 'CodonManager' GameObject does not have an AminoAcidData component!");
            return;
        }

        // Now generate the amino acid
        GenerateAminoAcid();
    }

    private void GenerateAminoAcid()
    {
        string anticodon = dnaSpawner.defaultSequence;
        if (string.IsNullOrEmpty(anticodon) || anticodon.Length != 3)
        {
            Debug.LogWarning("[AminoAcidGenerator] defaultSequence must be exactly 3 letters (an anticodon).");
            return;
        }

        var (aminoAcid, color) = aminoAcidData.GetAminoAcidFromAnticodon(anticodon);

        if (aminoAcid != null)
        {
            // Find AminoAcid child under this spawner’s parent
            Transform aaTransform = transform.parent.Find("AminoAcid");
            if (aaTransform == null)
            {
                Debug.LogWarning("[AminoAcidGenerator] No 'AminoAcid' child found under parent of tRNA spawner.");
                return;
            }

            // Change sphere color
            Renderer sphereRenderer = aaTransform.GetComponent<Renderer>();
            if (sphereRenderer != null)
                sphereRenderer.material.color = color;

            // Update TMP text
            TextMeshPro label = aaTransform.GetComponentInChildren<TextMeshPro>();
            if (label != null)
                label.text = aminoAcid;

            Debug.Log($"[AminoAcidGenerator] Anticodon {anticodon} → {aminoAcid}");
        }
        else
        {
            Debug.LogWarning($"[AminoAcidGenerator] Unknown anticodon: {anticodon}");
        }
    }
}
