using System.Collections.Generic;
using Unity.MARS.Conditions;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.MARS.Tutorials.Editor
{
    class GameTemplateSceneOpenCriterion : Criterion
    {
        const string k_CrystalsReplicatorReferenceName = "Crystals Replicator Game Object";
        const string k_CrystalProxyObjectReferenceName = "Crystal Proxy Game Object";
        const string k_CrystalAlignmentConditionReferenceName = "Crystal Alignment Condition";
        const string k_BuildSurfaceProxyObjectReferenceName = "Build Surface Proxy Game Object";
        const string k_ARCharacterObjectReferenceName = "AR Character Game Object";

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_FutureCrystalsReplicatorReference;

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_FutureCrystalProxyObjectReference;

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_FutureCrystalAlignmentConditionReference;

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_FutureBuildSurfaceProxyObjectReference;

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_FutureARCharacterReference;

        public override void StartTesting()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            UpdateCompletion();
        }

        public override void StopTesting()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UpdateCompletion();
        }

        void OnSceneSaved(Scene scene)
        {
            UpdateCompletion();
        }

        protected override void OnValidate()
        {
            // Destroy unused future reference assets
            base.OnValidate();

            if (m_FutureCrystalsReplicatorReference == null)
                m_FutureCrystalsReplicatorReference = CreateFutureObjectReference();

            if (m_FutureCrystalProxyObjectReference == null)
                m_FutureCrystalProxyObjectReference = CreateFutureObjectReference();

            if (m_FutureCrystalAlignmentConditionReference == null)
                m_FutureCrystalAlignmentConditionReference = CreateFutureObjectReference();

            if (m_FutureBuildSurfaceProxyObjectReference == null)
                m_FutureBuildSurfaceProxyObjectReference = CreateFutureObjectReference();

            if (m_FutureARCharacterReference == null)
                m_FutureARCharacterReference = CreateFutureObjectReference();

            m_FutureCrystalsReplicatorReference.ReferenceName = k_CrystalsReplicatorReferenceName;
            m_FutureCrystalProxyObjectReference.ReferenceName = k_CrystalProxyObjectReferenceName;
            m_FutureCrystalAlignmentConditionReference.ReferenceName = k_CrystalAlignmentConditionReferenceName;
            m_FutureBuildSurfaceProxyObjectReference.ReferenceName = k_BuildSurfaceProxyObjectReferenceName;
            m_FutureARCharacterReference.ReferenceName = k_ARCharacterObjectReferenceName;

            UpdateFutureObjectReferenceNames();
        }

        protected override bool EvaluateCompletion()
        {
            // No need to check if the template scene itself is opened, because the Templates window opens a template
            // in a new scene. Instead, we check for completion by looking for the existence of specific objects,
            // from which we need future references for the rest of the tutorial.

            // Scene must be saved, otherwise future object references will not work
            var activeScene = SceneManager.GetActiveScene();
            if (AssetDatabase.AssetPathToGUID(activeScene.path).IsNullOrEmpty())
                return false;

            // Update future object references
            var crystalsReplicatorGameObj = GameObject.Find(GameTemplateTutorialUtils.CrystalsReplicatorName);
            var buildSurfaceReplicatorGameObj = GameObject.Find(GameTemplateTutorialUtils.BuildSurfaceReplicatorName);
            var arCharacterGameObj = GameObject.Find(GameTemplateTutorialUtils.ARCharacterName);

            if (crystalsReplicatorGameObj == null || buildSurfaceReplicatorGameObj == null || arCharacterGameObj == null)
                return false;

            var crystalProxy = crystalsReplicatorGameObj.GetComponentInChildren<Proxy>();
            var buildSurfaceProxy = buildSurfaceReplicatorGameObj.GetComponentInChildren<Proxy>();
            if (crystalProxy == null || buildSurfaceProxy == null)
                return false;

            var crystalAlignmentCondition = crystalProxy.GetComponent<AlignmentCondition>();
            if (crystalAlignmentCondition == null)
                return false;

            m_FutureCrystalsReplicatorReference.SceneObjectReference.Update(crystalsReplicatorGameObj);
            m_FutureCrystalProxyObjectReference.SceneObjectReference.Update(crystalProxy.gameObject);
            m_FutureCrystalAlignmentConditionReference.SceneObjectReference.Update(crystalAlignmentCondition);
            m_FutureBuildSurfaceProxyObjectReference.SceneObjectReference.Update(buildSurfaceProxy.gameObject);
            m_FutureARCharacterReference.SceneObjectReference.Update(arCharacterGameObj);
            return true;
        }

        protected override IEnumerable<FutureObjectReference> GetFutureObjectReferences()
        {
            var references = new List<FutureObjectReference>();
            if (m_FutureCrystalsReplicatorReference != null)
                references.Add(m_FutureCrystalsReplicatorReference);

            if (m_FutureCrystalProxyObjectReference != null)
                references.Add(m_FutureCrystalProxyObjectReference);

            if (m_FutureCrystalAlignmentConditionReference != null)
                references.Add(m_FutureCrystalAlignmentConditionReference);

            if (m_FutureBuildSurfaceProxyObjectReference != null)
                references.Add(m_FutureBuildSurfaceProxyObjectReference);

            if (m_FutureARCharacterReference != null)
                references.Add(m_FutureARCharacterReference);

            return references;
        }

        public override bool AutoComplete()
        {
            var scenePath = GameTemplateTutorialUtils.GetGameSimpleScenePath();
            if (scenePath.IsNotNullOrEmpty())
            {
                EditorSceneManager.OpenScene(scenePath);
                return true;
            }

            return false;
        }
    }
}
