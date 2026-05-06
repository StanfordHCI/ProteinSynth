/*
    GenotypeTarget.cs file: this script is attached to every ImageTracker_<Genotype> GameObject. 
    - Uses Vuforia library to observe if card is tracked 
    - If target is tracked or untracked, then call on PunnettSquareManager / PunnettSquareTracker to register or unregister the genotype
*/

using UnityEngine;
using Vuforia;

public class GenotypeTarget : MonoBehaviour
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
        string genotype = behaviour.TargetName;

        // If we're tracking or extended tracked, register
        if (status.Status == Status.TRACKED)
        {
            PunnettSquareTracker.instance.RegisterGenotype(genotype, gameObject);
        }

        // If Vuforia says the target is lost or not observed anymore, unregister
        if (status.Status == Status.EXTENDED_TRACKED || status.Status == Status.NO_POSE || status.StatusInfo == StatusInfo.NOT_OBSERVED)
        {
            PunnettSquareTracker.instance.UnregisterGenotype(genotype, gameObject);
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
