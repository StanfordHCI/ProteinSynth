using UnityEngine;
using Yarn.Unity;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(DialogueRunner))]
public class GlobalInMemoryVariableStorage : InMemoryVariableStorage
{
    public static GlobalInMemoryVariableStorage Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            throw new System.InvalidOperationException("Only one dialogue variable store may be active.");
        Instance = this;
        GetComponent<DialogueRunner>().VariableStorage = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
