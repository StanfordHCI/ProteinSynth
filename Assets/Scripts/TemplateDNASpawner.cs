using UnityEngine;
using System.Collections;

public class TemplateDNASpawner : MonoBehaviour
{
    public bool autoSpawn = false;

    [Header("Template Sequence (A, T, C, G, U)")]
    [Tooltip("Can be any length. Will only spawn up to the available child spawn points.")]
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
    [SerializeField] private float fadeInDuration = 1f;

    [Header("Parent with Spawn Points")]
    [Tooltip("Parent that contains spawn points as children")]
    public Transform spawnParent;

    private void Start()
    {
        if (autoSpawn)
            SpawnTemplateSequence();
    }

    public void SpawnTemplateSequence()
    {
        SpawnTemplateSequence(defaultSequence);
    }

    public bool SpawnTemplateSequence(string sequence)
    {
        if (spawnParent == null)
        {
            Debug.LogWarning("Spawn parent not assigned!");
            return false;
        }

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
                Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(xRotation, yRotation, zRotation);

                GameObject spawned = Instantiate(prefab, spawnPos, spawnRot, spawnPoint);

                // Start fade-in
                StartCoroutine(FadeIn(spawned, fadeInDuration));
            }
            else
            {
                Debug.LogWarning($"No prefab found for base '{baseChar}' at position {i}");
            }
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

    // 🧹 Delete all spawned children instantly
    public void ClearAllChildren()
    {
        foreach (Transform spawnPoint in spawnParent)
        {
            for (int i = spawnPoint.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(spawnPoint.GetChild(i).gameObject);
            }
        }
    }

    // 🌟 Fade in new objects (URP Shader Graph compatible, with debug)
    private IEnumerator FadeIn(GameObject obj, float duration)
    {
        if (obj == null)
        {
            Debug.LogWarning("FadeIn: Object is null.");
            yield break;
        }

        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"FadeIn: No Renderer found on {obj.name}");
            yield break;
        }

        Material[] mats = renderer.materials;
        Debug.Log($"FadeIn: Found {mats.Length} materials on {obj.name}");

        // Store starting + ending colors
        Color[] startColors = new Color[mats.Length];
        Color[] endColors = new Color[mats.Length];

        for (int i = 0; i < mats.Length; i++)
        {
            Debug.Log($"FadeIn: Checking material {i}: {mats[i].name}");

            // Just test common color property names
            string[] possibleProps = { "_BaseColor", "_Color" };
            string chosenProp = null;

            foreach (var prop in possibleProps)
            {
                if (mats[i].HasProperty(prop))
                {
                    chosenProp = prop;
                    break;
                }
            }

            if (chosenProp != null)
            {
                Debug.Log($"FadeIn: Using {chosenProp} on {mats[i].name}");

                endColors[i] = mats[i].GetColor(chosenProp);
                startColors[i] = endColors[i];
                startColors[i].a = 0f; // start transparent
                mats[i].SetColor(chosenProp, startColors[i]);

                // Store property name in shader keywords for later use
                mats[i].SetFloat("_FadeInDebug", 1f); // just a debug marker
            }
            else
            {
                Debug.LogWarning($"FadeIn: No color property found on {mats[i].name}");
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty("_BaseColor"))
                {
                    Color newColor = Color.Lerp(startColors[i], endColors[i], t);
                    mats[i].SetColor("_BaseColor", newColor);
                    Debug.Log($"Fading {mats[i].name} via _BaseColor: alpha = {newColor.a:F2}");
                }
                else if (mats[i].HasProperty("_Color"))
                {
                    Color newColor = Color.Lerp(startColors[i], endColors[i], t);
                    mats[i].SetColor("_Color", newColor);
                    Debug.Log($"Fading {mats[i].name} via _Color: alpha = {newColor.a:F2}");
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final color
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i].HasProperty("_BaseColor"))
            {
                mats[i].SetColor("_BaseColor", endColors[i]);
                Debug.Log($"FadeIn complete for {mats[i].name} (_BaseColor).");
            }
            else if (mats[i].HasProperty("_Color"))
            {
                mats[i].SetColor("_Color", endColors[i]);
                Debug.Log($"FadeIn complete for {mats[i].name} (_Color).");
            }
        }
    }

}