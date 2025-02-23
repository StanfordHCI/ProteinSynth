using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorLerp : MonoBehaviour
{
    [SerializeField] Renderer currentC;
    Material mat;
    private void Start() {
        mat = currentC.material;
    }
    public void ChangeColor(Color startColor, Color NewColor, float duration)
    {
        StartCoroutine(ChangeColorOverTime(startColor, NewColor,duration));
    }
    IEnumerator ChangeColorOverTime(Color startColor, Color endColor, float duration)
    {
        float elapsedTime = 0f;
        Debug.Log("change");

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            mat.color = Color.Lerp(startColor, endColor, progress);
            yield return null; // Wait for the next frame
        }

        mat.color = Color.gray;; // Ensure final color is set
    }
}
