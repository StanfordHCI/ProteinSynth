using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Float : MonoBehaviour
{
 // Start is called before the first frame update

    public float noiseScale = 1f;
    public float noiseStrength = 2f;
    public float waitToMove = 0;
    public bool substrate=false;
    public bool connect = true;

    [SerializeField] Transform emzymePos;
    public float enzymeTime;
    public float atEnzymeTime;


  //  Rigidbody rb;
    private float offsetX, offsetZ, offsetY;

        void Start()
    {
        offsetX = Random.Range(0f, 100f);
        offsetY = Random.Range(0f, 100f);
        offsetZ = Random.Range(0f, 100f);
        StartCoroutine(floatAround());
    }

        IEnumerator floatAround()
    {
        float elapsedTime = 0f;

        while (elapsedTime < waitToMove || !substrate)
        {
            elapsedTime += Time.deltaTime;

            float x = (Mathf.PerlinNoise(Time.time * noiseScale + offsetX, 0f)   -.5f ) * noiseStrength;
            float y = (Mathf.PerlinNoise(Time.time * noiseScale + offsetY, 300f) -.5f ) * noiseStrength;
            float z = (Mathf.PerlinNoise(0f, Time.time * noiseScale + offsetZ)   -.5f ) * noiseStrength;

            transform.position += new Vector3(x, y, z) * Time.deltaTime;
            yield return null; // Wait for the next frame
        }



        yield return StartCoroutine(MoveToY(emzymePos, enzymeTime)); // Move to A
        if(!connect)
        {
            substrate = false;
            StartCoroutine(floatAround());
            if(emzymePos.GetComponent<Renderer>()!=null)
            {
                emzymePos.GetComponent<Renderer>().enabled = false; //remove that hydrogen
                
                foreach (Transform c in transform)
                {
                    c.gameObject.SetActive(true);
                }
            }

        }
        Destroy(transform.GetComponent<Rigidbody>());
        yield return new WaitForSeconds(atEnzymeTime);
        if(transform.name.Contains("A")) //part of substrate A
        StartCoroutine(transform.parent.GetComponent<EnzymeFloat>().floatAround()); // Move to A

    }

    IEnumerator MoveToY(Transform targetY, float duration)
    {
        Vector3 Pos = transform.position;
        Quaternion Rot = transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration; // Normalize progress (0 to 1)
            Vector3 newPos = Vector3.Lerp(Pos, targetY.position, progress);
            Quaternion newRot = Quaternion.Lerp(Rot,targetY.rotation,progress);
            transform.position = newPos;
            transform.rotation = newRot;

            yield return null; // Wait for the next frame
        }

        transform.position = targetY.position; // Ensure final position
        transform.rotation = targetY.rotation;
    }

}
