using Unity.Tutorials.Core;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;

namespace Unity.MARS.Tutorials.Editor
{
    /// <summary>
    /// Module that ensures in-Editor tutorials work with Simulation preview
    /// </summary>
    class SimulationPreviewTutorialsModule : IModuleDependency<SimulatedObjectsManager>
    {
        void IModuleDependency<SimulatedObjectsManager>.ConnectDependency(SimulatedObjectsManager dependency)
        {
            // Don't let SceneObjectGuids be copied to preview scene, or else there will be conflicting identifiers
            dependency.ComponentTypesToRemoveFromCopies.Add(typeof(SceneObjectGuid));
        }

        void IModule.LoadModule() { }

        void IModule.UnloadModule() { }
    }
}
