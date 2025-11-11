using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    public GameObject MarsSession;

    public void ToggleCamera() 
    {
        MarsSession.SetActive(false);
        MarsSession.SetActive(true);
    }
}
