/*
    CodonTarget.cs file: this script is attached to every ImageTracker_<CodonName> GameObject. 
    - Uses Vuforia library to observe if card is tracked 
    - If target is tracked or untracked, then cal on CodonManager / CodonTracker to register or unregister the codon.
*/

using UnityEngine;
using Vuforia;

public class CodonTarget : MonoBehaviour
{
    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        string codonName = behaviour.TargetName;

        // If we're tracking or extended tracked, register
        if (status.Status == Status.TRACKED)
        {
            CodonTracker.instance.RegisterCodon(codonName, gameObject);
        }

        // If Vuforia says the target is lost or not observed anymore, unregister
        if (status.Status == Status.EXTENDED_TRACKED || status.Status == Status.NO_POSE || status.StatusInfo == StatusInfo.NOT_OBSERVED)
        {
            CodonTracker.instance.UnregisterCodon(codonName);
        }
    }

    void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }
}
