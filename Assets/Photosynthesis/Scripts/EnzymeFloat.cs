using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnzymeFloat : MonoBehaviour
{
 // Start is called before the first frame update

    public float noiseScale = 1f;
    public float noiseStrength = 2f;



   


  //  Rigidbody rb;
    private float offsetX, offsetZ, offsetY;

        void Start()
    {
        offsetX = Random.Range(0f, 100f);
        offsetY = Random.Range(0f, 100f);
        offsetZ = Random.Range(0f, 100f);
 //       rb = GetComponent<Rigidbody>();
    }

        public IEnumerator floatAround()
    {

        float elapsedTime = 0f;

        while (true)
        {


            float x = (Mathf.PerlinNoise(Time.time * noiseScale + offsetX, 0f)   -.5f ) * noiseStrength;
            float y = (Mathf.PerlinNoise(Time.time * noiseScale + offsetY, 300f) -.5f ) * noiseStrength;
            float z = (Mathf.PerlinNoise(0f, Time.time * noiseScale + offsetZ)   -.5f ) * noiseStrength;

            transform.position += new Vector3(x, y, z) * Time.deltaTime;
            yield return null; // Wait for the next frame
        }
    }

    

}
