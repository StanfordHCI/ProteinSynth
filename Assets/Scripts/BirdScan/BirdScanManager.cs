/*
    BirdScanManager.cs: step 1 of the Punnett square lab — find birds around you and sample their genotype
    - start_bird_scan spawns birds that fly in around the student (blue bird for BB/Bb, gold bird for bb)
    - Birds land on real floors/tables found with Vuforia Ground Plane hit tests, or a guessed floor until some are found
    - A bird counts as detected when any part of it overlaps the on-screen reticle and it's within range; that enables Take sample
    - Taking a sample flashes the screen, reveals the bird's illustration + genotype, then files it into the next slot
    - wait_for_bird_samples blocks the Yarn script until samplesRequired samples are collected
    - Yarn usage (GameObject must be named BirdScanManager):
        <<start_bird_scan BirdScanManager>>
        <<wait_for_bird_samples BirdScanManager>>
        <<end_bird_scan BirdScanManager>>
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Yarn.Unity;
using Image = UnityEngine.UI.Image;

public class BirdScanManager : MonoBehaviour
{
    [Header("Birds")]
    [Tooltip("Living Birds prefab for the blue phenotype (BB, Bb). Defaults to lb_blueJayHQ.")]
    public GameObject blueBirdPrefab;
    [Tooltip("Living Birds prefab for the gold phenotype (bb). Defaults to lb_goldFinchHQ.")]
    public GameObject goldBirdPrefab;
    [Tooltip("One bird is spawned per entry.")]
    public string[] birdGenotypes = { Genotype.BB, Genotype.Bb, Genotype.bb, Genotype.Bb, Genotype.BB, Genotype.bb };
    public float birdScale = 1f;
    public int samplesRequired = 6;

    [Header("Surfaces (Vuforia Ground Plane hit tests)")]
    [Tooltip("Found or added at runtime if left empty.")]
    public PlaneFinderBehaviour planeFinder;
    [Tooltip("Random screen points tested against real surfaces each second while scanning.")]
    public float hitTestsPerSecond = 6f;
    [Tooltip("Landing spots closer together than this (m) are merged.")]
    public float minSpotSpacing = 0.3f;
    public int maxSpots = 40;
    [Tooltip("Only surfaces at least this far (m) below the camera count, i.e. floors and tables.")]
    public float minDropBelowCamera = 0.2f;
    [Tooltip("Horizontal distance (m) from the student that birds will land, so they never land underfoot.")]
    public Vector2 landingDistanceRange = new Vector2(0.7f, 3f);
    [Tooltip("Until real surfaces are found, birds land on a guessed floor this far (m) below the camera.")]
    public bool useEstimatedFloor = true;
    public float estimatedFloorBelowCamera = 1.2f;

    [Header("Flying in (relative to the camera)")]
    public Vector2 airDistanceRange = new Vector2(1.2f, 2.5f);
    public Vector2 airHeightRange = new Vector2(0.1f, 0.6f);

    [Header("Detection")]
    public Camera arCamera;
    [Tooltip("Birds whose center is inside this rect on screen are detected.")]
    public RectTransform reticle;
    public float maxScanDistance = 3.5f;
    public bool canScanFlyingBirds = true;
    [Tooltip("Keep a bird detected this long after it leaves the reticle so the button doesn't flicker.")]
    public float detectionGraceSeconds = 0.25f;

    [Header("UI")]
    public GameObject scanUIRoot;
    [Tooltip("Other UI (e.g. the Punnett square Canvas) hidden while scanning and shown again by end_bird_scan.")]
    public GameObject[] hideWhileScanning = new GameObject[0];
    public TextMeshProUGUI headerText;
    public BirdSampleSlot[] slots;
    public GameObject detectedBadge;
    public Button takeSampleButton;
    public TextMeshProUGUI takeSampleLabel;
    public Image flashOverlay;
    public RectTransform revealCard;
    public Image revealPortrait;
    public TextMeshProUGUI revealGenotypeText;
    [Tooltip("Seconds the reveal card stays up before filing into its slot (tap the card to skip).")]
    public float revealSeconds = 2f;

    [Header("Illustrations")]
    public Sprite blueBirdIllustration;
    public Sprite goldBirdIllustration;
    public Color blueCardColor = new Color(0.79f, 0.95f, 1f);
    public Color goldCardColor = new Color(1f, 0.95f, 0.76f);

    [Header("Colors")]
    public Color reticleIdleColor = Color.white;
    public Color reticleDetectedColor = new Color(0.36f, 0.89f, 0.29f);
    public Color buttonIdleColor = new Color(1f, 1f, 1f, 0.2f);
    public Color buttonActiveColor = new Color(0.4f, 0.39f, 0.96f);
    public Color buttonLabelIdleColor = new Color(1f, 1f, 1f, 0.85f);
    public Color buttonLabelActiveColor = Color.white;

    [Header("Testing")]
    [Tooltip("Start scanning on Start instead of waiting for the Yarn command.")]
    public bool startOnStart = false;
    [Tooltip("In the Editor the webcam can't move or find surfaces, so keep birds flying and landing in front of the camera.")]
    public bool keepBirdsInViewInEditor = true;

    public Vector3 ViewerPosition => CameraTransform != null ? CameraTransform.position : Vector3.zero;
    public bool IsComplete => BirdSamples.Genotypes.Count >= samplesRequired;

    private Transform CameraTransform => arCamera != null ? arCamera.transform : (Camera.main != null ? Camera.main.transform : null);

    private readonly List<ScannableBird> birds = new List<ScannableBird>();
    private readonly List<GameObject> hiddenByScan = new List<GameObject>();
    private readonly List<Vector3> surfaceSpots = new List<Vector3>();
    private readonly Dictionary<ScannableBird, Vector3> claimedSpots = new Dictionary<ScannableBird, Vector3>();
    private float nextHitTestTime;
    private Graphic[] reticleGraphics;
    private readonly Vector3[] boundsCorners = new Vector3[8];

    private bool scanning;
    private bool revealing;
    private bool skipReveal;
    private bool? shownDetected;
    private ScannableBird detectedBird;
    private float lastDetectedTime;

    private Vector2 revealCardHomePosition;
    private Vector3 revealCardHomeScale;

    void Awake()
    {
        if (blueBirdPrefab == null)
            blueBirdPrefab = Resources.Load<GameObject>("lb_blueJayHQ");
        if (goldBirdPrefab == null)
            goldBirdPrefab = Resources.Load<GameObject>("lb_goldFinchHQ");

        if (reticle != null)
            reticleGraphics = reticle.GetComponentsInChildren<Graphic>(true);
        // Buttons only receive taps through a graphic with raycastTarget on
        if (takeSampleButton != null)
        {
            takeSampleButton.onClick.AddListener(TakeSample);
            if (takeSampleButton.image != null)
                takeSampleButton.image.raycastTarget = true;
        }
        if (revealCard != null)
        {
            revealCardHomePosition = revealCard.anchoredPosition;
            revealCardHomeScale = revealCard.localScale;
            if (revealCard.TryGetComponent(out Button cardButton))
                cardButton.onClick.AddListener(() => skipReveal = true);
            if (revealCard.TryGetComponent(out Image cardImage))
                cardImage.raycastTarget = true;
            revealCard.gameObject.SetActive(false);
        }
        if (flashOverlay != null)
            SetAlpha(flashOverlay, 0f);
        if (scanUIRoot != null)
            scanUIRoot.SetActive(false);

        // A PlaneFinderBehaviour added from code isn't fully set up (null events, errors when Vuforia starts),
        // so it has to come from GameObject > Vuforia Engine > Ground Plane > Plane Finder
        if (planeFinder == null)
            planeFinder = FindObjectOfType<PlaneFinderBehaviour>();
        if (planeFinder != null && planeFinder.OnInteractiveHitTest != null)
        {
            planeFinder.HitTestMode = HitTestMode.INTERACTIVE;
            planeFinder.OnInteractiveHitTest.AddListener(OnSurfaceHit);
        }
        else
        {
            planeFinder = null;
            Debug.LogWarning("BirdScanManager: no Vuforia Plane Finder in the scene, so birds will land on a guessed floor. Add one with GameObject > Vuforia Engine > Ground Plane > Plane Finder.");
        }
    }

    void OnDestroy()
    {
        if (planeFinder != null && planeFinder.OnInteractiveHitTest != null)
            planeFinder.OnInteractiveHitTest.RemoveListener(OnSurfaceHit);
    }

    void Start()
    {
        if (startOnStart)
            StartBirdScan();
    }

    [YarnCommand("start_bird_scan")]
    public void StartBirdScan()
    {
        ClearBirds();
        BirdSamples.Genotypes.Clear();
        foreach (BirdSampleSlot slot in slots)
            if (slot != null) slot.Clear();
        if (headerText != null)
            headerText.text = $"Collect {samplesRequired} samples";

        surfaceSpots.Clear();
        EnsureAudioListener();
        SpawnBirds();

        detectedBird = null;
        shownDetected = null;
        revealing = false;
        scanning = true;
        if (scanUIRoot != null)
            scanUIRoot.SetActive(true);
        SetScanControlsVisible(true);
        HideOtherUI();
    }

    [YarnCommand("wait_for_bird_samples")]
    public IEnumerator WaitForBirdSamples()
    {
        while (!IsComplete || revealing)
            yield return null;
    }

    [YarnCommand("end_bird_scan")]
    public void EndBirdScan()
    {
        scanning = false;
        StopAllCoroutines();
        revealing = false;
        if (scanUIRoot != null)
            scanUIRoot.SetActive(false);
        ClearBirds();
        RestoreOtherUI();
    }

    // Hide other UI (e.g. the Punnett square canvas) while scanning; only what was visible gets restored
    private void HideOtherUI()
    {
        foreach (GameObject ui in hideWhileScanning)
        {
            if (ui != null && ui.activeSelf)
            {
                ui.SetActive(false);
                hiddenByScan.Add(ui);
            }
        }
    }

    private void RestoreOtherUI()
    {
        foreach (GameObject ui in hiddenByScan)
            if (ui != null) ui.SetActive(true);
        hiddenByScan.Clear();
    }

    void Update()
    {
        if (!scanning)
            return;

        SampleSurfaces();

        if (revealing || IsComplete)
            return;

        ScannableBird inFrame = FindBirdInReticle();
        if (inFrame != null)
        {
            detectedBird = inFrame;
            lastDetectedTime = Time.time;
        }
        else if (detectedBird != null && (detectedBird.Sampled || Time.time - lastDetectedTime > detectionGraceSeconds))
        {
            detectedBird = null;
        }

        ShowDetected(detectedBird != null);
    }

    public void TakeSample()
    {
        if (!scanning || revealing || detectedBird == null || detectedBird.Sampled || IsComplete)
            return;
        StartCoroutine(SampleRoutine(detectedBird));
    }

    // ── Surfaces ───────────────────────────────────────────────────────────

    // Hit-test random screen points against Vuforia's detected ground planes; results arrive in OnSurfaceHit.
    // Spots build up as the student looks around, so birds spread to wherever they've pointed the camera.
    private void SampleSurfaces()
    {
        if (planeFinder == null || hitTestsPerSecond <= 0f || Time.time < nextHitTestTime)
            return;
        nextHitTestTime = Time.time + 1f / hitTestsPerSecond;

        Vector2 screenPoint = new Vector2(Random.Range(0.1f, 0.9f) * Screen.width, Random.Range(0.05f, 0.8f) * Screen.height);
        planeFinder.PerformHitTest(screenPoint);
    }

    private void OnSurfaceHit(HitTestResult hit)
    {
        if (hit == null || !scanning)
            return;

        Vector3 viewer = ViewerPosition;
        Vector3 spot = hit.Position;
        if (spot.y > viewer.y - minDropBelowCamera)
            return;
        foreach (Vector3 existing in surfaceSpots)
            if ((existing - spot).sqrMagnitude < minSpotSpacing * minSpotSpacing)
                return;

        surfaceSpots.Add(spot);
        if (surfaceSpots.Count > maxSpots)
        {
            // Drop the spot farthest from where the student is now
            int farthest = 0;
            for (int i = 1; i < surfaceSpots.Count; i++)
                if ((surfaceSpots[i] - viewer).sqrMagnitude > (surfaceSpots[farthest] - viewer).sqrMagnitude)
                    farthest = i;
            surfaceSpots.RemoveAt(farthest);
        }
    }

    // Pick a free landing spot near the student for this bird (releasing its old one).
    // Uses detected surfaces when there are any in range, otherwise a guessed floor.
    public bool TryClaimLandingSpot(ScannableBird bird, out Vector3 spot)
    {
        claimedSpots.Remove(bird);
        Vector3 viewer = ViewerPosition;

        List<Vector3> candidates = new List<Vector3>();
        foreach (Vector3 s in surfaceSpots)
            if (InLandingRange(s, viewer) && !NearClaimedSpot(s))
                candidates.Add(s);

        if (EditorInViewMode)
        {
            spot = viewer;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                spot = InViewPoint(new Vector2(0.25f, 0.6f), new Vector2(1f, 2f));
                if (!NearClaimedSpot(spot))
                    break;
            }
        }
        else if (candidates.Count > 0)
        {
            spot = candidates[Random.Range(0, candidates.Count)];
        }
        else if (useEstimatedFloor)
        {
            // The lowest real surface found so far is the best guess at the floor
            float floorY = viewer.y - estimatedFloorBelowCamera;
            if (surfaceSpots.Count > 0)
            {
                floorY = surfaceSpots[0].y;
                foreach (Vector3 s in surfaceSpots)
                    floorY = Mathf.Min(floorY, s.y);
            }

            spot = viewer;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                spot = viewer + RandomHorizontal() * Random.Range(landingDistanceRange.x, landingDistanceRange.y);
                spot.y = floorY;
                if (!NearClaimedSpot(spot))
                    break;
            }
        }
        else
        {
            spot = default;
            return false;
        }

        claimedSpots[bird] = spot;
        return true;
    }

    public Vector3 AirWaypoint()
    {
        if (EditorInViewMode)
            return InViewPoint(new Vector2(0.55f, 0.9f), airDistanceRange);
        return ViewerPosition
            + RandomHorizontal() * Random.Range(airDistanceRange.x, airDistanceRange.y)
            + Vector3.up * Random.Range(airHeightRange.x, airHeightRange.y);
    }

    private bool InLandingRange(Vector3 spot, Vector3 viewer)
    {
        Vector2 flat = new Vector2(spot.x - viewer.x, spot.z - viewer.z);
        float d = flat.magnitude;
        return d >= landingDistanceRange.x && d <= landingDistanceRange.y;
    }

    private bool NearClaimedSpot(Vector3 spot)
    {
        foreach (Vector3 claimed in claimedSpots.Values)
            if ((claimed - spot).sqrMagnitude < minSpotSpacing * minSpotSpacing)
                return true;
        return false;
    }

    private bool EditorInViewMode => Application.isEditor && keepBirdsInViewInEditor && CameraTransform != null;

    // A point a random distance along a ray through a random spot on screen (viewport y within yRange)
    private Vector3 InViewPoint(Vector2 yRange, Vector2 distanceRange)
    {
        Camera cam = arCamera != null ? arCamera : Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(Random.Range(0.15f, 0.85f), Random.Range(yRange.x, yRange.y), 0f));
        return ray.GetPoint(Random.Range(distanceRange.x, distanceRange.y));
    }

    private static Vector3 RandomHorizontal()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
    }

    // ── Birds ──────────────────────────────────────────────────────────────

    // Birds fly in from points in the air spread around the student; bird 0 comes in from straight ahead
    private void SpawnBirds()
    {
        Transform cam = CameraTransform;
        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up) : Vector3.forward;
        if (forward.sqrMagnitude < 1e-4f)
            forward = Vector3.forward;
        float baseYaw = Quaternion.LookRotation(forward.normalized).eulerAngles.y;
        float sector = 360f / Mathf.Max(1, birdGenotypes.Length);

        for (int i = 0; i < birdGenotypes.Length; i++)
        {
            float yaw = baseYaw + i * sector + (i == 0 ? 0f : Random.Range(-0.3f, 0.3f) * sector);
            Vector3 start = ViewerPosition
                + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * Random.Range(airDistanceRange.x, airDistanceRange.y)
                + Vector3.up * Random.Range(airHeightRange.x, airHeightRange.y);

            if (EditorInViewMode)
                start = InViewPoint(new Vector2(0.55f, 0.9f), airDistanceRange);

            ScannableBird bird = SpawnBird(birdGenotypes[i], start);
            if (bird != null)
                birds.Add(bird);
        }
    }

    private ScannableBird SpawnBird(string genotype, Vector3 start)
    {
        GameObject prefab = Genotype.IsRecessivePhenotype(genotype) ? goldBirdPrefab : blueBirdPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"BirdScanManager: no bird prefab for genotype {genotype}");
            return null;
        }

        GameObject go = Instantiate(prefab, start, Quaternion.identity, transform);
        go.name = $"Bird_{genotype}_{birds.Count}";
        go.transform.localScale = prefab.transform.localScale * birdScale;

        ScannableBird bird = go.AddComponent<ScannableBird>();

        // lb_Bird needs lb_BirdController; take its audio clips and replace it. Looked up by name so this
        // still compiles for teammates who haven't imported the (Asset Store, not committed) package.
        Component living = go.GetComponent("lb_Bird");
        if (living != null)
        {
            bird.song1 = ClipField(living, "song1");
            bird.song2 = ClipField(living, "song2");
            bird.flyAway1 = ClipField(living, "flyAway1");
            bird.flyAway2 = ClipField(living, "flyAway2");
            DestroyImmediate(living);
        }

        // Movement is kinematic; colliders aren't needed for on-screen detection
        if (go.TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        foreach (Collider col in go.GetComponentsInChildren<Collider>())
            col.enabled = false;

        bird.Init(this, genotype, start);
        return bird;
    }

    // ARCamera's listener is disabled in this scene (the main dialogue scene normally provides one when this
    // scene is loaded additively). When testing the scene on its own, turn the camera's listener on for bird song.
    private void EnsureAudioListener()
    {
        foreach (AudioListener listener in FindObjectsOfType<AudioListener>())
            if (listener.isActiveAndEnabled)
                return;

        Transform cam = CameraTransform;
        if (cam == null)
            return;
        AudioListener own = cam.GetComponent<AudioListener>();
        if (own == null)
            own = cam.gameObject.AddComponent<AudioListener>();
        own.enabled = true;
    }

    private static AudioClip ClipField(Component component, string field)
    {
        return component.GetType().GetField(field)?.GetValue(component) as AudioClip;
    }

    private void ClearBirds()
    {
        foreach (ScannableBird bird in birds)
            if (bird != null) Destroy(bird.gameObject);
        birds.Clear();
        claimedSpots.Clear();
    }

    // ── Detection ──────────────────────────────────────────────────────────

    private ScannableBird FindBirdInReticle()
    {
        Camera cam = arCamera != null ? arCamera : Camera.main;
        if (cam == null || reticle == null)
            return null;

        Rect frame = ScreenRect(reticle);
        ScannableBird best = null;
        float bestSqr = float.MaxValue;
        foreach (ScannableBird bird in birds)
        {
            if (bird == null || bird.Sampled || (bird.IsFlying && !canScanFlyingBirds))
                continue;

            // Any part of the bird overlapping the reticle counts, not just its center
            if (!TryScreenRect(cam, bird.ScanBounds, out Rect birdRect, out float depth))
                continue;
            if (depth > maxScanDistance || !frame.Overlaps(birdRect))
                continue;

            float sqr = (birdRect.center - frame.center).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = bird;
            }
        }
        return best;
    }

    // Screen-space rect around a world bounds; false if any of it is behind the camera
    private bool TryScreenRect(Camera cam, Bounds bounds, out Rect rect, out float depth)
    {
        Vector3 c = bounds.center, e = bounds.extents;
        for (int i = 0; i < 8; i++)
            boundsCorners[i] = c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in boundsCorners)
        {
            Vector3 s = cam.WorldToScreenPoint(corner);
            if (s.z <= 0f)
            {
                rect = default;
                depth = 0f;
                return false;
            }
            min = Vector2.Min(min, s);
            max = Vector2.Max(max, s);
        }
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        depth = cam.WorldToScreenPoint(c).z;
        return true;
    }

    private static Rect ScreenRect(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera uiCam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void ShowDetected(bool detected)
    {
        if (shownDetected == detected)
            return;
        shownDetected = detected;

        if (reticleGraphics != null)
            foreach (Graphic g in reticleGraphics)
                g.color = detected ? reticleDetectedColor : reticleIdleColor;
        if (detectedBadge != null)
            detectedBadge.SetActive(detected);
        if (takeSampleButton != null)
        {
            takeSampleButton.interactable = detected;
            if (takeSampleButton.image != null)
                takeSampleButton.image.color = detected ? buttonActiveColor : buttonIdleColor;
        }
        if (takeSampleLabel != null)
            takeSampleLabel.color = detected ? buttonLabelActiveColor : buttonLabelIdleColor;
    }

    private void SetScanControlsVisible(bool visible)
    {
        if (reticle != null)
            reticle.gameObject.SetActive(visible);
        if (takeSampleButton != null)
            takeSampleButton.gameObject.SetActive(visible);
        if (detectedBadge != null && !visible)
            detectedBadge.SetActive(false);
        shownDetected = null;
    }

    // ── Sampling ───────────────────────────────────────────────────────────

    private IEnumerator SampleRoutine(ScannableBird bird)
    {
        revealing = true;
        skipReveal = false;
        detectedBird = null;
        SetScanControlsVisible(false);

        bird.MarkSampled();
        BirdSamples.Genotypes.Add(bird.genotype);
        int slotIndex = BirdSamples.Genotypes.Count - 1;

        bool gold = Genotype.IsRecessivePhenotype(bird.genotype);
        Sprite illustration = gold ? goldBirdIllustration : blueBirdIllustration;
        Color cardColor = gold ? goldCardColor : blueCardColor;

        // Camera flash
        if (flashOverlay != null)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
            {
                SetAlpha(flashOverlay, Mathf.Lerp(0.85f, 0f, t));
                yield return null;
            }
            SetAlpha(flashOverlay, 0f);
        }

        if (revealCard != null)
        {
            if (revealCard.TryGetComponent(out Image cardImage))
                cardImage.color = cardColor;
            if (revealPortrait != null)
            {
                revealPortrait.sprite = illustration;
                revealPortrait.gameObject.SetActive(illustration != null);
            }
            if (revealGenotypeText != null)
                revealGenotypeText.text = bird.genotype;

            revealCard.anchoredPosition = revealCardHomePosition;
            revealCard.gameObject.SetActive(true);

            // Pop in
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                revealCard.localScale = revealCardHomeScale * EaseOutBack(t);
                yield return null;
            }
            revealCard.localScale = revealCardHomeScale;

            for (float t = 0f; t < revealSeconds && !skipReveal; t += Time.deltaTime)
                yield return null;

            // Shrink into the slot
            BirdSampleSlot slot = slotIndex < slots.Length ? slots[slotIndex] : null;
            if (slot != null)
            {
                RectTransform slotRect = (RectTransform)slot.transform;
                Vector3 fromPos = revealCard.position;
                float targetScale = slotRect.rect.width * slotRect.lossyScale.x / (revealCard.rect.width * revealCard.lossyScale.x);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
                {
                    float e = Mathf.SmoothStep(0f, 1f, t);
                    revealCard.position = Vector3.Lerp(fromPos, slotRect.position, e);
                    revealCard.localScale = revealCardHomeScale * Mathf.Lerp(1f, targetScale, e);
                    yield return null;
                }
            }

            revealCard.gameObject.SetActive(false);
            revealCard.anchoredPosition = revealCardHomePosition;
            revealCard.localScale = revealCardHomeScale;
        }

        if (slotIndex < slots.Length && slots[slotIndex] != null)
            slots[slotIndex].Fill(illustration, bird.genotype, cardColor);

        if (!IsComplete)
            SetScanControlsVisible(true);
        else if (headerText != null)
            headerText.text = "All samples collected!";
        revealing = false;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }
}
