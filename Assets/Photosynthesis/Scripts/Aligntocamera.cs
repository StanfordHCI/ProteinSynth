using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aligntocamera : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Transform[] rays;
    public float dist;

    void Update()
    {
        transform.LookAt(Camera.main.transform,Vector3.down);
                foreach (Transform child in transform)
        {
    //        child.LookAt(Camera.main.transform);
            float d = (child.position - Camera.main.transform.position).magnitude;
            child.localScale = new Vector3(child.localScale.x, child.localScale.y, d-dist);
        }
    }
}
