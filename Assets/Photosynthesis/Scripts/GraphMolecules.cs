using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphMolecules : MonoBehaviour
{
    public Transform O2;
    public Transform CO2;
    public float totTime;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Graph(CO2,true));
        StartCoroutine(Graph(O2, false));
    }


    IEnumerator Graph(Transform obj, bool decrease)
    {

        Vector3 startPosition = obj.localScale;
        Vector3 targetPosition;
        if(decrease)
        targetPosition = new Vector3(startPosition.x,0,startPosition.z);
        else
        targetPosition = new Vector3(startPosition.x,1,startPosition.z);

        float elapsedTime = 0f;

        while (elapsedTime < totTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / totTime;

            // Move outward
            obj.transform.localScale = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }



    }
}
