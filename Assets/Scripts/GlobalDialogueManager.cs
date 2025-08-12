using Yarn.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Provides global static methods for a singleton DialogueRunner.
/// Can load scenes additively to keep the dialogue UI persistent.
public class GlobalDialogueManager : MonoBehaviour
{
    public static DialogueRunner runner;

    public static bool triggered = false;

    private void Awake()
    {
        runner = gameObject.GetComponent<DialogueRunner>();
        Debug.Log(runner);
    }

    // TODO: need to clean up the two different active scene management
    // code that's happening here
    private static string activeScene = null;

    [YarnCommand("LoadScene")]
    public static System.Collections.IEnumerator LoadScene(string scene)
    {
        if (SceneManager.GetActiveScene().name == scene)
        {
            Debug.Log("Scene " + scene + " already loaded!");
            yield break;
        }
        activeScene = scene;

        AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        // Wait for scene to finish loading
        while (!op.isDone)
        {
            yield return null;
        }
        // Wait a frame so every Awake and Start method is called
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("UnloadScene")]
    public static void UnloadScene()
    {
        if (activeScene != null)
        {
            SceneManager.UnloadSceneAsync(activeScene);
            activeScene = null;
        }
        else
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);
        }
        // runner.StartDialogue("Switch");
    }

    public static void StartDialogue(string startNode)
    {
        if (startNode == "ProteinSynthesisDNATutorial") {
            if (!triggered) {
                runner.StartDialogue(startNode);
            }
            triggered = true; 
        } else {
            runner.StartDialogue(startNode);
        }
    }

    public static void StopDialogue()
    {
        runner.Stop();
    }
}