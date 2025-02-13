using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.MARS.Providers;
using Unity.MARS.Visualizers;
using Unity.Tutorials.Core.Editor;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEditor.MARS.Simulation;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class AnyPlanesInSimulationCriterion : Criterion
    {
        MARSPlaneVisualizer m_PlaneVisualizer;

        static readonly List<MRPlane> k_Planes = new List<MRPlane>();

        public override void StartTesting()
        {
            if (TryFindPlanesVisualizerInSim())
                UpdateCompletion();

            EditorApplication.update += UpdateCompletion;
        }

        public override void StopTesting()
        {
            EditorApplication.update -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            if (m_PlaneVisualizer == null && !TryFindPlanesVisualizerInSim())
                return false;

            if (!m_PlaneVisualizer.HasProvider<IProvidesPlaneFinding>())
                return false;

            k_Planes.Clear();
            m_PlaneVisualizer.GetPlanes(k_Planes);
            return k_Planes.Count > 0;
        }

        bool TryFindPlanesVisualizerInSim()
        {
            var simObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
            if (simObjectsManager == null || simObjectsManager.SimulatedContentRoot == null)
                return false;

            m_PlaneVisualizer = simObjectsManager.SimulatedContentRoot.GetComponentInChildren<MARSPlaneVisualizer>();
            return m_PlaneVisualizer != null;
        }

        public override bool AutoComplete()
        {
            return EvaluateCompletion();
        }
    }
}
