using Unity.Tutorials.Core.Editor;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class ComponentsInSimulationCriterion : Criterion
    {
        [SerializeField]
        ComponentAddedCriterion.SerializedTypeCollection m_RequiredComponentTypes = new ComponentAddedCriterion.SerializedTypeCollection();

        public override void StartTesting()
        {
            UpdateCompletion();
            QuerySimulationModule.OnOneShotSimulationStart += UpdateCompletion;
            QuerySimulationModule.onTemporalSimulationStart += UpdateCompletion;
        }

        public override void StopTesting()
        {
            QuerySimulationModule.OnOneShotSimulationStart -= UpdateCompletion;
            QuerySimulationModule.onTemporalSimulationStart -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            if (m_RequiredComponentTypes.Count == 0)
                return false;

            var simObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
            if (simObjectsManager == null || simObjectsManager.SimulatedContentRoot == null)
                return false;

            foreach (var typeAndFutureReference in m_RequiredComponentTypes)
            {
                var componentType = typeAndFutureReference?.SerializedType?.Type;
                if (componentType == null)
                    return false;

                var componentInstance = simObjectsManager.SimulatedContentRoot.GetComponentInChildren(componentType);
                if (componentInstance == null)
                    return false;
            }

            return true;
        }

        public override bool AutoComplete()
        {
            return EvaluateCompletion();
        }
    }
}
