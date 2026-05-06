/*
    GeneralSpawner.cs file: for general use to spawn 3D objects on image target markers
    - Attached to any ImageTracker_<Genotype> GameObject, and will spawn the specified 3D object as a child of the marker when the marker is tracked
*/

using UnityEngine;
using Vuforia;

public class GeneralSpawner : MonoBehaviour
{
    public GameObject objectToAttach;

    ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();

        if (observer)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    void OnDestroy()
    {
        if (observer)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Marker found
        if (status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED)
        {
            AttachObject();
        }
    }

    void AttachObject()
    {
        if (objectToAttach == null)
            return;

        // Parent object to marker
        objectToAttach.transform.SetParent(transform);

        // Position relative to marker
        objectToAttach.transform.localPosition = Vector3.zero;
        
        // Face the user (camera)
        Transform cam = Camera.main.transform;
        objectToAttach.transform.LookAt(cam);

        // Rotation relative to marker
        objectToAttach.transform.localRotation = Quaternion.Euler(0, 90, 0);
    }
}