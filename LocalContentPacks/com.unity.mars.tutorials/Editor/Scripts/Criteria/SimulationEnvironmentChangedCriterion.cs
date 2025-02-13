using Unity.Tutorials.Core.Editor;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;

namespace Unity.MARS.Tutorials.Editor
{
    class SimulationEnvironmentChangedCriterion : Criterion
    {
        int m_InitialEnvironmentIndex = -1;

        public override void StartTesting()
        {
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            if (environmentManager != null)
            {
                if (SimulationSettings.instance.EnvironmentMode == EnvironmentMode.Synthetic)
                    m_InitialEnvironmentIndex = environmentManager.CurrentSyntheticEnvironmentIndex;

                environmentManager.EnvironmentChanged += UpdateCompletion;
            }
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
            return SimulationSettings.instance.EnvironmentMode == EnvironmentMode.Synthetic &&
                environmentManager != null && environmentManager.CurrentSyntheticEnvironmentIndex != m_InitialEnvironmentIndex;
        }

        public override bool AutoComplete()
        {
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            return environmentManager != null && environmentManager.TrySetupNextEnvironmentAndRestartSimulation(true);
        }
    }
}
