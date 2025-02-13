using Unity.Tutorials.Core.Editor;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class MultipleEnvironmentsAvailableCriterion : Criterion
    {
        public override void StartTesting()
        {
            UpdateCompletion();
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            if (environmentManager != null)
                environmentManager.EnvironmentChanged += UpdateCompletion;
        }

        public override void StopTesting()
        {
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            if (environmentManager != null)
                environmentManager.EnvironmentChanged -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            return environmentManager != null && environmentManager.EnvironmentPrefabPaths.Count > 1;
        }

        public override bool AutoComplete()
        {
            return EvaluateCompletion();
        }
    }
}
