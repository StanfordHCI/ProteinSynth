/*
    RatioBarUI.cs - attached to the RatioBar GameObject
    Watches PunnettSquareTracker.activeGenotypes and animates segments in/out
    Used to visualize genotypic ratios of Punnett squares as they are assembled
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RatioBarUI : MonoBehaviour
{
    [Header("Segments")]
    public LayoutElement[] segments;
    public Image[] segmentImages;
    public TextMeshProUGUI[] segmentLabels;

    [Header("Labels")]
    public TextMeshProUGUI counterAA;
    public TextMeshProUGUI counterAa;
    public TextMeshProUGUI counteraa;

    private float[] targetWidths;
    private Coroutine[] segmentCoroutines;
    private string[] lastState;

    private static readonly Dictionary<string, Color> genotypeColors = new()
    {
        { "A_A_", new Color(1f, 0.2f, 0.8f) },
        { "A_a",  new Color(1f, 0.65f, 0f) },
        { "aa",   new Color(0.353f, 0.392f, 1f) }  // #5A64FF
    };

    // Sorting order for genotypes
    private static readonly Dictionary<string, int> genotypeSortOrder = new()
    {
        { "A_A_", 0 },
        { "A_a",  1 },
        { "aa",   2 }
    };


    void Start()
    {
        targetWidths = new float[4];
        segmentCoroutines = new Coroutine[4];
        lastState = new string[4];

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].flexibleWidth = 0f;
            segmentImages[i].color = new Color(1f, 1f, 1f, 0f);
        }
    }

    void Update()
    {
        if (PunnettSquareTracker.instance == null) return;

        string[] current = PunnettSquareTracker.instance.GetActiveGenotypes();

        // Sort filled slots in AA → Aa → aa order, empty slots at the end
        System.Array.Sort(current, (a, b) =>
        {
            bool aEmpty = string.IsNullOrEmpty(a);
            bool bEmpty = string.IsNullOrEmpty(b);

            if (aEmpty && bEmpty) return 0;
            if (aEmpty) return 1;
            if (bEmpty) return -1;

            int aOrder = genotypeSortOrder.TryGetValue(a, out int ao) ? ao : 99;
            int bOrder = genotypeSortOrder.TryGetValue(b, out int bo) ? bo : 99;
            return aOrder.CompareTo(bOrder);
        });
        
        // Only update if something changed
        bool changed = false;
        for (int i = 0; i < 4; i++)
        {
            if (current[i] != lastState[i])
            {
                changed = true;
                break;
            }
        }

        if (!changed) return;

        lastState = (string[])current.Clone();
        UpdateSegments(current);
        UpdateLabels(current);
    }

    void UpdateSegments(string[] genotypes)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            string g = genotypes[i];
            bool isEmpty = string.IsNullOrEmpty(g);
            float newTarget = isEmpty ? 0f : 1f;

            // Always update label and colors
            if (!isEmpty)
            {
                segmentLabels[i].text = GenotypeDisplayName(g);
                segmentLabels[i].color = GetLabelColor(g);

                // Immediately correct the image color if segment is already visible
                if (genotypeColors.TryGetValue(g, out Color correctColor))
                {
                    correctColor.a = segmentImages[i].color.a;
                    segmentImages[i].color = correctColor;
                }
            }
            else
            {
                segmentLabels[i].text = "";
            }

            if (Mathf.Approximately(newTarget, targetWidths[i])) continue;

            targetWidths[i] = newTarget;

            if (segmentCoroutines[i] != null)
                StopCoroutine(segmentCoroutines[i]);

            if (newTarget > 0f && genotypeColors.TryGetValue(g, out Color col))
                segmentCoroutines[i] = StartCoroutine(AnimateIn(i, col));
            else
                segmentCoroutines[i] = StartCoroutine(AnimateOut(i));
        }
    }

    // Convert internal encoding to display string
    string GenotypeDisplayName(string g) => g switch
    {
        "A_A_" => "AA",
        "A_a"  => "Aa",
        "aa"   => "aa",
        _      => g
    };

    // Dark label on light backgrounds, light on dark
    Color GetLabelColor(string g) => g switch
    {
        "A_A_" => new Color(0.6f, 0f, 0.4f),    // dark pink
        "A_a"  => new Color(0.6f, 0.35f, 0f),   // dark orange
        "aa"   => new Color(0.145f, 0.075f, 0.463f),  // #251376
        _      => Color.black
    };

    void UpdateLabels(string[] genotypes)
    {
        int countAA = 0, countAa = 0, countaa = 0;
        foreach (string g in genotypes)
        {
            if (g == "A_A_") countAA++;
            else if (g == "A_a") countAa++;
            else if (g == "aa") countaa++;
        }

        counterAA.text = $"{countAA}";
        counterAa.text = $"{countAa}";
        counteraa.text = $"{countaa}";
    }

    IEnumerator AnimateIn(int index, Color targetColor)
    {
        float elapsed = 0f;
        float duration = 0.35f;
        float startWidth = segments[index].flexibleWidth;
        Color startColor = segmentImages[index].color;
        targetColor.a = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            segments[index].flexibleWidth = Mathf.Lerp(startWidth, 1f, ElasticOut(t));
            segmentImages[index].color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        segments[index].flexibleWidth = 1f;
        segmentImages[index].color = targetColor;
    }

    IEnumerator AnimateOut(int index)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        float startWidth = segments[index].flexibleWidth;
        Color startColor = segmentImages[index].color;
        Color endColor = startColor;
        endColor.a = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            segments[index].flexibleWidth = Mathf.Lerp(startWidth, 0f, t * t);
            segmentImages[index].color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        segments[index].flexibleWidth = 0f;
        segmentImages[index].color = endColor;
    }

    float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        float p = 0.3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }
}