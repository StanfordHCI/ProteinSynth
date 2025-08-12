using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TemplateDNASpawner : MonoBehaviour
{
    public bool autoSpawn = false;
    [Header("Template DNA Settings")]
    public string defaultDnaSequence = "TACGGCATTAGCTACGGC"; // Template strand bases (from 5' to 3')
    public GameObject basePrefab;
    public List<Transform> spawnPoints; // Match number of letters in sequence

    private Camera camera;

    private void Start()
    {
        // camera = MARSSession.Instance?.sessionCamera;
        if (camera == null) {
            camera = Camera.main;
        }

        if (autoSpawn) {
            SpawnTemplateDNA();
        }
    }

    void LateUpdate() {
        // Make sure text is always facing the camera
        // for (int i = 0; i < spawnPoints.Count; i++) {
        //     TMP_Text text = spawnPoints[i].GetComponentInChildren<TMP_Text>();
        //     if (text != null) {
        //         text.transform.LookAt(camera.transform.position);
        //         text.transform.rotation *= Quaternion.Euler(0f, 180f, 0f); // need to flip text around
        //     }
        // }
    }

    public void SpawnTemplateDNA() {
        SpawnTemplateDNA(defaultDnaSequence);
    }

    public bool SpawnTemplateDNA(string dnaSequence)
    {
        if (spawnPoints.Count != dnaSequence.Length)
        {
            Debug.LogWarning("Spawn point count does not match DNA sequence length!");
            return false;
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
        return true;
    }
}
