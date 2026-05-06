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
    [System.Serializable]
    public class TrackedGenotype
    {
        public string genotype;
        public GameObject obj;
    }

    private List<TrackedGenotype> tracked = new List<TrackedGenotype>();
    private string[,] activeGenotypes = new string[2, 2];
    private string[,] answerGenotypes= new string[2, 2];

    // 3D objects to show parents on the Punnett square
    public GameObject parent1;
    public GameObject parent2;

    [Header("UI Elements")]
    public TextMeshProUGUI traitText;
    public TextMeshProUGUI offspringRatio;
    public Button checkSquare; 
    public TextMeshProUGUI genesTrackedText;
    public TextMeshProUGUI correctText; 


    void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        checkSquare.interactable = false; 

        // TODO: Dynamically update these answers, for now hard-coded for example
        answerGenotypes = new string[,]
        {
            { "A_A_", "A_a" },
            { "GAG", "aa" }
        };
    }

    // Update is called once per frame
    void Update()
    {
        // Sort tracked genotypes into the right cells
        SortGenotypes(); 
        UpdateTrackedGenesText(); // for debugging, to see what genes are tracked

        // Verify all cells are filled to enable button to check Punnett square
        VerifyFilledCells();

        
    }

    void SortGenotypes() {
        List<TrackedGenotype> sorted = new List<TrackedGenotype>();
        foreach (var t in tracked)
        {
            sorted.Add(t);
        }

        float rowTolerance = 0.1f;

        // Define grid axes based on your physical layout
        Vector3 origin = parent1.transform.position;

        // parent1 = horizontal axis (left → right)
        Vector3 rightAxis = parent1.transform.right;

        // parent2 = vertical axis (bottom → top or top → bottom depending on setup)
        Vector3 upAxis = parent2.transform.up;

        // Convert world position → grid coordinates
        Vector2 GetGridPos(Vector3 worldPos)
        {
            Vector3 delta = worldPos - origin;

            float x = Vector3.Dot(delta, rightAxis);
            float y = Vector3.Dot(delta, upAxis);

            return new Vector2(x, y);
        }

        // Sort into grid order (top-to-bottom, left-to-right)
        sorted.Sort((a, b) =>
        {
            Vector2 aGrid = GetGridPos(a.obj.transform.position);
            Vector2 bGrid = GetGridPos(b.obj.transform.position);

            // Row comparison (Y axis)
            if (Mathf.Abs(aGrid.y - bGrid.y) > rowTolerance)
            {
                return bGrid.y.CompareTo(aGrid.y); // higher y = top row
            }

            // Column comparison (X axis)
            return aGrid.x.CompareTo(bGrid.x);
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

            TrackedGenotype obj = sorted[i];
            activeGenotypes[row, col] = obj.genotype;

            Debug.Log($"Placed {obj.genotype} at [{row},{col}]");
        }
    }

    public void RegisterGenotype(string genotype,GameObject obj) {
        // TODO: Animate genotype being locked into cell
        tracked.Add(new TrackedGenotype
        {
            genotype = genotype,
            obj = obj
        });
    }

    public void UnregisterGenotype(string genotype, GameObject obj) {
        // TODO: Animate genotype being removed from cell
        tracked.RemoveAll(t => t.obj == obj);
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
            CheckPunnettSquare(); // TODO: remove this, here for testing bc i can't get the button to click
        } else {
            checkSquare.interactable = false;
        }
    }

    // When student presses button, compare the current state of the square to the correct answer
    public void CheckPunnettSquare() {
        for (int i = 0; i < currentDim; i++) {
            for (int j = 0; j < currentDim; j++) {
                if (activeGenotypes[i, j] != answerGenotypes[i, j]) {
                    Debug.Log("wrong genotype at cell (" + i + ", " + j + "), currently says" + activeGenotypes[i, j] + " but should be " + answerGenotypes[i, j]);
                    correctText.text = "Almost there, try again!";
                    return;
                }
            }
        }

        Debug.Log("Punnett square is correct!");
        correctText.text = "Good job! Punnett square is correct!";
        SpawnOffspring(); 
    }

    // Spawn offspring if Punnett square was completed successfully
    void SpawnOffspring() {
        // Animate all phenotypes appearing above the genotypes
        // Animate spinner choosing between genotypes
        // Show selected child
    }

    void UpdateTrackedGenesText() {
        string trackedGenes = "Tracked Genotypes:\n";
        foreach (var obj in tracked) {
            trackedGenes += obj.genotype+ "\n";
        }
        Debug.Log("current tracked genes are" + trackedGenes); 
        genesTrackedText.text = trackedGenes;
    }
}
