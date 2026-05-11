/*
    CellTourManager.cs
    - Watches Vuforia image targets for Cell and Hand (assign the Image Target roots; Hand is optional for your own use — inside/outside uses the camera).
    - On first successful Cell track, spawns the cell prefab above the marker, then hides the Cell target object.
    - While the spawned cell exists, logs when the AR/camera moves inside vs outside the cell volume.
    - When inside the cell, tap / click (ray from AR camera) on an organelle child runs the Yarn node mapped in Organelle tap targets (needs Colliders on those objects).
*/

using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using Yarn.Unity;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class CellTourManager : MonoBehaviour
{
    [System.Serializable]
    public class OrganelleTapTarget
    {
        [Tooltip("Path under spawned cell root, e.g. Ribosome or Group/Mito (Transform.Find syntax).")]
        public string relativePathFromCellRoot;

        [Tooltip("Yarn Spinner start node (must exist in loaded project).")]
        public string yarnStartNode;
    }

    [Header("Image targets")]
    [Tooltip("Vuforia Image Target GameObject for the Cell marker.")]
    public GameObject cellImageTarget;

    [Tooltip("Optional second marker (unused by inside/outside logic; kept for Inspector / future hooks).")]
    public GameObject handImageTarget;

    [Header("Camera (inside / outside probe + taps)")]
    [Tooltip("AR / Vuforia camera; if unset, Camera.main is used.")]
    public GameObject arCamera;

    [Header("Cell model")]
    [Tooltip("Prefab instantiated above the Cell marker on first track.")]
    public GameObject cellPrefab;

    [Tooltip("Offset along the marker's local up (normal from the printed image).")]
    [SerializeField] private float spawnHeightAboveTarget = 0.05f;

    [Tooltip("If the cell prefab has no Colliders, inside/outside uses distance to the spawned root.")]
    [SerializeField] private float fallbackInsideRadius = 0.25f;

    [Tooltip("Max distance (m) from ClosestPoint to count as inside a collider (AR-scale float noise).")]
    [SerializeField] private float insideDistanceTolerance = 0.002f;

    [Header("Organelles → Yarn (children of spawned cell need Colliders on mesh or wrappers)")]
    [SerializeField] private OrganelleTapTarget[] organelleTapTargets;

    [Tooltip("Physics ray distance from camera for taps.")]
    [SerializeField] private float tapRayMaxDistance = 50f;

    private ObserverBehaviour cellObserver;
    private GameObject spawnedCell;
    private bool cellPlacementDone;
    private bool? lastLoggedInside;
    private bool cameraInsideCell;
    private readonly Dictionary<GameObject, string> organelleRootToYarnNode = new Dictionary<GameObject, string>();

    private static readonly OrganelleTapTarget[] DefaultOrganelles =
    {
        new OrganelleTapTarget { relativePathFromCellRoot = "Ribosome", yarnStartNode = "Ribosome" },
        new OrganelleTapTarget { relativePathFromCellRoot = "Nucleus", yarnStartNode = "Nucleus" },
        new OrganelleTapTarget { relativePathFromCellRoot = "Mitochondria", yarnStartNode = "Mitochondria" },
    };

    void Awake()
    {
        cellObserver = FindObserverOnTarget(cellImageTarget);
        if (organelleTapTargets == null || organelleTapTargets.Length == 0)
            organelleTapTargets = DefaultOrganelles;
    }

    /// <summary>Vuforia often lives on the Image Target root, but some prefabs nest it — check self, children, and parents.</summary>
    private static ObserverBehaviour FindObserverOnTarget(GameObject targetRoot)
    {
        if (targetRoot == null)
            return null;

        ObserverBehaviour o = targetRoot.GetComponent<ObserverBehaviour>();
        if (o != null)
            return o;

        o = targetRoot.GetComponentInChildren<ObserverBehaviour>(true);
        if (o != null)
            return o;

        return targetRoot.GetComponentInParent<ObserverBehaviour>();
    }

    void OnEnable()
    {
        if (cellObserver != null)
            cellObserver.OnTargetStatusChanged += OnCellTargetStatusChanged;
    }

    void OnDisable()
    {
        if (cellObserver != null)
            cellObserver.OnTargetStatusChanged -= OnCellTargetStatusChanged;
    }

    void Update()
    {
        bool tappedThisFrame = TryGetPointerDownScreenPosition(out Vector2 tapScreen);
        if (tappedThisFrame)
            Debug.Log($"[CellTour] Tap detected at screen ({tapScreen.x:F0}, {tapScreen.y:F0}).");

        if (spawnedCell == null)
            return;

        Transform camTransform = ResolveCameraTransform();
        if (camTransform != null)
        {
            cameraInsideCell = IsPointInsideCell(camTransform.position);

            if (!lastLoggedInside.HasValue || lastLoggedInside.Value != cameraInsideCell)
            {
                lastLoggedInside = cameraInsideCell;
                Debug.Log(cameraInsideCell ? "Inside the cell." : "Outside the cell.");
            }
        }

        TryProcessOrganelleTap(tappedThisFrame, tapScreen);
    }

    private Transform ResolveCameraTransform()
    {
        if (arCamera != null)
            return arCamera.transform;

        return Camera.main != null ? Camera.main.transform : null;
    }

    private Camera ResolveCameraForRay()
    {
        if (arCamera != null && arCamera.TryGetComponent(out Camera c))
            return c;

        return Camera.main;
    }

    private void OnCellTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (cellPlacementDone || cellPrefab == null)
            return;

        bool tracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
        if (!tracked)
            return;

        Transform t = behaviour.transform;
        Vector3 pos = t.position + t.up * spawnHeightAboveTarget;
        Quaternion rot = t.rotation;

        spawnedCell = Instantiate(cellPrefab, pos, rot);
        cellPlacementDone = true;

        RegisterOrganelleTapRoots();

        if (cellImageTarget != null)
            cellImageTarget.SetActive(false);
    }

    private void RegisterOrganelleTapRoots()
    {
        organelleRootToYarnNode.Clear();

        if (spawnedCell == null || organelleTapTargets == null)
            return;

        foreach (OrganelleTapTarget entry in organelleTapTargets)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.relativePathFromCellRoot) || string.IsNullOrWhiteSpace(entry.yarnStartNode))
                continue;

            Transform root = spawnedCell.transform.Find(entry.relativePathFromCellRoot);
            if (root == null)
            {
                Debug.LogWarning($"CellTourManager: no child '{entry.relativePathFromCellRoot}' under spawned cell; tap for '{entry.yarnStartNode}' will not work.");
                continue;
            }

            organelleRootToYarnNode[root.gameObject] = entry.yarnStartNode;
        }
    }

    private void TryProcessOrganelleTap(bool tappedThisFrame, Vector2 tapScreen)
    {
        if (!tappedThisFrame)
            return;

        if (!cameraInsideCell || organelleRootToYarnNode.Count == 0)
        {
            if (cameraInsideCell && organelleRootToYarnNode.Count == 0)
                Debug.Log("[CellTour] Tap ignored: no organelle tap targets registered (check spawned cell paths).");
            return;
        }

        Camera cam = ResolveCameraForRay();
        if (cam == null)
        {
            Debug.Log("[CellTour] Tap ignored: no camera for ray.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(tapScreen);
        if (!Physics.Raycast(ray, out RaycastHit hit, tapRayMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[CellTour] Tap ray hit nothing within {tapRayMaxDistance}m.");
            return;
        }

        if (!TryResolveOrganelleYarnNode(hit.collider.transform, out string yarnNode))
        {
            Debug.Log($"[CellTour] Tap hit '{hit.collider.name}' — not a mapped organelle (no Yarn node).");
            return;
        }

        StartYarnNodeReplacingCurrent(yarnNode);
    }

    private static bool TryGetPointerDownScreenPosition(out Vector2 screenPos)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        // Touches / mouse are independent paths: clicks work even while a finger rests on-screen.
        bool touchDown = TryGetLegacyFingerDownScreenPosition(out screenPos);
        if (touchDown)
            return true;

        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        screenPos = default;
        return false;
#elif ENABLE_INPUT_SYSTEM
        if (TryGetTouchscreenFingerDownScreenPosition(out screenPos))
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }

        screenPos = default;
        return false;
#else
        screenPos = default;
        return false;
#endif
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    private static bool TryGetLegacyFingerDownScreenPosition(out Vector2 screenPos)
    {
        screenPos = default;
        int n = Input.touchCount;
        if (n <= 0)
            return false;

        for (int i = 0; i < n; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began)
                continue;
            screenPos = t.position;
            return true;
        }

        return false;
    }
#endif

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private static bool TryGetTouchscreenFingerDownScreenPosition(out Vector2 screenPos)
    {
        screenPos = default;
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        for (int i = 0; i < touchscreen.touches.Count; ++i)
        {
            TouchControl finger = touchscreen.touches[i];
            if (finger == null)
                continue;
            if (!finger.press.wasPressedThisFrame)
                continue;
            screenPos = finger.position.ReadValue();
            return true;
        }

        if (touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }
#endif

    private bool TryResolveOrganelleYarnNode(Transform hitTransform, out string yarnNode)
    {
        yarnNode = null;
        if (spawnedCell == null || hitTransform == null)
            return false;

        Transform t = hitTransform;
        while (t != null)
        {
            if (t.gameObject == spawnedCell)
                break;

            if (organelleRootToYarnNode.TryGetValue(t.gameObject, out yarnNode))
                return true;

            t = t.parent;
        }

        return false;
    }

    private static void StartYarnNodeReplacingCurrent(string yarnNode)
    {
        if (string.IsNullOrEmpty(yarnNode))
            return;

        DialogueRunner runner = GlobalDialogueManager.runner;
        if (runner == null)
        {
            Debug.LogWarning("[CellTour] Try start Yarn node '" + yarnNode + "' failed: GlobalDialogueManager.runner is null.");
            return;
        }

        if (runner.IsDialogueRunning)
        {
            Debug.Log("[CellTour] Stopping current dialogue before starting node '" + yarnNode + "'.");
            GlobalDialogueManager.StopDialogue();
        }

        Debug.Log($"[CellTour] Starting Yarn node '{yarnNode}'.");
        GlobalDialogueManager.StartDialogue(yarnNode);
    }

    private bool IsPointInsideCell(Vector3 worldPoint)
    {
        if (spawnedCell == null)
            return false;

        Collider[] colliders = spawnedCell.GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
        {
            float r = fallbackInsideRadius;
            return (worldPoint - spawnedCell.transform.position).sqrMagnitude <= r * r;
        }

        float tol = Mathf.Max(insideDistanceTolerance, 1e-5f);
        float epsilonSqr = tol * tol;
        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled)
                continue;

            Vector3 closest = c.ClosestPoint(worldPoint);
            if ((closest - worldPoint).sqrMagnitude <= epsilonSqr)
                return true;
        }

        return false;
    }
}
