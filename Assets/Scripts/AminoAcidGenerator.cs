using UnityEngine;
using TMPro;

public class AminoAcidGenerator : MonoBehaviour
{
    [SerializeField] private AminoAcidData aminoAcidData;
    private TemplateDNASpawner dnaSpawner;

    void Start()
    {
        dnaSpawner = GetComponent<TemplateDNASpawner>();

        if (dnaSpawner == null)
        {
            Debug.LogError("No TemplateDNASpawner found on this object!");
            return;
        }

        if (aminoAcidData == null)
        {
            Debug.LogError("AminoAcidData reference not assigned in the Inspector!");
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

        // Use helper function from AminoAcidData
        var (aminoAcid, color) = aminoAcidData.GetAminoAcidFromAnticodon(anticodon);

        if (aminoAcid != null)
        {
            // Find AminoAcid child of this spawner’s parent
            Transform aaTransform = transform.parent.Find("AminoAcid");
            if (aaTransform == null)
            {
                Debug.LogWarning("No AminoAcid child found under parent of tRNA spawner.");
                return;
            }

            // Change sphere color
            Renderer sphereRenderer = aaTransform.GetComponent<Renderer>();
            if (sphereRenderer != null)
                sphereRenderer.material.color = color;

            // Change text on the TMP label
            TextMeshPro label = aaTransform.GetComponentInChildren<TextMeshPro>();
            if (label != null)
                label.text = aminoAcid;

            Debug.Log($"Anticodon {anticodon} → {aminoAcid}");
        }
        else
        {
            Debug.LogWarning($"Unknown anticodon: {anticodon}");
        }
    }
}
