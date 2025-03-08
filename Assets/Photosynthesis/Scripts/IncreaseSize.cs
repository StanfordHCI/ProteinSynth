using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncreaseSize : MonoBehaviour
{
    public float targetRadius = 3f; // Final radius (B)
    public float duration = 2f; // Time to scale (C seconds)

    void Start()
    {
        StartCoroutine(ScaleOverTime(transform.localScale.y, targetRadius, duration));
    }

    IEnumerator ScaleOverTime(float startSize, float endSize, float time)
    {
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.one * startSize; // Convert radius to diameter
        Vector3 endScale = Vector3.one * endSize;

        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / time;
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null; // Wait for the next frame
        }

        transform.localScale = endScale; // Ensure exact final scale
    }
}
