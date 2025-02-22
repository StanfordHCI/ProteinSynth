using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyGraph : MonoBehaviour
{
    public float BondCO2 = .2f; // First target Y position
    public float durationCO2 = 3f; // Time to reach A
    public Transform Graph;

    float Max = 1; // Second target Y position
    public float durationMax = 1f; // Time to reach B

    public float BondO2 = .4f; // Final target Y position
    public float durationO2 = 4f; // Time to reach C

    void Start()
    {
        StartCoroutine(MoveThroughStages());
    }

    IEnumerator MoveThroughStages()
    {
        yield return StartCoroutine(MoveToY(BondCO2, durationCO2)); // Move to A
        Graph.localScale = new Vector3(Graph.localScale.x, 1, Graph.localScale.z);
        yield return new WaitForSeconds(.5f);
        yield return StartCoroutine(MoveToY(BondO2, durationO2)); // Decrease to C
    }

    IEnumerator MoveToY(float targetY, float duration)
    {
        float startY = Graph.localScale.y;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            float newY = Mathf.Lerp(startY, targetY, progress);
            Graph.localScale = new Vector3(Graph.localScale.x, newY, Graph.localScale.z);
            yield return null; // Wait for the next frame
        }

        Graph.localScale = new Vector3(Graph.localScale.x, targetY, Graph.localScale.z); // Ensure final position
    }
}
