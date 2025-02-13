using Unity.Tutorials.Core.Editor;
using UnityEditor;

namespace Unity.MARS.Tutorials.Editor
{
    [InitializeOnLoad]
    static class MarsTutorialStartup
    {
        const string k_NeverShowDialogTitle = "Unity MARS Tutorials";
        const string k_NeverShowDialogMessage = "Unity shows tutorials the first time each new Unity MARS project is opened. " +
            "Do you want to never show tutorials on startup for future Unity MARS projects? You can always change this in " +
            "Preferences > MARS > Tutorials.";
        const string k_NeverShowDialogOkLabel = "Never show";
        const string k_NeverShowDialogCancelLabel = "Keep showing";

        const string k_DontShowOnStartupKey = "MARS.DontShowTutorialsOnStartup";

        static MarsTutorialStartup()
        {
            if (!TutorialFrameworkHelpers.IsProjectInitialized())
            {
                if (ShouldNotShowOnStartup())
                {
                    // Don't show tutorials only if there are no startup settings and the only tutorial container is the MARS one
                    var tutorialContainerGUIDs = AssetDatabase.FindAssets($"t:{typeof(TutorialContainer).FullName}");
                    var tutorialProjectSettingsGUIDs = AssetDatabase.FindAssets($"t:{typeof(TutorialProjectSettings).FullName}");
                    if (tutorialContainerGUIDs.Length == 1 && tutorialProjectSettingsGUIDs.Length == 0)
                        TutorialFrameworkHelpers.PreventProjectInitialization();
                }

                if (!TutorialFrameworkHelpers.IsProjectInitializationPrevented())
                    TutorialFrameworkHelpers.AfterProjectInit += OnAfterTutorialProjectInit;
            }
        }

        static void OnAfterTutorialProjectInit()
        {
            TutorialFrameworkHelpers.AfterProjectInit -= OnAfterTutorialProjectInit;
            if (EditorPrefs.HasKey(k_DontShowOnStartupKey))
                return;

            var dontShowOnStartup = EditorUtility.DisplayDialog(
                k_NeverShowDialogTitle, k_NeverShowDialogMessage, k_NeverShowDialogOkLabel, k_NeverShowDialogCancelLabel);

            SetNeverShowOnStartup(dontShowOnStartup);
        }

        internal static bool ShouldNotShowOnStartup()
        {
            return EditorPrefs.HasKey(k_DontShowOnStartupKey) && EditorPrefs.GetBool(k_DontShowOnStartupKey);
        }

        internal static void SetNeverShowOnStartup(bool neverShow)
        {
            EditorPrefs.SetBool(k_DontShowOnStartupKey, neverShow);
        }
    }
}
