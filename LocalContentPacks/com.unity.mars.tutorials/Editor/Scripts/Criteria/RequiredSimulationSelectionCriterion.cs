using Unity.Tutorials.Core.Editor;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEditor.MARS.Simulation;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking that the simulated content copy of an object is selected
    /// </summary>
    class RequiredSimulationSelectionCriterion : Criterion
    {
        [SerializeField]
        ObjectReference m_OriginalGameObject;

        public override void StartTesting()
        {
            UpdateCompletion();
            Selection.selectionChanged += UpdateCompletion;
        }

        public override void StopTesting()
        {
            Selection.selectionChanged -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            var simObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
            if (simObjectsManager == null)
                return false;

            var sceneObjRef = m_OriginalGameObject.SceneObjectReference;
            if (!sceneObjRef.IsGameObjectReference)
                return false;

            var referencedObject = sceneObjRef.ReferencedObject;
            if (referencedObject == null)
                return false;

            var referencedGameObject = referencedObject as GameObject;
            var simCopyTransform = simObjectsManager.GetCopiedTransform(referencedGameObject.transform);
            return simCopyTransform != null && Selection.Contains(simCopyTransform.gameObject);
        }

        public override bool AutoComplete()
        {
            var simObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
            if (simObjectsManager == null)
                return false;

            var sceneObjRef = m_OriginalGameObject.SceneObjectReference;
            if (!sceneObjRef.IsGameObjectReference)
                return false;

            var referencedObject = sceneObjRef.ReferencedObject;
            if (referencedObject == null)
                return false;

            var referencedGameObject = referencedObject as GameObject;
            var simCopyTransform = simObjectsManager.GetCopiedTransform(referencedGameObject.transform);
            if (simCopyTransform == null)
                return false;

            Selection.activeObject = simCopyTransform.gameObject;
            return true;
        }
    }
}
