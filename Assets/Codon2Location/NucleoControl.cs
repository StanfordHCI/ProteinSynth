using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class NucleoControl : MonoBehaviour
{
    // Start is called before the first frame update
    public float codspeed = 2f;
    public float noiseScale = 1f;
    public float noiseStrength = 2f;
    Vector3 headPos = Vector3.zero;
  //  Rigidbody rb;
    private float offsetX, offsetZ, offsetY;
    public Vector3[] codonPos = new Vector3[3];
     bool go2codon = false; 
     bool fst, scnd,thrd;
    [SerializeField] Transform[] codons = new Transform[3];
    Transform head;
    void Awake()
    {
        Transform[] t = GetComponentsInChildren<Transform>();
        head = t[1]; //first is the parent because dumb
        for (int i = 0; i < codons.Length; i++)
        {
            codons[i] = t[i+2];
        }
    }
    void Start()
    {
        offsetX = Random.Range(0f, 100f);
        offsetY = Random.Range(0f, 100f);
        offsetZ = Random.Range(0f, 100f);
        head.position = 1.0f/3*(codons[0].position+codons[1].position+codons[2].position) +Vector3.up;
 //       rb = GetComponent<Rigidbody>();
    }

    public void FoundFirst(){
        fst = true;
    }
    public void LostFirst(){
        fst = false;
    }
    public void FoundSecd(){
        scnd = true;
    }
    public void LostSecd(){
        scnd = false;
    }
    public void FoundThird(){
        thrd = true;
    }
    public void LostThird(){
        thrd = false;
    }
    void Update()
    {


        float x = (Mathf.PerlinNoise(Time.time * noiseScale + offsetX, 0f)   -.5f ) * noiseStrength;
        float y = (Mathf.PerlinNoise(Time.time * noiseScale + offsetY, 300f) -.5f ) * noiseStrength;
        float z = (Mathf.PerlinNoise(0f, Time.time * noiseScale + offsetZ)   -.5f ) * noiseStrength;

        transform.position += new Vector3(x, y, z) * Time.deltaTime;

        if(fst == scnd == thrd == true) //this shoud be a coroutine. this is poling which is so inefficient. shame shame
        //this does kind of change direction when the codons present themselves, but making it more organic would mean using rigid bodies and then your dealing with forces which
        //is kind of unnecessary
        {
            headPos = 1.0f/3*(codonPos[0]+codonPos[1]+codonPos[2]);
            if((transform.position - headPos).magnitude < .01) 
            {transform.position = headPos;}
            else
            {
            transform.position = Vector3.MoveTowards(transform.position, headPos, codspeed * Time.deltaTime);}

            //update internal codons
            for (int i=0; i<codons.Length; i++)
            {
                Debug.Log(i);
                codons[i].position =  Vector3.MoveTowards(codons[i].position, codonPos[i], codspeed * Time.deltaTime);
            }
            head.position = 1.0f/3*(codons[0].position+codons[1].position+codons[2].position) +Vector3.up;

        }



    }
    //     void FixedUpdate()
    // {
    //     // Generate Perlin Noise values for each velocity component
    //     float noiseX = (Mathf.PerlinNoise(Time.time * noiseScale + offsetX, 0f) - 0.5f) * 2f;
    //     float noiseY = (Mathf.PerlinNoise(Time.time * noiseScale + offsetY, 0f) - 0.5f) * 2f;
    //     float noiseZ = (Mathf.PerlinNoise(Time.time * noiseScale + offsetZ, 0f) - 0.5f) * 2f;

    //     // Set velocity based on Perlin noise
    //     rb.velocity = new Vector3(noiseX, noiseY, noiseZ) * speed;
    // }
}
