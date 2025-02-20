using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class SeparateCO2 : MonoBehaviour
{

    public float separationForce = 3f; // How far the objects separate
    public float dissolveTime = 2f; // Time before they disappear
    public Transform[] CO2s;
    public string parentName;
    int index=0;

    void OnTriggerEnter(Collider other)
    {
        if(other.transform.parent != null && other.transform.parent.name.Contains(parentName)) //doesn't collide with itself
        {
            Debug.Log(index);
            SeparateAndDissolve(other.transform.parent, CO2s[index]);
            index++;
        }

    }

    void SeparateAndDissolve(Transform parentObject, Transform CO2)
    {
        if (parentObject == null) return;


        Transform[] children = new Transform[parentObject.childCount];

        // Store child objects
        for (int i = 0; i < parentObject.childCount; i++)
        {
            children[i] = parentObject.GetChild(i);
        }

        // Remove parent and make children independent
        foreach (Transform child in children)
        {
            if (CO2==null)
            StartCoroutine(DissolveObject(child, true));
            else
            StartCoroutine(DissolveObject(child, false)); // Start dissolving
            child.SetParent(null); // Detach from parent
        }

        if(parentObject.tag=="H2O")
            SeparateAndDissolve(CO2, null);

        Destroy(parentObject.gameObject); // Destroy parent
    }

    IEnumerator DissolveObject(Transform obj, bool wait)
    {

        Vector3 Direction = obj.localPosition.normalized * separationForce;
        // Debug.Log(obj.name);
        // Debug.Log(obj.position);
        // Debug.Log(obj.localPosition);
        Vector3 startPosition = obj.transform.position;
        Vector3 targetPosition = startPosition + Direction;

        if(wait)
        yield return new WaitForSeconds(dissolveTime);

        float elapsedTime = 0f;

        while (elapsedTime < dissolveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dissolveTime;

            // Move outward
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }



    }
}
