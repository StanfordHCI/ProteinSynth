using Unity.Tutorials.Core.Editor;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;

namespace Unity.MARS.Tutorials.Editor
{
    class SimulationPreviewRunningCriterion : Criterion
    {
        public override void StartTesting()
        {
            UpdateCompletion();
            QuerySimulationModule.onTemporalSimulationStart += UpdateCompletion;
            QuerySimulationModule.onTemporalSimulationStop += UpdateCompletion;
        }

        public override void StopTesting()
        {
            QuerySimulationModule.onTemporalSimulationStart -= UpdateCompletion;
            QuerySimulationModule.onTemporalSimulationStop -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            var simulationModule = ModuleLoaderCore.instance.GetModule<QuerySimulationModule>();
            return simulationModule != null && simulationModule.simulatingTemporal;
        }

        public override bool AutoComplete()
        {
            var simulationModule = ModuleLoaderCore.instance.GetModule<QuerySimulationModule>();
            if (simulationModule == null)
                return false;

            if (simulationModule.simulatingTemporal)
                return true;

            simulationModule.RequestSimulationModeSelection(SimulationModeSelection.TemporalMode);
            simulationModule.RestartSimulationIfNeeded();
            return true;
        }
    }
}
