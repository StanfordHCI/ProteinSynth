using System.Collections.Generic;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class RequiredProxyGroupChildrenCriterion : Criterion
    {
        [SerializeField]
        ObjectReference m_ProxyGroup;

        [SerializeField]
        ObjectReference m_RequiredChildProxy1;

        [SerializeField]
        ObjectReference m_RequiredChildProxy2;

        static readonly List<Proxy> k_ChildProxies = new List<Proxy>();

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
            var proxyGroup = m_ProxyGroup.SceneObjectReference.ReferencedObject as ProxyGroup;
            var childProxy1 = m_RequiredChildProxy1.SceneObjectReference.ReferencedObject as Proxy;
            var childProxy2 = m_RequiredChildProxy2.SceneObjectReference.ReferencedObject as Proxy;
            if (proxyGroup == null || childProxy1 == null || childProxy2 == null)
                return false;

            // Group must have exactly two children
            proxyGroup.RepopulateChildList();
            proxyGroup.GetChildList(k_ChildProxies);
            if (k_ChildProxies.Count != 2)
                return false;

            var firstChild = k_ChildProxies[0];
            var secondChild = k_ChildProxies[1];
            return firstChild == childProxy1 && secondChild == childProxy2 || firstChild == childProxy2 && secondChild == childProxy1;
        }

        public override bool AutoComplete()
        {
            return false;
        }
    }
}
