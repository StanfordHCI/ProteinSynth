using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class tRNASpawner : MonoBehaviour
{
    [Header("References")]
    public Transform spawnParent;
    public GameObject tRNAPrefab;
    public Transform aminoAcidHolder; // drag your AminoAcidHolder (under mRNA) here

    [Header("Offsets")]
    public float xOffset;
    public float yOffset;
    public float zOffset;
    public float xRotation;
    public float yRotation;
    public float zRotation;

    [Header("Queue Settings")]
    public int maxVisibleTRNAs = 2;

    [Header("Timing Settings")]
    public float spawnDelay = 2.0f; // *unused now but still visible in inspector*
    public float hideDelay = 1.0f;

    [Header("Animation Settings")]
    public float enterAnimDuration = 2.0f; // how long the Enter animation lasts
    public float exitAnimDuration = 2.0f;  // how long the Exit animation lasts

    private Coroutine activeCoroutine;
    private readonly Queue<GameObject> activeTRNAs = new Queue<GameObject>();

    public void StartSpawning(string sequence)
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(SpawnSequenceCoroutine(sequence));
    }

    private IEnumerator SpawnSequenceCoroutine(string sequence)
    {
        if (aminoAcidHolder == null)
            Debug.LogWarning("[tRNASpawner] AminoAcidHolder is not assigned.");

        int spawnCount = Mathf.Min(sequence.Length, spawnParent.childCount);

        for (int i = 0; i < spawnCount; i++)
        {
            // Only spawn at the middle of each codon (i = 1, 4, 7, ...)
            if (i % 3 != 1) continue;

            Transform spawnPoint = spawnParent.GetChild(i);

            Vector3 spawnPos = spawnPoint.position +
                               spawnPoint.TransformDirection(new Vector3(xOffset, yOffset, zOffset));
            Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

            GameObject spawned = Instantiate(tRNAPrefab, spawnPos, spawnRot, spawnPoint);

            // Configure the tRNA’s internal spawner with the codon (i-1,i,i+1)
            string codon = (i - 1 >= 0 && i + 1 < sequence.Length) ? sequence.Substring(i - 1, 3) : "";
            Transform childSpawner = spawned.transform.Find("Model/trna spawner");
            if (childSpawner != null)
            {
                TemplateDNASpawner dnaSpawner = childSpawner.GetComponent<TemplateDNASpawner>();
                if (dnaSpawner != null)
                {
                    dnaSpawner.defaultSequence = codon;
                    dnaSpawner.SpawnTemplateSequence();
                }
            }

            // Play Enter animation
            Animator animator = spawned.GetComponent<Animator>();
            if (animator != null)
                animator.Play("Enter");

            // Play swoosh sound when tRNA enters
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("swoosh");
            }

            // Wait for Enter animation to finish
            yield return new WaitForSeconds(enterAnimDuration + 0.1f);

            // Play pop sound when Enter animation ends
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("pop");
            }

            // Move amino acid to ribosome holder
            Transform aminoAcid = spawned.transform.Find("Model/AminoAcid");
            if (aminoAcid != null && aminoAcidHolder != null)
                aminoAcid.SetParent(aminoAcidHolder, true);

            // Add new tRNA to active queue
            activeTRNAs.Enqueue(spawned);

            // If too many visible, offboard oldest IN PARALLEL
            if (activeTRNAs.Count > maxVisibleTRNAs)
            {
                GameObject oldest = activeTRNAs.Dequeue();
                StartCoroutine(OffboardTRNA(oldest)); // <-- non-blocking
            }

            // Spawn the next tRNA halfway through this one's exit
            yield return new WaitForSeconds(exitAnimDuration * 0.5f);
        }

        // After all spawns, remove any leftovers one-by-one
        while (activeTRNAs.Count > 0)
        {
            GameObject tRNA = activeTRNAs.Dequeue();
            yield return StartCoroutine(OffboardTRNA(tRNA));
            yield return new WaitForSeconds(hideDelay);
        }
        activeCoroutine = null;
        GlobalDialogueManager.StartDialogue("ProteinSynthesisCongrats");
        CodonTracker.instance.ToggleEndSceneDelayed(true);
    }

    private IEnumerator OffboardTRNA(GameObject tRNA)
    {
        if (tRNA == null) yield break;

        // Play Exit animation
        Animator animator = tRNA.GetComponent<Animator>();
        if (animator != null)
            animator.Play("Exit");

        // Play swoosh sound when tRNA exits
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("swoosh");
        }

        // Wait for exit to finish
        yield return new WaitForSeconds(exitAnimDuration);

        // Hide
        if (tRNA != null)
            tRNA.SetActive(false);
    }
}
