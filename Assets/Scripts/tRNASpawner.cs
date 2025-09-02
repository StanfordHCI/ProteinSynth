using System.Collections;
using UnityEngine;

public class tRNASpawner : MonoBehaviour
{
    [Header("References")]
    public Transform spawnParent;
    public GameObject tRNAPrefab;

    [Header("Offsets")]
    public float xOffset;
    public float yOffset;
    public float zOffset;
    public float xRotation;
    public float yRotation;
    public float zRotation;

    private Coroutine activeCoroutine;

    public void StartSpawning(string sequence)
    {
        Debug.Log("Spawning tRNA");
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(SpawnSequenceCoroutine(sequence));
    }

    private IEnumerator SpawnSequenceCoroutine(string sequence)
    {
        int spawnCount = Mathf.Min(sequence.Length, spawnParent.childCount);

        for (int i = 0; i < spawnCount; i++)
        {
            // ✅ Only spawn at the 2nd char of each group of 3 (i = 1, 4, 7, ...)
            if (i % 3 == 1)
            {
                Transform spawnPoint = spawnParent.GetChild(i);

                Vector3 offset = new Vector3(xOffset, yOffset, zOffset);
                Vector3 spawnPos = spawnPoint.position + spawnPoint.TransformDirection(offset);
                Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

                // Spawn prefab
                GameObject spawned = Instantiate(tRNAPrefab, spawnPos, spawnRot, spawnPoint);

                // 🔹 Grab codon triplet (i-1, i, i+1) safely
                string codon = "";
                if (i - 1 >= 0 && i + 1 < sequence.Length)
                {
                    codon = sequence.Substring(i - 1, 3);
                }

                // Pass codon to TemplateDNASpawner and start spawning immediately
                TemplateDNASpawner dnaSpawner = spawned.GetComponent<TemplateDNASpawner>();
                if (dnaSpawner != null)
                {
                    dnaSpawner.defaultSequence = codon;
                    dnaSpawner.SpawnTemplateSequence();
                }

                Debug.Log("Spawning tRNA:" + codon);

                // 👉 TODO: Trigger animation if needed
                // spawned.GetComponent<Animator>()?.SetTrigger("Play");

                yield return new WaitForSeconds(1.0f); // wait before spawning next
            }
        }

        activeCoroutine = null; // finished
    }
}
