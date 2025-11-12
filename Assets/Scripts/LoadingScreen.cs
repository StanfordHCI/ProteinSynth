using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class LoadingScreen : MonoBehaviour
{
    public GameObject MarsSession;
    public GameObject startButton; 

    void Start() {
        startButton.SetActive(false); 
        StartCoroutine(WaitToToggleCamera());
    }

    private void ToggleCamera() 
    {
        MarsSession.SetActive(false);
        MarsSession.SetActive(true);
    }

    public void StartLab() 
    {
        GlobalDialogueManager.StartDialogue("ProteinSynthesisLab");
        gameObject.SetActive(false);
    }

    private IEnumerator WaitToToggleCamera()
    {
        Debug.Log("In toggle camera coroutine"); 
        yield return new WaitForSeconds(3f);
        ToggleCamera();  
        yield return new WaitForSeconds(0.5f);
        ToggleCamera(); 
        ToggleCamera(); 
        ToggleCamera(); 
        ToggleCamera(); 
        ToggleCamera(); 
        ToggleCamera(); 
        yield return new WaitForSeconds(0.5f);
        ToggleCamera(); 
        MarsSession.SetActive(true);
        startButton.SetActive(true); 
    } 
}
