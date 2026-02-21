using UnityEngine;

public class tRNAAnimationRelay : MonoBehaviour
{
    /// <summary>
    /// Called from an Animation Event when this tRNA's Enter animation finishes.
    /// Finds the tRNASpawner that owns this tRNA (via hierarchy) and notifies it.
    /// </summary>
    public void OnEnterFinished()
    {
        // This relay is on the tRNA. tRNA is parented under spawnPoint, which is a child of spawnParent on the mRNA root.
        // So: this.transform.parent = spawn point, spawn point.parent = spawnParent (on the mRNA strand that's running).
        Transform spawnPoint = transform.parent;
        if (spawnPoint == null)
        {
            Debug.LogWarning("[tRNAAnimationRelay] tRNA has no parent (spawn point).");
            return;
        }

        Transform spawnParent = spawnPoint.parent;
        if (spawnParent == null)
        {
            Debug.LogWarning("[tRNAAnimationRelay] Spawn point has no parent.");
            return;
        }

        // mRNA root is the parent of spawnParent (sibling of "mRNA spawner" in the prefab)
        Transform mRNARoot = spawnParent.parent;
        if (mRNARoot == null)
        {
            Debug.LogWarning("[tRNAAnimationRelay] Could not find mRNA root.");
            return;
        }

        tRNASpawner spawner = mRNARoot.GetComponentInChildren<tRNASpawner>();
        if (spawner != null)
        {
            spawner.HandleEnterFinished(gameObject);
        }
        else
        {
            Debug.LogWarning("[tRNAAnimationRelay] tRNASpawner not found under mRNA root.");
        }
    }
}