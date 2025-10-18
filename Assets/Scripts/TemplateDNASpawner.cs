using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TemplateDNASpawner : MonoBehaviour
{
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

    public bool SpawnTemplateSequence()
    {
        return SpawnTemplateSequence(defaultSequence);
    }

    public bool SpawnTemplateSequence(string sequence)
    {
        if (spawnParent == null) return false;

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

        // wipe before spawning fresh
        ClearAllChildren();

        // start new coroutine
        activeCoroutine = StartCoroutine(SpawnSequenceCoroutine(sequence));
        return true;
    }

    private IEnumerator SpawnSequenceCoroutine(string sequence)
    {
        int spawnCount = Mathf.Min(sequence.Length, spawnParent.childCount);

        for (int i = 0; i < spawnCount; i++)
        {
            char baseChar = sequence[i];
            Transform spawnPoint = spawnParent.GetChild(i);
            GameObject prefab = GetPrefab(baseChar);

            if (prefab != null)
            {
                Vector3 offset = new Vector3(xOffset, yOffset, zOffset);
                Vector3 spawnPos = spawnPoint.position + spawnPoint.TransformDirection(offset);
                if (i == 0) {
                    Debug.Log("spawnPoint.position" + spawnPoint.position);
                    Debug.Log("offset" + spawnPoint.TransformDirection(offset));
                    Debug.Log("spawnPos" + spawnPos);
                }
                Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

                GameObject spawned = Instantiate(prefab, spawnPos, spawnRot, spawnPoint);

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
