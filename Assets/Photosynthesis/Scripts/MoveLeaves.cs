using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveLeaves : MonoBehaviour
{
    public float floatHeight = 5f; // Height to move up
    public float duration = 1f;   // Time each object takes to float up
    private Transform[] objects;

    void Start()
    {
        // Get all child objects
        int childCount = transform.childCount;
        objects = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            objects[i] = transform.GetChild(i);
            StartCoroutine(FloatObject(objects[i], i)); // Start each object sequentially
        }
    }

    IEnumerator FloatObject(Transform obj, int index)
    {
        yield return new WaitForSeconds(duration*index); // Delay start by index seconds

        Vector3 startPos = obj.localPosition;
        Vector3 endPos = startPos + Vector3.up * floatHeight;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            obj.localPosition = Vector3.Lerp(startPos, endPos, progress);
            yield return null; // Wait until the next frame
        }

        obj.localPosition = endPos; // Ensure it reaches the final position
    }
}
