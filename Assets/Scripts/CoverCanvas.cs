using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class CoverCanvas : MonoBehaviour
{
    // Delete cover after app is started
    [YarnCommand("hide_cover")]
    public void hide_cover() {
        Destroy(this.gameObject);  // destroys the GameObject this script is attached to
    }
}
