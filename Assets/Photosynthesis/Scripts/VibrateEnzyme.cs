using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrateEnzyme : MonoBehaviour
{


    public float frequency = 15;
    public float breakDistance = 2f; // Distance objects separate (Y units)
    public float separationSpeed = 2f; // Speed of breaking apart

    public Transform left, right;
    Float[] floats;
    EnzymeFloat[] efloat;

    private void Start() {
        floats = FindObjectsOfType<Float>();
        efloat = FindObjectsOfType<EnzymeFloat>();
        StartCoroutine(VibrateAndSplit());
    }
    public void SetLow()
    {
        frequency = 14;
        breakDistance = -.08f;
        separationSpeed = .21f;
        foreach (var f in floats)
        {
            f.noiseScale = .2f;
            f.noiseStrength = 7;
            f.enzymeTime = .5f;
            f.atEnzymeTime = .25f;
        };
        foreach (var f in efloat)
        {
            f.noiseScale = .2f;
            f.noiseStrength = 7;
        }

    }

    public void SetDead()
    {
        foreach (var f in floats)
        {
            f.connect = false;
        }
    }

    public void SetMed()
    {
        frequency = 60;
        breakDistance = -.61f;
        separationSpeed = .52f;
        foreach (var f in floats)
        {
            f.noiseScale = .5f;
            f.noiseStrength = 14;
            f.enzymeTime = .25f;
            f.atEnzymeTime = .125f;
        };
        foreach (var f in efloat)
        {
            f.noiseScale = .5f;
            f.noiseStrength = 14;
        }
    }

        public void SetHigh()
    {
        frequency = 68.4f;
        breakDistance = 5.7f;
        separationSpeed = 4.14f; 
        foreach (var f in floats)
        {
            f.noiseScale = .75f;
            f.noiseStrength = 21;
            f.connect = false;
        }
    }
    // Update is called once per frame
    IEnumerator VibrateAndSplit()
    {
        Vector3 p1Start = left.localPosition;
        Vector3 p2Start = right.localPosition;
        while (true)
        {

            // Frequency increases over time
            float offset = Mathf.Sin(Time.time * frequency) * separationSpeed; // Side-to-side motion
           // Debug.Log(offset);
            // Increasing separation over time
            float separationFactor = breakDistance;

            left.localPosition = p1Start + Vector3.left * (separationFactor + offset);
            right.localPosition = p2Start + Vector3.right * (separationFactor + offset);

            yield return null;
        }


    }
}
