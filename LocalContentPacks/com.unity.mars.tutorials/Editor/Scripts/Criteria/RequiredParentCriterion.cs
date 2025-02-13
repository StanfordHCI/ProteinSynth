using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class RequiredParentCriterion : Criterion
    {
        [SerializeField]
        ObjectReference m_TargetGameObject;

        [SerializeField]
        ObjectReference m_RequiredParentGameObject;

        public override void StartTesting()
        {
            UpdateCompletion();
            EditorApplication.hierarchyChanged += UpdateCompletion;
        }

        public override void StopTesting()
        {
            EditorApplication.hierarchyChanged -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            var targetSceneObjRef = m_TargetGameObject.SceneObjectReference;
            var requiredParentSceneObjRef = m_RequiredParentGameObject.SceneObjectReference;
            if (!targetSceneObjRef.IsGameObjectReference || !requiredParentSceneObjRef.IsGameObjectReference)
                return false;

            var targetGameObj = targetSceneObjRef.ReferencedObjectAsGameObject;
            var requiredParentGameObj = requiredParentSceneObjRef.ReferencedObjectAsGameObject;
            if (targetGameObj == null || requiredParentGameObj == null)
                return false;

            return targetGameObj.transform.parent == requiredParentGameObj.transform;
        }

        public override bool AutoComplete()
        {
            var targetSceneObjRef = m_TargetGameObject.SceneObjectReference;
            var requiredParentSceneObjRef = m_RequiredParentGameObject.SceneObjectReference;
            if (!targetSceneObjRef.IsGameObjectReference || !requiredParentSceneObjRef.IsGameObjectReference)
                return false;

            var targetGameObj = targetSceneObjRef.ReferencedObjectAsGameObject;
            var requiredParentGameObj = requiredParentSceneObjRef.ReferencedObjectAsGameObject;
            if (targetGameObj == null || requiredParentGameObj == null)
                return false;

            targetGameObj.transform.parent = requiredParentGameObj.transform;
            UpdateCompletion();
            return true;
        }
    }
}
