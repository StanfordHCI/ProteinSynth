using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEditor.MARS.Simulation;

namespace Unity.MARS.Tutorials.Editor
{
    class DeviceViewModeCriterion : Criterion
    {
        public override void StartTesting()
        {
            UpdateCompletion();
            EditorApplication.update += UpdateCompletion;
        }

        public override void StopTesting()
        {
            EditorApplication.update -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            foreach (var simView in SimulationView.SimulationViews)
            {
                if (simView.SceneType == ViewSceneType.Device)
                    return true;
            }

            return false;
        }

        public override bool AutoComplete()
        {
            var window = EditorWindow.GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Device;
            return true;
        }
    }
}
