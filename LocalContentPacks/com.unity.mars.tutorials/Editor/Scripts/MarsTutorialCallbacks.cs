using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEditor.MARS;
using UnityEditor.MARS.Simulation;
using UnityEditor.XRTools.Utils;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class MarsTutorialCallbacks : EditorScriptableSettings<MarsTutorialCallbacks>
    {
        public void PingObject(Object obj)
        {
            EditorGUIUtility.PingObject(obj);
        }

        public void ShowSceneView()
        {
            EditorWindow.GetWindow<SceneView>();
        }

        public void ShowMarsPanel()
        {
            EditorWindow.GetWindow<MarsPanel>();
        }

        public void ShowSimulationView()
        {
            EditorWindow.GetWindow<SimulationView>();
        }
    }
}
