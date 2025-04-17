using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class AminoControl : MonoBehaviour
{
    public List<NucleoControl> codons;
    // Start is called before the first frame update
    
    void MakeConnection(NucleoControl one, NucleoControl two)
    {
        two.connector.localScale = new Vector3(one.connector.localScale.x, one.connector.localScale.y, 0);
        one.connector.position = .5f*(one.head.position + two.head.position);
        one.connector.localScale =  new Vector3(one.connector.localScale.x, one.connector.localScale.y, 2*(one.head.position - two.head.position).magnitude);
        one.connector.LookAt(two.head);
    }

    // Update is called once per frame
    void Update()
    {
        codons = codons.OrderByDescending(n => n.head.position.x).ToList(); //ones with the lower x values are listed first
        
        
        for (int i = 0; i < codons.Count-1; i++)
        {
        //    if(codons[i].total && codons[i+1].total) //when both codons have found the image target
            {
                MakeConnection(codons[i], codons[i+1]);
            }
        }
    }
}
