using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrateNSplit : MonoBehaviour
{
public float vibrationDuration = 7f; // Total time before breaking (X seconds)
public float moveDuration = 1;
public float fadeDuration = 2;
    public float maxFrequency = 20f; // Max speed of vibration
    public float minFrequency = 10;
    public float breakDistance = 2f; // Distance objects separate (Y units)
    public float separationSpeed = 2f; // Speed of breaking apart

    private float elapsedTime = 0f;
    private bool isBreaking = false;
    public Transform left, right, bond, Carbon, Glucose;



    void Start()
    {
                
        StartCoroutine(VibrateAndSplit());


    }



    IEnumerator VibrateAndSplit()
    {
        Vector3 p1Start = left.localPosition;
        Vector3 p2Start = right.localPosition;
        while (elapsedTime < vibrationDuration)
        {
            elapsedTime += Time.deltaTime;

            // Frequency increases over time
            float frequency = Mathf.Lerp(minFrequency, maxFrequency, elapsedTime / vibrationDuration);
            float offset = Mathf.Sin(Time.time * frequency) * 0.05f; // Side-to-side motion
           // Debug.Log(offset);
            // Increasing separation over time
            float separationFactor = Mathf.Lerp(0f, breakDistance, elapsedTime / vibrationDuration);

            left.localPosition = p1Start + Vector3.left * (separationFactor + offset);
            right.localPosition = p2Start + Vector3.right * (separationFactor - offset);
            bond.localScale = new Vector3(right.localPosition.x-left.localPosition.x, bond.localScale.y, bond.localScale.z);

            yield return null;
        }

        isBreaking = true;
        bond.gameObject.SetActive(false);
        left.localPosition = p1Start + Vector3.left * (breakDistance + .1f);
        right.localPosition = p2Start + Vector3.right * (breakDistance + .1f);

        yield return new WaitForSeconds(.5f);
        StartCoroutine(MoveToPosition(Carbon, Glucose.position, true));
        StartCoroutine(MoveToPosition(left,left.parent.TransformPoint(p1Start)));
        StartCoroutine(MoveToPosition(right,right.parent.TransformPoint(p2Start)));
    }

    IEnumerator MoveToPosition(Transform t, Vector3 finalPos, bool destroy = false)
    {
        float Duration;
        if(!destroy)
        Duration = moveDuration+fadeDuration;
        else
        Duration = moveDuration;

        Vector3 startPosition = t.position;
        float elapsedTime = 0f;

        while (elapsedTime < Duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / Duration;
            t.position = Vector3.Lerp(startPosition, finalPos, progress);
            if(!destroy)
            {
                if(right.localPosition.x-left.localPosition.x < breakDistance*2)
                {
                    bond.gameObject.SetActive(true);
                    bond.localScale = new Vector3(right.localPosition.x-left.localPosition.x, bond.localScale.y, bond.localScale.z);
                }
            }
            yield return null;
        }

        t.position = finalPos; // Ensure final position

        if(destroy)
        {StartCoroutine(FadeOut(t));
        StartCoroutine(FadeIn(Glucose));}

    }

    IEnumerator FadeOut(Transform t)
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float fadeAmount = 1 - (elapsedTime / fadeDuration);
            Renderer objRenderer = t.GetComponent<Renderer>();
            if (objRenderer != null)
            {
                Color newColor = objRenderer.material.color;
                newColor.a = fadeAmount; // Adjust alpha
                objRenderer.material.color = newColor;
            }

            yield return null;
        }

        Destroy(t.gameObject); // Destroy after fading out (optional)
    }

    IEnumerator FadeIn(Transform t) 
    {
       // yield return new WaitForSeconds(moveDuration);
        
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            
            float fadeAmount = (elapsedTime / fadeDuration);
            //Renderer objRenderer = t.GetComponent<Renderer>();
            foreach(var objRenderer in t.GetComponentsInChildren<Renderer>())
            {
            if (objRenderer != null)
            {
                Color newColor = objRenderer.material.color;
                newColor.a = fadeAmount; // Adjust alpha
                objRenderer.material.color = newColor;
            }
            objRenderer.enabled = true;
            }
            elapsedTime += Time.deltaTime/2;
            yield return null;
        }
    }


}
