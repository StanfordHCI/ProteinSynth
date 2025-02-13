using Unity.MARS.Query;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    // Ordinarily we would use PropertyModificationCriterion but it does not support enum values,
    // so we use a criterion specifically for exclusivity
    class ProxyExclusivityCriterion : Criterion
    {
        [SerializeField]
        ObjectReference m_ProxyReference;

        [SerializeField]
        Exclusivity m_RequiredExclusivity;

        public override void StartTesting()
        {
            UpdateCompletion();
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += UpdateCompletion;
        }

        public override void StopTesting()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= UpdateCompletion;
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            UpdateCompletion();
            return modifications;
        }

        protected override bool EvaluateCompletion()
        {
            var referencedObject = m_ProxyReference.SceneObjectReference.ReferencedObject;
            if (referencedObject == null)
                return false;

            var proxy = referencedObject as Proxy;
            if (proxy == null)
                return false;

            return proxy.exclusivity == m_RequiredExclusivity;
        }

        public override bool AutoComplete()
        {
            var referencedObject = m_ProxyReference.SceneObjectReference.ReferencedObject;
            if (referencedObject == null)
                return false;

            var proxy = referencedObject as Proxy;
            if (proxy == null)
                return false;

            proxy.exclusivity = m_RequiredExclusivity;
            UpdateCompletion();
            return true;
        }
    }
}
