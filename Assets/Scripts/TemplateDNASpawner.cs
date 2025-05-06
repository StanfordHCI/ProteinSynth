using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TemplateDNASpawner : MonoBehaviour
{
    [Header("Template DNA Settings")]
    public string dnaSequence = "TACGGCATTAGCTACGGC"; // Template strand bases (from 5' to 3')
    public GameObject basePrefab;
    public List<Transform> spawnPoints; // Match number of letters in sequence

    private void Start()
    {
        SpawnTemplateDNA();
    }

    public void SpawnTemplateDNA()
    {
        if (spawnPoints.Count != dnaSequence.Length)
        {
            Debug.LogWarning("Spawn point count does not match DNA sequence length!");
            return;
        }

        for (int i = 0; i < dnaSequence.Length; i++)
        {
            char baseChar = dnaSequence[i];
            Transform spawnPoint = spawnPoints[i];

            Quaternion fixedRotation = Quaternion.Euler(0f, 90f, -180f);

            GameObject newBase = Instantiate(basePrefab, spawnPoint.position, fixedRotation, spawnPoint);

            // Set base letter text
            TMP_Text text = newBase.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = baseChar.ToString();

            // Color the base
            Renderer rend = newBase.GetComponent<Renderer>();
            if (rend != null)
            {
                Color color = Color.white;
                switch (baseChar)
                {
                    case 'A': color = Color.green; break;
                    case 'T': color = Color.magenta; break;
                    case 'C': color = Color.blue; break;
                    case 'G': color = Color.yellow; break;
                }
                rend.material.color = color;
            }
        }
    }
}
