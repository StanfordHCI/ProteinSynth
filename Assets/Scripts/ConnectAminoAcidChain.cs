using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AminoAcidConnector : MonoBehaviour
{
    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
        line.widthMultiplier = 0.002f; // thickness of the line
    }

    void LateUpdate()
    {
        int count = transform.childCount;
        if (count == 0)
        {
            line.positionCount = 0;
            return;
        }

        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            line.SetPosition(i, transform.GetChild(i).position);
        }
    }
}
