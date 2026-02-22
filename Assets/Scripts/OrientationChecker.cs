using UnityEngine;

public class OrientationChecker : MonoBehaviour
{
    private bool lastIsLandscape;

    void Start()
    {
        UpdateOrientation();
    }

    void Update()
    {
        UpdateOrientation();
    }

    void UpdateOrientation()
    {
#if UNITY_EDITOR
        // Simulate orientation in Editor using resolution
        bool isLandscape = Screen.width > Screen.height;
#elif UNITY_IOS
        // Real device orientation on iOS
        DeviceOrientation orientation = Input.deviceOrientation;

        if (orientation == DeviceOrientation.Unknown ||
            orientation == DeviceOrientation.FaceUp ||
            orientation == DeviceOrientation.FaceDown)
            return;

        bool isLandscape =
            orientation == DeviceOrientation.LandscapeLeft ||
            orientation == DeviceOrientation.LandscapeRight;
#else
        bool isLandscape = Screen.width > Screen.height;
#endif

        if (isLandscape != lastIsLandscape)
        {
            lastIsLandscape = isLandscape;
            ApplyState(isLandscape);
        }
    }

    void ApplyState(bool isLandscape)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isLandscape);
        }
    }
}