using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveE : MonoBehaviour
{
    public Vector3 X0 = new Vector3(-.84f,-.71f,0); // First target Y position
    public float duration0 = 3f; // Time to reach A
    
    public Vector3 X1 = new Vector3(.58f,-.71f,0); // Second target Y position
    public float duration1 = 1f; // Time to reach B

    public Vector3 X2 = new Vector3(2.32f,-.71f,0); // Final target Y position

    public float duration2 = 4f; // Time to reach C
    public Vector3 emitX3;
    public float emitDuration3;
        public Vector3 emitX4;
    public float emitDuration4;
    public float StartAfter = 0f;
    public bool hide = true;
    public bool emiting = false;
    public InitPPos cg = InitPPos.none;
    public SetGraph top;
    public SetGraph bot;
    Material[] r = new Material[2];
    
    public enum InitPPos {
        top,
        bot, 
        none
    }

    void Start()
    {
        if(emiting)
        for (int i = 0; i < GetComponentsInChildren<Renderer>().Length; i++)
        {
            r[i] = GetComponentsInChildren<Renderer>()[i].material;
        }
        StartCoroutine(Begin());

    }

    IEnumerator Begin(){
        yield return new WaitForSeconds(StartAfter);
        foreach(Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled=true;
        StartCoroutine(MoveThroughStages());
    }

    IEnumerator MoveThroughStages()
    {
        if(emiting)
        FindObjectOfType<SetGraph>().Graph(10,.2f);
        yield return StartCoroutine(MoveToY(X0, duration0)); // Move to A
        switch (cg)
        {
            case InitPPos.top:
            top?.Graph(-1);
            break;
            case InitPPos.bot:
            bot?.Graph(-1);
            break;
            default:
            break;
        }
        
        if(emiting)
            {FindObjectOfType<SetGraph>().Graph(0,duration1);
            yield return StartCoroutine(MoveToY(X1, duration1,true)); // Decrease to C
            
            }
        else
            yield return StartCoroutine(MoveToY(X1, duration1));
        switch (cg)
        {
            case InitPPos.top:
            bot?.Graph(1);
            break;
            case InitPPos.bot:
            top?.Graph(1);
            break;
            default:
            break;
        }
        
        yield return StartCoroutine(MoveToY(X2, duration2)); // Decrease to C

        if(emiting)
        {
            FindObjectOfType<SetGraph>().Graph(10,.2f);
            foreach (var r in r)
            {
                r.SetColor("_EmissionColor", Color.green);
            }
            yield return StartCoroutine(MoveToY(emitX3, emitDuration3)); // Decrease to 

            FindObjectOfType<ColorLerp>().ChangeColor(Color.green,Color.red,emitDuration4);
            FindObjectOfType<SetGraph>().Graph(0,emitDuration4);
            yield return StartCoroutine(MoveToY(emitX4, emitDuration4,true)); // Decrease to C
        }
        if(hide)
        foreach(var r in GetComponentsInChildren<Renderer>())
            r.enabled=false;
    }

    IEnumerator MoveToY(Vector3 targetY, float duration, bool emit=false, bool decrease=true)
    {
        Vector3 startY = transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            Vector3 newY = Vector3.Lerp(startY, targetY, progress);
            transform.localPosition = newY;
            if(emit)
                {
                    float newE = Mathf.Lerp(1,0,progress);
                    
                    if(decrease)
                    foreach (var r in r)
                    {
                        //Debug.Log(r.material.GetColor("_EmissionColor").maxColorComponent);
                        r.SetColor("_EmissionColor", newE*Color.green);
                    }
                }
            yield return null; // Wait for the next frame
        }

        transform.localPosition = targetY; // Ensure final position
    }
}
