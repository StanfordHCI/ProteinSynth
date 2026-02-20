using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public bool shown; 
    public GameObject cameraButton; 
    public GameObject jumpToLab; 

    void Start() {
        shown = false; 
        if (cameraButton != null) {
            cameraButton.SetActive(false);
        }
        if (jumpToLab != null) {
            jumpToLab.SetActive(false);
        }
    }

    public void ToggleMenu() {
        Debug.Log("In toggling menu");
        shown = !shown; 
        if (cameraButton != null) {
            cameraButton.SetActive(shown);
        }
        if (jumpToLab != null) {
            jumpToLab.SetActive(shown);
        }
    }
}
