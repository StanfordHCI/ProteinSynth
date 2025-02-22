using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCog : MonoBehaviour
{
    public float rotationTime = 2f; // Time to rotate (y seconds)
    public float waitTime = 3f; // Time before the next rotation (x seconds)
    private float targetRotationY; // Stores the next Y rotation

    public float rotateBy = 90f;
    public float initWait = 0;

    void Start()
    {
        targetRotationY = transform.eulerAngles.z;
        StartCoroutine(RotateLoop());
    }

    IEnumerator RotateLoop()
    {
        yield return new WaitForSeconds(initWait-waitTime);
        int i = 0;
        while (i<3) // Infinite loop
        {
            targetRotationY += rotateBy; // Increase target rotation
            yield return new WaitForSeconds(waitTime); // Wait x seconds before rotating again

            yield return StartCoroutine(RotateOverTime(targetRotationY, rotationTime));
            i++;

        }
    }

    IEnumerator RotateOverTime(float targetY, float duration)
    {
        float startY = transform.eulerAngles.z;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            float newY = Mathf.LerpAngle(startY, targetY, progress);
            transform.rotation = Quaternion.Euler(0, 0, newY);
            yield return null; // Wait for the next frame
        }

        transform.rotation = Quaternion.Euler(0, 0, targetY); // Ensure final rotation
    }
}
