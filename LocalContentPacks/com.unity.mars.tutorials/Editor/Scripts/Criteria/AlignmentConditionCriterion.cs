using Unity.MARS.Conditions;
using Unity.MARS.Data;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    // Ordinarily we would use PropertyModificationCriterion but it does not support enum values,
    // so we use a criterion specifically for alignment
    class AlignmentConditionCriterion : Criterion
    {
        [SerializeField]
        ObjectReference m_AlignmentConditionReference;

        [SerializeField]
        MarsPlaneAlignment m_RequiredAlignment;

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
            var referencedObject = m_AlignmentConditionReference.SceneObjectReference.ReferencedObject;
            if (referencedObject == null)
                return false;

            var alignmentCondition = referencedObject as AlignmentCondition;
            if (alignmentCondition == null)
                return false;

            return alignmentCondition.alignment == m_RequiredAlignment;
        }

        public override bool AutoComplete()
        {
            var referencedObject = m_AlignmentConditionReference.SceneObjectReference.ReferencedObject;
            if (referencedObject == null)
                return false;

            var alignmentCondition = referencedObject as AlignmentCondition;
            if (alignmentCondition == null)
                return false;

            alignmentCondition.alignment = m_RequiredAlignment;
            UpdateCompletion();
            return true;
        }
    }
}
