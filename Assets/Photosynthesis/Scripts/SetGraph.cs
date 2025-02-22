using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SetGraph : MonoBehaviour
{
    [SerializeField] Transform graph;
    public float totTime = 1f;
    float graphHeight = 10;
    bool graphit;
    // Start is called before the first frame update
    public void Graph(float amount)
    {
        graphHeight+= amount;
        graphit = true;
        
    }

    private void LateUpdate() {
        if(graphit)
        {
            StartCoroutine(Graph(graph, graphHeight/10f));
            graphit=false;
        }
    }

        IEnumerator Graph(Transform obj, float amount)
    {

        Vector3 startPosition = obj.localScale;
        Vector3 targetPosition;
        targetPosition = new Vector3(startPosition.x,amount,startPosition.z);


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
