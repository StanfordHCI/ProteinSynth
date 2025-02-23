using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSun : MonoBehaviour
{
 public Transform sun; // Assign the Sun GameObject in the Inspector
    public Transform startPosition; // Assign the Start Position in the Inspector
    public Transform[] endPositions; // Assign 12 End Positions in the Inspector
    public float travelTime = 10f; // Time to reach each end position
    public float waitTime = 0;
    public float initDelay = 0;
    private int currentTargetIndex = 0; // Keeps track of the current destination

    void Start()
    {

        if (sun != null && startPosition != null)
        {
            sun.position = startPosition.position; // Start at the initial position
            StartCoroutine(SetSun());
        }
        else
        {
            Debug.LogError("Please assign the Sun, Start Position, and 12 End Positions.");
        }
    }

    IEnumerator SetSun()
    {
        yield return new WaitForSeconds(initDelay);
        while (currentTargetIndex < endPositions.Length)
        {
            Transform targetPosition = endPositions[currentTargetIndex];
            foreach (Renderer r in sun.GetComponentsInChildren<Renderer>())
                r.enabled = true;
            yield return StartCoroutine(MoveToPosition(sun, targetPosition.position, travelTime));

            // Reset to start position
            sun.position = startPosition.position;
            foreach (Renderer r in sun.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            
            yield return new WaitForSeconds(waitTime);         
            // Move to the next end position
            currentTargetIndex++;

           // yield return new WaitForSeconds(1f); // Optional delay before moving again
        }
        yield return new WaitForSeconds(2f); // Optional delay before moving again
        FindObjectOfType<MakeProducts>()?.Assemble();
        FindObjectOfType<SeparateMolecules>()?.KillBond();

    }

    IEnumerator MoveToPosition(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            obj.position = Vector3.Lerp(start, target, progress);
            yield return null;
        }

        obj.position = target; // Ensure exact positioning at the end
    }
}
