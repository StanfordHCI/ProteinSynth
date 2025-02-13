using System.Collections.Generic;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class PrefabsInProjectCriterion : Criterion
    {
        [SerializeField]
        List<GameObject> m_Prefabs = new List<GameObject>();

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
            foreach (var prefab in m_Prefabs)
            {
                if (prefab == null)
                    return false;

                var prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);
                if (prefabAssetType == PrefabAssetType.MissingAsset || prefabAssetType == PrefabAssetType.NotAPrefab)
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
