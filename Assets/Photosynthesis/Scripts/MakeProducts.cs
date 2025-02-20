using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeProducts : MonoBehaviour
{
    Transform[] SugarcarbonPositions = new Transform[6];   // 6 Carbon target positions
     Transform[] SugaroxygenPositions = new Transform[6];   // 6 Oxygen target positions
     Transform[] SugarhydrogenPositions = new Transform[12]; // 12 Hydrogen target positions
    public Vector3[] OxyO2Positions; // 6 pos
    public float moveSpeed = 2f;          // Speed of movement

    private List<Transform> carbonAtoms = new List<Transform>();
    private List<Transform> oxygenAtoms = new List<Transform>();
    private List<Transform> hydrogenAtoms = new List<Transform>();

    private void Start()
    {
        GetPos();

    }

    public void Assemble()
    {
        GatherAtoms();
        StartCoroutine(AssembleGlucose());
    }

    void GetPos()
    {
        int h=0; int c=0; int o=0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            switch (child.name)
            {
                case string name when name.Contains("Carbon"):
                SugarcarbonPositions[c] = child;
                c++;
                break;

                case string name when name.Contains("Oxygen"):
                SugaroxygenPositions[o] = child;
                o++;
                break;

                case string name when name.Contains("Hydrogen"):
                SugarhydrogenPositions[h] = child;
                h++;
                break;
                
                default:
                break;
            }
        }
    }

    void GatherAtoms()
    {
        // Find all objects by tag and store only their Transforms
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Carbon"))
            carbonAtoms.Add(obj.transform);

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Oxygen"))
            oxygenAtoms.Add(obj.transform);

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Hydrogen"))
            hydrogenAtoms.Add(obj.transform);
    }

    IEnumerator AssembleGlucose()
    {
        if (carbonAtoms.Count < SugarcarbonPositions.Length ||
            oxygenAtoms.Count < SugaroxygenPositions.Length ||
            hydrogenAtoms.Count < SugarhydrogenPositions.Length)
        {
            Debug.LogError("Not enough Carbon, Oxygen, or Hydrogen atoms to form glucose.");
            yield break;
        }

        // Move Carbons
        for (int i = 0; i < SugarcarbonPositions.Length; i++)
        {
            StartCoroutine(MoveToPosition(carbonAtoms[i], SugarcarbonPositions[i].position));
        }

        // Move Oxygens
        for (int i = 0; i < SugaroxygenPositions.Length; i++)
        {
            StartCoroutine(MoveToPosition(oxygenAtoms[0], SugaroxygenPositions[i].position));
            oxygenAtoms.RemoveAt(0);
        }

        // Move Hydrogens
        for (int i = 0; i < SugarhydrogenPositions.Length; i++)
        {
            StartCoroutine(MoveToPosition(hydrogenAtoms[i], SugarhydrogenPositions[i].position));
        }

        StartCoroutine(FormOxygenMolecules());

        yield return null;
    }

    IEnumerator FormOxygenMolecules()
    {
        Debug.Log("hi");
        if (oxygenAtoms.Count < OxyO2Positions.Length * 2)
        {
            Debug.LogError("Not enough Oxygen atoms to form O₂ molecules.");
            yield break;
        }

        for (int i = 0; i < OxyO2Positions.Length; i++)
        {
            int index1 = i * 2;
            int index2 = index1 + 1;

            // Get two oxygen atoms per O₂ molecule
            Transform oxygen1 = oxygenAtoms[index1];
            Transform oxygen2 = oxygenAtoms[index2];

            // Calculate two positions near the center of the target O₂ position
            Vector3 centerPos = transform.TransformPoint(OxyO2Positions[i]);
            Vector3 offset = new Vector3(0.05f, 0, 0); // Adjusted to spread the atoms

            Vector3 targetPos1 = centerPos - offset;
            Vector3 targetPos2 = centerPos + offset;

            // Move both atoms to form the O₂ molecule
            StartCoroutine(MoveToPosition(oxygen1, targetPos1));
            StartCoroutine(MoveToPosition(oxygen2, targetPos2));
        }

        yield return null;
    }


    IEnumerator MoveToPosition(Transform atom, Vector3 targetPosition)
    {
        Vector3 startPosition = atom.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveSpeed)
        {
            elapsedTime += Time.deltaTime;
            atom.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveSpeed);
            yield return null;
        }

        atom.position = targetPosition; // Ensure final position
    }
}
