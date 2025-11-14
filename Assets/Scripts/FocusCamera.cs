using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia; 

public class FocusCamera : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                SetFocusRegion(touch.position, 0.5f);
            }
        }
    }

    void SetFocusRegion(Vector2 focusPosition, float extent)
    {
        var regionOfInterest = new CameraRegionOfInterest(focusPosition, extent);
        if (VuforiaBehaviour.Instance.CameraDevice.FocusRegionSupported == true)
        {
            var success = VuforiaBehaviour.Instance.CameraDevice.SetFocusRegion(regionOfInterest);
            if (success)
            {
                VuforiaBehaviour.Instance.CameraDevice.SetFocusMode(FocusMode.FOCUS_MODE_TRIGGERAUTO);
            }
            else
            {
                Debug.Log("Failed to set Focus Mode for region " + regionOfInterest.ToString());
            }
        }
        else
        {
            Debug.Log("Focus region supported: " + VuforiaBehaviour.Instance.CameraDevice.FocusRegionSupported);
            VuforiaBehaviour.Instance.CameraDevice.SetFocusRegion(CameraRegionOfInterest.Default());
        }
    }
}
