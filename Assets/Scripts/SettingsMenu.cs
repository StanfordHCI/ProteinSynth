using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public bool shown; 
    public GameObject cameraButton; 

    void Start() {
        shown = false; 
        cameraButton.SetActive(false);
    }

    public void ToggleMenu() {
        Debug.Log("In toggling menu");
        shown = !shown; 
        cameraButton.SetActive(shown);
    }
}
