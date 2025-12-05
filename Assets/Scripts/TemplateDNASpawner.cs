using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TemplateDNASpawner : MonoBehaviour
{
    public bool autoSpawn = false;

    [Header("Template Sequence (A, T, C, G, U)")]
    public string defaultSequence = "TACGGCATTAGCTAC"; 

    [Header("Base Prefabs")]
    public GameObject prefabA;
    public GameObject prefabT;
    public GameObject prefabC;
    public GameObject prefabG;
    public GameObject prefabU;

    [Header("3D Model Transforms (applied relative to spawn point)")]
    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    [SerializeField] private float zOffset;
    [SerializeField] private float xRotation;
    [SerializeField] private float yRotation;
    [SerializeField] private float zRotation;

    [Header("Spawn Settings")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 1f;

    [Header("Parent with Spawn Points")]
    public Transform spawnParent;

    private Coroutine activeCoroutine;                // spawning coroutine
    private readonly List<Coroutine> fadeCoroutines = new(); // track all fade-ins
    private string previousSequence = "";             // track previous sequence to detect changes

    private void Start()
    {
        if (autoSpawn)
            SpawnTemplateSequenceInstant();
    }

    public void SpawnTemplateSequenceInstant()
    {
        if (spawnParent == null)
        {
            Debug.LogWarning($"{name}: spawnParent not assigned!");
            return;
        }

        ClearAllChildren();

        string sequence = defaultSequence.ToUpper();
        int spawnCount = Mathf.Min(sequence.Length, spawnParent.childCount);

        for (int i = 0; i < spawnCount; i++)
        {
            char baseChar = sequence[i];
            Transform spawnPoint = spawnParent.GetChild(i);
            GameObject prefab = GetPrefab(baseChar);

            if (prefab == null)
            {
                Debug.LogWarning($"{name}: No prefab found for base '{baseChar}' at position {i}");
                continue;
            }

            Vector3 offset = new Vector3(xOffset, yOffset, zOffset);
            Vector3 spawnPos = spawnPoint.position + spawnPoint.TransformDirection(offset);
            Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

            GameObject spawned = Instantiate(prefab, spawnPos, spawnRot, spawnPoint);
        }

        // Reset previous sequence for instant spawn
        previousSequence = sequence;
    }


    public bool SpawnTemplateSequence()
    {
        return SpawnTemplateSequence(defaultSequence);
    }

    public bool SpawnTemplateSequence(string sequence)
    {
        if (spawnParent == null) return false;

        sequence = sequence.ToUpper();

        // cancel previous spawns
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        // cancel any running fades
        foreach (var fade in fadeCoroutines)
        {
            if (fade != null) StopCoroutine(fade);
        }
        fadeCoroutines.Clear();

        // Find the first index where sequences differ
        int firstDifferenceIndex = FindFirstDifference(previousSequence, sequence);

        // If sequences are identical, do nothing
        if (firstDifferenceIndex == -1 && previousSequence == sequence)
        {
            return true;
        }

        // Verify existing objects match up to the difference point
        bool existingMatches = true;
        if (firstDifferenceIndex > 0 && !string.IsNullOrEmpty(previousSequence))
        {
            existingMatches = CheckExistingSequenceMatchesUpTo(previousSequence, firstDifferenceIndex);
        }

        // If no previous sequence, completely different, or existing objects don't match, clear everything
        if (string.IsNullOrEmpty(previousSequence) || firstDifferenceIndex == 0 || !existingMatches)
        {
            ClearAllChildren();
            activeCoroutine = StartCoroutine(SpawnSequenceCoroutine(sequence, 0));
        }
        else
        {
            // Clear from first difference onward
            ClearChildrenFromIndex(firstDifferenceIndex);
            
            // Spawn new sequence from first difference onward
            string newPart = sequence.Substring(firstDifferenceIndex);
            activeCoroutine = StartCoroutine(SpawnSequenceCoroutine(newPart, firstDifferenceIndex));
        }

        // Update previous sequence
        previousSequence = sequence;
        return true;
    }

    private IEnumerator SpawnSequenceCoroutine(string sequence, int startIndex = 0)
    {
        int spawnCount = Mathf.Min(sequence.Length, spawnParent.childCount - startIndex);

        for (int i = 0; i < spawnCount; i++)
        {
            char baseChar = sequence[i];
            int actualIndex = startIndex + i;
            
            if (actualIndex >= spawnParent.childCount)
                break;

            Transform spawnPoint = spawnParent.GetChild(actualIndex);
            GameObject prefab = GetPrefab(baseChar);

            if (prefab != null)
            {
                Vector3 offset = new Vector3(xOffset, yOffset, zOffset);
                Vector3 spawnPos = spawnPoint.position + spawnPoint.TransformDirection(offset);
                if (actualIndex == 0) {
                    Debug.Log("spawnPoint.position" + spawnPoint.position);
                    Debug.Log("offset" + spawnPoint.TransformDirection(offset));
                    Debug.Log("spawnPos" + spawnPos);
                }
                Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

                GameObject spawned = Instantiate(prefab, spawnPos, spawnRot, spawnPoint);

                // Play pop sound when nucleotide is spawned with varied pitch
                if (AudioManager.Instance != null)
                {
                    // Vary pitch between 0.8 and 1.2 for variation
                    float pitchVariation = Random.Range(0.8f, 1.2f);
                    AudioManager.Instance.PlaySFXWithPitch("pop", pitchVariation);
                }

                if (useFadeIn)
                {
                    Coroutine fade = StartCoroutine(FadeIn(spawned, fadeInDuration));
                    fadeCoroutines.Add(fade);
                    yield return fade; // wait until finished before next object
                    fadeCoroutines.Remove(fade);
                }
            }
        }

        activeCoroutine = null; // finished
    }

    private int FindFirstDifference(string oldSequence, string newSequence)
    {
        if (string.IsNullOrEmpty(oldSequence))
            return 0;

        if (string.IsNullOrEmpty(newSequence))
            return 0;

        int minLength = Mathf.Min(oldSequence.Length, newSequence.Length);

        // Find first character that differs
        for (int i = 0; i < minLength; i++)
        {
            if (oldSequence[i] != newSequence[i])
            {
                return i;
            }
        }

        // If we get here, the shorter sequence is a prefix of the longer one
        // If new is longer, first difference is at the end of old
        // If old is longer, first difference is at the end of new (but we'll clear from minLength)
        if (newSequence.Length > oldSequence.Length)
        {
            return oldSequence.Length; // Extension - start from end of old
        }
        else if (oldSequence.Length > newSequence.Length)
        {
            return newSequence.Length; // Shortened - clear from this point
        }

        // Sequences are identical
        return -1;
    }

    private void ClearChildrenFromIndex(int startIndex)
    {
        if (spawnParent == null || startIndex < 0)
            return;

        int childCount = spawnParent.childCount;
        for (int i = startIndex; i < childCount; i++)
        {
            Transform spawnPoint = spawnParent.GetChild(i);
            for (int j = spawnPoint.childCount - 1; j >= 0; j--)
            {
                DestroyImmediate(spawnPoint.GetChild(j).gameObject);
            }
        }
    }

    private bool CheckExistingSequenceMatchesUpTo(string sequence, int upToIndex)
    {
        if (string.IsNullOrEmpty(sequence) || spawnParent == null || upToIndex <= 0)
            return false;

        int checkCount = Mathf.Min(upToIndex, sequence.Length, spawnParent.childCount);

        for (int i = 0; i < checkCount; i++)
        {
            Transform spawnPoint = spawnParent.GetChild(i);
            
            // If spawn point has no children, sequence doesn't match
            if (spawnPoint.childCount == 0)
                return false;

            // Get the expected prefab for this position
            char expectedBase = sequence[i];
            GameObject expectedPrefab = GetPrefab(expectedBase);

            if (expectedPrefab == null)
                return false;

            // Check if any child matches the expected prefab
            bool foundMatch = false;
            string expectedPrefabName = expectedPrefab.name;
            
            for (int j = 0; j < spawnPoint.childCount; j++)
            {
                GameObject child = spawnPoint.GetChild(j).gameObject;
                string childName = child.name;
                
                // Remove "(Clone)" suffix if present for comparison
                if (childName.EndsWith("(Clone)"))
                {
                    childName = childName.Substring(0, childName.Length - 7);
                }
                
                // Compare by checking if the child's name matches the prefab name
                // or contains the expected base character
                if (childName == expectedPrefabName || 
                    childName.Contains(expectedBase.ToString()))
                {
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
                return false;
        }

        return true;
    }

    private bool CheckExistingSequenceMatches(string sequence)
    {
        if (string.IsNullOrEmpty(sequence) || spawnParent == null)
            return false;

        int checkCount = Mathf.Min(sequence.Length, spawnParent.childCount);

        for (int i = 0; i < checkCount; i++)
        {
            Transform spawnPoint = spawnParent.GetChild(i);
            
            // If spawn point has no children, sequence doesn't match
            if (spawnPoint.childCount == 0)
                return false;

            // Get the expected prefab for this position
            char expectedBase = sequence[i];
            GameObject expectedPrefab = GetPrefab(expectedBase);

            if (expectedPrefab == null)
                return false;

            // Check if any child matches the expected prefab
            bool foundMatch = false;
            string expectedPrefabName = expectedPrefab.name;
            
            for (int j = 0; j < spawnPoint.childCount; j++)
            {
                GameObject child = spawnPoint.GetChild(j).gameObject;
                string childName = child.name;
                
                // Remove "(Clone)" suffix if present for comparison
                if (childName.EndsWith("(Clone)"))
                {
                    childName = childName.Substring(0, childName.Length - 7);
                }
                
                // Compare by checking if the child's name matches the prefab name
                // or contains the expected base character
                if (childName == expectedPrefabName || 
                    childName.Contains(expectedBase.ToString()))
                {
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
                return false;
        }

        return true;
    }

    private GameObject GetPrefab(char baseChar)
    {
        switch (char.ToUpper(baseChar))
        {
            case 'A': return prefabA;
            case 'T': return prefabT;
            case 'C': return prefabC;
            case 'G': return prefabG;
            case 'U': return prefabU;
            default: return null;
        }
    }

    public void ClearAllChildren()
    {
        foreach (Transform spawnPoint in spawnParent)
        {
            for (int i = spawnPoint.childCount - 1; i >= 0; i--)
                DestroyImmediate(spawnPoint.GetChild(i).gameObject);
        }
    }

    private IEnumerator FadeIn(GameObject obj, float duration)
    {
        if (obj == null) yield break;

        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) yield break;

        Material[] mats = renderer.materials;
        Color[] startColors = new Color[mats.Length];
        Color[] endColors = new Color[mats.Length];
        string[] props = { "_BaseColor", "_Color" };

        for (int i = 0; i < mats.Length; i++)
        {
            string chosenProp = null;
            foreach (var prop in props)
            {
                if (mats[i].HasProperty(prop))
                {
                    chosenProp = prop;
                    break;
                }
            }

            if (chosenProp != null)
            {
                endColors[i] = mats[i].GetColor(chosenProp);
                startColors[i] = endColors[i];
                startColors[i].a = 0f;
                mats[i].SetColor(chosenProp, startColors[i]);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < mats.Length; i++)
            {
                foreach (var prop in props)
                {
                    if (mats[i].HasProperty(prop))
                    {
                        mats[i].SetColor(prop, Color.Lerp(startColors[i], endColors[i], t));
                        break;
                    }
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            foreach (var prop in props)
            {
                if (mats[i].HasProperty(prop))
                {
                    mats[i].SetColor(prop, endColors[i]);
                    break;
                }
            }
        }
    }
}
