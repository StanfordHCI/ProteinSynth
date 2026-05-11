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
    private char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

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
        string genotype = NormalizeGenotype(behaviour.TargetName);

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

    // Normalize all genotype variants to represent the same genotype
    // Needed multiple variants for image marker tracking purposes
    private string NormalizeGenotype(string targetName) {
        return targetName.TrimEnd(digits);
    }

}
