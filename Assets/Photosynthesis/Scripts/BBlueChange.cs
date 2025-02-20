using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BBlueChange : MonoBehaviour
{
public Material targetMaterial; // Assign this in the Inspector
    public Color startColor = Color.green;
    public Color endColor = Color.blue;
    public float duration = 10f;
    private float elapsedTime = 0f;

    void Update()
    {
        if (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            targetMaterial.color = Color.Lerp(startColor, endColor, progress);
        }
        else
        {
            targetMaterial.color = endColor;
        }
    }
}
