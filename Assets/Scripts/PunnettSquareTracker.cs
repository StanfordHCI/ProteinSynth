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

    [Header("Offspring Animation")]
    public GameObject offspringPrefab;          // 3d model for offspring
    public GameObject dustParticlePrefab;       // sparkle particles
    public GameObject offspringGlowPrefab;      // glow particles
    public float offspringSpawnHeight = 3f;    
    public float orbitDuration       = 2.5f;   // in sec
    public float descentDuration     = 1.8f;   // in sec
    public Camera mainCam;
    public float dimFadeDuration = 0.3f;


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
            { "A_a", "aa" }
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

    // Mostly used by the RatioBarUI.cs component to visualize genotypic ratios
    public string[] GetActiveGenotypes() {
        string[] flat = new string[currentDim * currentDim];
        int i = 0;
        for (int r = 0; r < currentDim; r++)
            for (int c = 0; c < currentDim; c++)
                flat[i++] = activeGenotypes[r, c] ?? "";
        return flat;
    }

    void UpdateTrackedGenesText() {
        string trackedGenes = "Tracked Genotypes:\n";
        foreach (var obj in tracked) {
            trackedGenes += obj.genotype+ "\n";
        }
        Debug.Log("current tracked genes are" + trackedGenes); 
        genesTrackedText.text = trackedGenes;
    }

    // Spawn offspring if Punnett square was completed successfully
    void SpawnOffspring()
    {
        StartCoroutine(SpawnOffspringRoutine());
    }

    IEnumerator SpawnOffspringRoutine()
    {
        // ── 0. Lock UI so the player can't re-trigger anything ───────────────────
        checkSquare.interactable = false;

        yield return StartCoroutine(FadeCameraBackground(0f, 1.0f, dimFadeDuration));


        // ── 1. Cache original parent transforms ──────────────────────────────────
        Vector3 p1Origin = parent1.transform.position;
        Vector3 p2Origin = parent2.transform.position;
        Quaternion p1OriginRot = parent1.transform.rotation;
        Quaternion p2OriginRot = parent2.transform.rotation;

        // Orbit centre = midpoint between the two parents
        Vector3 orbitCentre = (p1Origin + p2Origin) * 0.5f;
        float orbitRadius   = Vector3.Distance(p1Origin, orbitCentre);

        // ── 2. Spawn dust/sparkle particles at the orbit centre ──────────────────
        GameObject dust = null;
        if (dustParticlePrefab != null)
        {
            dust = Instantiate(dustParticlePrefab, orbitCentre, Quaternion.identity);
            // Make the particle cloud grow over the orbit duration
            var main = dust.GetComponent<ParticleSystem>().main;
            main.duration        = orbitDuration + 0.5f;
            main.startLifetime   = orbitDuration * 0.6f;
            dust.GetComponent<ParticleSystem>().Play();
        }

        // ── 3. Orbit the two parents around the midpoint ─────────────────────────
        float elapsed     = 0f;
        float totalAngle  = 360f * 2f;           // Two full revolutions
        float p1StartAngle = Mathf.Atan2(p1Origin.z - orbitCentre.z,
                                        p1Origin.x - orbitCentre.x) * Mathf.Rad2Deg;
        float p2StartAngle = p1StartAngle + 180f; // Always opposite

        while (elapsed < orbitDuration)
        {
            elapsed += Time.deltaTime;
            float t         = elapsed / orbitDuration;
            float easedT    = t < 0.5f                            // ease-in-out cubic
                                ? 4f * t * t * t
                                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            float angleDeg  = easedT * totalAngle;

            // Parent 1
            float a1 = (p1StartAngle + angleDeg) * Mathf.Deg2Rad;
            parent1.transform.position = orbitCentre + new Vector3(
                Mathf.Cos(a1) * orbitRadius,
                0f,
                Mathf.Sin(a1) * orbitRadius);

            // Parent 2 (always opposite)
            float a2 = (p2StartAngle + angleDeg) * Mathf.Deg2Rad;
            parent2.transform.position = orbitCentre + new Vector3(
                Mathf.Cos(a2) * orbitRadius,
                0f,
                Mathf.Sin(a2) * orbitRadius);

            // Spin each parent on their own Y axis for extra flair
            parent1.transform.Rotate(Vector3.up, 360f * Time.deltaTime / orbitDuration * 3f, Space.World);
            parent2.transform.Rotate(Vector3.up, 360f * Time.deltaTime / orbitDuration * 3f, Space.World);

            // Pulse scale: slight grow-and-shrink during orbit
            float scalePulse = 1f + 0.12f * Mathf.Sin(elapsed * Mathf.PI * 4f);
            parent1.transform.localScale = Vector3.one * scalePulse;
            parent2.transform.localScale = Vector3.one * scalePulse;

            yield return null;
        }

        // ── 4. Snap parents back to their original positions ─────────────────────
        float snapDuration = 0.35f;
        float snapElapsed  = 0f;

        Vector3 p1Snap = parent1.transform.position;
        Vector3 p2Snap = parent2.transform.position;

        while (snapElapsed < snapDuration)
        {
            snapElapsed += Time.deltaTime;
            float st = Mathf.SmoothStep(0f, 1f, snapElapsed / snapDuration);

            parent1.transform.position   = Vector3.Lerp(p1Snap, p1Origin, st);
            parent2.transform.position   = Vector3.Lerp(p2Snap, p2Origin, st);
            parent1.transform.rotation   = Quaternion.Slerp(parent1.transform.rotation, p1OriginRot, st);
            parent2.transform.rotation   = Quaternion.Slerp(parent2.transform.rotation, p2OriginRot, st);
            parent1.transform.localScale = Vector3.Lerp(parent1.transform.localScale, Vector3.one, st);
            parent2.transform.localScale = Vector3.Lerp(parent2.transform.localScale, Vector3.one, st);

            yield return null;
        }

        // Hard-reset to exact originals
        parent1.transform.SetPositionAndRotation(p1Origin, p1OriginRot);
        parent1.transform.localScale = Vector3.one;
        parent2.transform.SetPositionAndRotation(p2Origin, p2OriginRot);
        parent2.transform.localScale = Vector3.one;

        // Stop/destroy dust cloud now that parents have settled
        if (dust != null)
        {
            var ps = dust.GetComponent<ParticleSystem>();
            ps.Stop();
            Destroy(dust, ps.main.startLifetime.constantMax + 0.5f);
        }

        // ── 5. Spawn the offspring high above the board ───────────────────────────
        if (offspringPrefab == null)
        {
            Debug.LogWarning("SpawnOffspring: offspringPrefab is not assigned!");
            yield break;
        }

        Vector3 spawnPos  = orbitCentre + Vector3.up * offspringSpawnHeight;
        Vector3 landPos   = orbitCentre;                 // lands at the board centre

        GameObject offspring = Instantiate(offspringPrefab, spawnPos, Quaternion.identity);
        offspring.transform.localScale = Vector3.zero;   // starts invisible (scale-in)

        // Attach a glow/halo particle effect to the offspring
        GameObject glowFX = null;
        if (offspringGlowPrefab != null)
        {
            glowFX = Instantiate(offspringGlowPrefab, offspring.transform);
            glowFX.GetComponent<ParticleSystem>()?.Play();
        }

        // ── 6. Descend: scale in + float down + gentle rotation ──────────────────
        float descElapsed = 0f;

        while (descElapsed < descentDuration)
        {
            descElapsed += Time.deltaTime;
            float t = descElapsed / descentDuration;

            // Ease-out cubic for position (fast start, gentle landing)
            float posT = 1f - Mathf.Pow(1f - t, 3f);

            // Ease-out back for scale (slight overshoot for "pop" feel)
            float c1 = 1.70158f, c3 = c1 + 1f;
            float scaleT = t < 1f
                ? 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f)
                : 1f;
            scaleT = Mathf.Clamp(scaleT, 0f, 1.15f); // cap the overshoot

            offspring.transform.position   = Vector3.Lerp(spawnPos, landPos, posT);
            offspring.transform.localScale = Vector3.one * scaleT;

            // Slow spin as it descends (like drifting from above)
            offspring.transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);

            // Subtle brightness pulse via emission (requires Emission module enabled)
            // If you're using a material with _EmissionColor, uncomment and adapt:
            // float glow = 1f + 0.5f * Mathf.Sin(descElapsed * Mathf.PI * 3f) * (1f - t);
            // offspring.GetComponentInChildren<Renderer>()?.material
            //     .SetColor("_EmissionColor", Color.white * glow);

            yield return null;
        }

        // Hard-set final pose
        offspring.transform.position   = landPos;
        offspring.transform.localScale = Vector3.one;

        // Stop glow particles so they fade naturally (don't destroy immediately)
        if (glowFX != null)
        {
            var ps = glowFX.GetComponent<ParticleSystem>();
            ps.Stop();
        }
        yield return StartCoroutine(FadeCameraBackground(1.0f, 0f, dimFadeDuration));

        Debug.Log("SpawnOffspring: offspring has landed!");
    }

    IEnumerator FadeCameraBackground(float fromDark, float toDark, float duration)
    {
        float elapsed = 0f;
        Color original = mainCam.backgroundColor;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float dark = Mathf.Lerp(fromDark, toDark, t);
            mainCam.backgroundColor = original * (1f - dark);
            yield return null;
        }
    }
}
