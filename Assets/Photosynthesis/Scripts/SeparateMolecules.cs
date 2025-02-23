using System.Collections;
using System.Collections.Generic;
using Unity.MARS.Actions;
using UnityEditor.Animations;
using UnityEngine;

public class SeparateMolecules : MonoBehaviour
{

    public float separationForce = 3f; // How far the objects separate
    public float dissolveTime = 2f; // Time before they disappear
    public Transform[] CO2s;
    Transform[] bonds = new Transform[24];
    public string parentName;
    int index=0;

    void OnTriggerEnter(Collider other)
    {
        if(other.transform.parent != null && other.transform.parent.name.Contains(parentName)) //doesn't collide with itself
        {
            KillParent(other.transform.parent, CO2s[index]);
            index++;
        }

    }

    void Awake()
    {
      var GO = GameObject.FindGameObjectsWithTag("Bond");
        for (int i = 0; i < GO.Length; i++)
        {
            bonds[i]  = GO[i].transform;
        }
    }

    public void KillBond()
    {
        foreach(var b in bonds)
            Destroy(b?.gameObject);
    }

    void KillParent(Transform parentObject, Transform CO2)
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
            if (child.tag == "Bond")
            StartCoroutine(Stretch(child));
            else if (CO2==null)
            StartCoroutine(Separate(child, true));
            else
            StartCoroutine(Separate(child, false)); // Start dissolving
            child.SetParent(null); // Detach from parent
        }

        if(parentObject.tag=="H2O")
            KillParent(CO2, null);

        Destroy(parentObject.gameObject); // Destroy parent
    }

    IEnumerator Stretch(Transform bond)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = bond.localScale;
        Vector3 targetScale = new Vector3(separationForce, originalScale.y, originalScale.z);

        while (elapsedTime < dissolveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dissolveTime; // Normalize progress (0 to 1)
            bond.localScale = Vector3.LerpUnclamped(originalScale, targetScale, progress);
            yield return null; // Wait for next frame
        }

        bond.localScale = targetScale; // Ensure final scale
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
