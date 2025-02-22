using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveE : MonoBehaviour
{
    public Vector3 X0 = new Vector3(-.84f,-.71f,0); // First target Y position
    public float duration0 = 3f; // Time to reach A
    Transform Graph;
    
    public Vector3 X1 = new Vector3(.58f,-.71f,0); // Second target Y position
    public float duration1 = 1f; // Time to reach B

    public Vector3 X2 = new Vector3(2.32f,-.71f,0); // Final target Y position
    public float duration2 = 4f; // Time to reach C
    public float StartAfter = 0f;
    public bool hide = true;
    public InitPPos cg = InitPPos.none;
    public SetGraph top;
    public SetGraph bot;
    
    public enum InitPPos {
        top,
        bot, 
        none
    }

    void Start()
    {
        Graph = transform;
        StartCoroutine(Begin());

    }

    IEnumerator Begin(){
        yield return new WaitForSeconds(StartAfter);
        foreach(Renderer r in Graph.GetComponentsInChildren<Renderer>())
            r.enabled=true;
        StartCoroutine(MoveThroughStages());
    }

    IEnumerator MoveThroughStages()
    {
        yield return StartCoroutine(MoveToY(X0, duration0)); // Move to A
        switch (cg)
        {
            case InitPPos.top:
            top.Graph(-1);
            break;
            case InitPPos.bot:
            bot.Graph(-1);
            break;
            default:
            break;
        }
        yield return StartCoroutine(MoveToY(X1, duration1));
                switch (cg)
        {
            case InitPPos.top:
            bot.Graph(1);
            break;
            case InitPPos.bot:
            top.Graph(1);
            break;
            default:
            break;
        }
        yield return StartCoroutine(MoveToY(X2, duration2)); // Decrease to C
        if(hide)
        foreach(Renderer r in Graph.GetComponentsInChildren<Renderer>())
            r.enabled=false;
    }

    IEnumerator MoveToY(Vector3 targetY, float duration)
    {
        Vector3 startY = Graph.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            Vector3 newY = Vector3.Lerp(startY, targetY, progress);
            Graph.localPosition = newY;
            yield return null; // Wait for the next frame
        }

        Graph.localPosition = targetY; // Ensure final position
    }
}
