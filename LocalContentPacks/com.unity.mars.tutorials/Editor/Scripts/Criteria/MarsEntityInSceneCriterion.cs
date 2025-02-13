using System.Collections.Generic;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.MARS.Tutorials.Editor
{
    class MarsEntityInSceneCriterion : Criterion
    {
        [SerializeField]
        [SerializedTypeFilter(typeof(MARSEntity), false)]
        SerializedType m_RequiredEntityType;

        [SerializeField]
        ObjectReference m_ExcludedEntityReference;

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_EntityFutureReference;

        [SerializeField]
        [HideInInspector]
        FutureObjectReference m_EntityGameObjectFutureReference;

        static readonly List<GameObject> k_RootGameObjects = new List<GameObject>();

        public override void StartTesting()
        {
            UpdateCompletion();
            EditorApplication.hierarchyChanged += UpdateCompletion;
        }

        public override void StopTesting()
        {
            EditorApplication.hierarchyChanged -= UpdateCompletion;
        }

        protected override void OnValidate()
        {
            // Destroy unused future reference assets
            base.OnValidate();

            if (m_EntityFutureReference == null)
                m_EntityFutureReference = CreateFutureObjectReference();

            if (m_EntityGameObjectFutureReference == null)
                m_EntityGameObjectFutureReference = CreateFutureObjectReference();

            var entityType = m_RequiredEntityType.Type;
            if (entityType != null)
            {
                m_EntityFutureReference.ReferenceName = entityType.Name;
                m_EntityGameObjectFutureReference.ReferenceName = $"{entityType.Name} Game Object";
            }

            UpdateFutureObjectReferenceNames();
        }

        protected override IEnumerable<FutureObjectReference> GetFutureObjectReferences()
        {
            var references = new List<FutureObjectReference>();
            if (m_EntityFutureReference != null)
                references.Add(m_EntityFutureReference);

            if (m_EntityGameObjectFutureReference != null)
                references.Add(m_EntityGameObjectFutureReference);

            return references;
        }

        protected override bool EvaluateCompletion()
        {
            var entityType = m_RequiredEntityType.Type;
            if (entityType == null)
                return false;

            var excludedEntity = m_ExcludedEntityReference.SceneObjectReference.ReferencedObjectAsComponent;
            var activeScene = SceneManager.GetActiveScene();
            k_RootGameObjects.Clear();
            activeScene.GetRootGameObjects(k_RootGameObjects);
            foreach (var gameObject in k_RootGameObjects)
            {
                var foundEntities = gameObject.GetComponentsInChildren(entityType);
                foreach (var entity in foundEntities)
                {
                    if (entity != excludedEntity)
                    {
                        m_EntityFutureReference.SceneObjectReference.Update(entity);
                        m_EntityGameObjectFutureReference.SceneObjectReference.Update(entity.gameObject);
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool AutoComplete()
        {
            var objectCreationResources = MarsObjectCreationResources.instance;
            if (objectCreationResources == null)
                return false;

            var entityType = m_RequiredEntityType.Type;
            if (entityType == null)
                return false;

            if (entityType == typeof(Proxy))
            {
                objectCreationResources.ProxyObjectPreset.CreateGameObject(out var createdObj, null);
                UpdateCompletion();
                return true;
            }

            if (entityType == typeof(ProxyGroup))
            {
                objectCreationResources.ProxyGroupPreset.CreateGameObject(out var createdObj, null);
                UpdateCompletion();
                return true;
            }

            if (entityType == typeof(Replicator))
            {
                objectCreationResources.ReplicatorPreset.CreateGameObject(out var createdObj, null);
                UpdateCompletion();
                return true;
            }

            return false;
        }
    }
}
