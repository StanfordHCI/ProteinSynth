/*
    PunnettSquareTracker.cs file: this script is attached to the PunnettSquareManager GameObject. 
    - Handles tracking the current alleles tracked on screen and current state of Punnett Square
*/

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Compiler;
using Yarn.Unity;
using Vuforia;
using UnityEngine.XR.ARFoundation;

public class PunnettSquareTracker : MonoBehaviour
{
    // Singleton instance so other scripts can access this tracker globally
    public static PunnettSquareTracker instance;

    // Matrices to store current state of each cell in the student's Punnett square, i.e. which genotype cards they've placed
    private int currentDim = 2 ; // dimension of current Punnett square

    // Store all tracked genotypes
    private Dictionary<string, GameObject> tracked = new Dictionary<string, GameObject>();
    private string[,] activeGenotypes = new string[2, 2];
    private string[,] answerGenotypes= new string[2, 2];

    // 3D objects to show parents on the Punnett square
    public GameObject parent1;
    public GameObject parent2;

    [Header("UI Elements")]
    public TextMeshProUGUI traitText;
    public TextMeshProUGUI offspringRatio;
    public TextMeshProUGUI hintText;
    public Button checkSquare; 



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Sort tracked genotypes into the right cells
        SortGenotypes(); 

        // Verify all cells are filled to enable button to check Punnett square
        VerifyFilledCells();
        
    }

    void SortGenotypes() {
        List<GameObject> sorted = new List<GameObject>(tracked.Values);

        float rowTolerance = 0.05f;

        // Sort top-to-bottom, then left-to-right
        sorted.Sort((a, b) =>
        {
            Vector3 aLocal = parent1.transform.InverseTransformPoint(a.transform.position);
            Vector3 bLocal = parent1.transform.InverseTransformPoint(b.transform.position);

            // Different rows
            if (Mathf.Abs(aLocal.y - bLocal.y) > rowTolerance)
            {
                return bLocal.y.CompareTo(aLocal.y);
            }

            // Same row
            return aLocal.x.CompareTo(bLocal.x);
        });

        // Clear matrix first
        for (int r = 0; r < currentDim; r++)
        {
            for (int c = 0; c < currentDim; c++)
            {
                activeGenotypes[r, c] = "";
            }
        }

        // Fill matrix
        for (int i = 0; i < sorted.Count; i++)
        {
            int row = i / currentDim;
            int col = i % currentDim;

            if (row >= currentDim)
                break;

            GameObject obj = sorted[i];

            // Assuming genotype string is the GameObject name
            activeGenotypes[row, col] = obj.name;

            Debug.Log($"Placed {obj.name} at [{row},{col}]");
        }
    }

    public void RegisterGenotype(string genotype,GameObject obj) {
        // Animate genotype being locked into cell
        if (!tracked.ContainsKey(genotype))
        {
            tracked[genotype] = obj;
        }
    }

    public void UnregisterGenotype(string genotype) {
        // Animate genotype being removed from cell
        if (tracked.ContainsKey(genotype))
        {
            tracked.Remove(genotype);
        }
    }

    void VerifyFilledCells() {
        bool filled = true; 
        for (int i = 0; i < currentDim; i++) {
            for (int j = 0; j < currentDim; j++) {
                if (string.IsNullOrEmpty(activeGenotypes[i, j])) {
                    filled = false;  
                    break;  
                }
            }
        }
        if (filled) {
            checkSquare.interactable = true;
        } else {
            checkSquare.interactable = false;
        }
    }

    // When student presses button, compare the current state of the square to the correct answer
    public void CheckPunnettSquare() {
        for (int i = 0; i < currentDim; i++) {
            for (int j = 0; j < currentDim; j++) {
                if (activeGenotypes[i, j] != answerGenotypes[i, j]) {
                    Debug.Log("wrong genotype at cell (" + i + ", " + j + ")");
                    hintText.text = "Almost there, try again!";
                    return;
                }
            }
        }

        Debug.Log("Punnett square is correct!");
        SpawnOffspring(); 
    }

    // Spawn offspring if Punnett square was completed successfully
    void SpawnOffspring() {
        // Animate all phenotypes appearing above the genotypes
        // Animate spinner choosing between genotypes
        // Show selected child
    }
}
