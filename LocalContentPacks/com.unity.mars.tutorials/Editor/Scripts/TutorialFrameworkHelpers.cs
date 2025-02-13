using System;
using System.IO;
using UnityEditor;

namespace Unity.MARS.Tutorials.Editor
{
    [InitializeOnLoad]
    static class TutorialFrameworkHelpers
    {
        const string k_InitFileMarkerPath = "InitCodeMarker";
        const string k_DontRunInitCodeMarker = "Assets/DontRunInitCodeMarker";

        internal static event Action AfterProjectInit;

        static TutorialFrameworkHelpers()
        {
            if (!IsProjectInitialized() && !IsProjectInitializationPrevented())
                EditorApplication.update += PollForProjectInit;
        }

        internal static bool IsProjectInitialized() => File.Exists(k_InitFileMarkerPath);

        static void PollForProjectInit()
        {
            if (IsProjectInitialized())
            {
                EditorApplication.update -= PollForProjectInit;
                AfterProjectInit?.Invoke();
            }
        }

        internal static void PreventProjectInitialization()
        {
            if (!IsProjectInitializationPrevented())
                Directory.CreateDirectory(k_DontRunInitCodeMarker);
        }

        internal static bool IsProjectInitializationPrevented() => File.Exists(k_DontRunInitCodeMarker);
    }
}
