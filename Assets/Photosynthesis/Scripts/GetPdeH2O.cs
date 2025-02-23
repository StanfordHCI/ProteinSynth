using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetPdeH2O : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 H2OPos;
    public float H2ODuration;
    public float ProtonDuration;
    public float fadeTime;
    public float separationForce;

    public float StartAfter;
    public SetGraph bot;

    void Start() 
    {
        StartCoroutine(Begin());
        
    }

    IEnumerator Begin()
    {
                yield return new WaitForSeconds(StartAfter);
        Transform[] children = new Transform[transform.childCount];

        // Store child objects
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }
        yield return StartCoroutine(MoveToY(transform, H2OPos, H2ODuration));
        bot?.Graph(2);
        foreach(Transform child in children)
            if(child.tag == "Hydrogen")
            StartCoroutine(MoveToY(child, new Vector3(Random.Range(-.39f,3.3f),Random.Range(-.74f,-.09f)),ProtonDuration));
            else
            StartCoroutine(FadeOut(child));
    }
    IEnumerator FadeOut(Transform t)
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float fadeAmount = 1 - (elapsedTime / fadeTime);
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

    IEnumerator MoveToY(Transform t, Vector3 targetY, float duration, bool local = true)
    {
        Vector3 startY = local ? t.localPosition : t.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            Vector3 newY = Vector3.Lerp(startY, targetY, progress);
            if (local) t.localPosition = newY;
            else t.position = newY;
            yield return null; // Wait for the next frame
        }
        if (local)
        t.localPosition = targetY; // Ensure final position
        else t.position = targetY;
    }
    IEnumerator Separate(Transform obj, bool wait)
    {


        Vector3 Direction = obj.localPosition.normalized * separationForce;
        // Debug.Log(obj.name);
        // Debug.Log(obj.position);
        // Debug.Log(obj.localPosition);
        Vector3 startPosition = obj.transform.position;
        Vector3 targetPosition = startPosition + Direction;

        if(wait)
        yield return new WaitForSeconds(fadeTime);

        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;

            // Move outward
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        StartCoroutine(MoveToY(obj, new Vector3(Random.Range(-.39f,4.3f),Random.Range(-.74f,-.09f)),ProtonDuration));

    }
}
