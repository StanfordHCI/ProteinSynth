using System.IO;
using Unity.Tutorials.Core.Editor;
using UnityEditor;

namespace Unity.MARS.Tutorials.Editor
{
    class GameTemplateInProjectCriterion : Criterion
    {
        public override void StartTesting()
        {
            UpdateCompletion();
            EditorApplication.projectChanged += UpdateCompletion;
        }

        public override void StopTesting()
        {
            EditorApplication.projectChanged -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            var path = GameTemplateTutorialUtils.GetGameSimpleScenePath();
            return path.IsNotNullOrEmpty() && File.Exists(path);
        }

        public override bool AutoComplete()
        {
            return EvaluateCompletion();
        }
    }
}
