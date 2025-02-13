using UnityEditor;
using UnityEngine;

namespace Unity.MARS.Tutorials.Editor
{
    class MarsTutorialPreferencesProvider : SettingsProvider
    {
        class Styles
        {
            internal readonly GUIContent NeverShowTutorialsOnStartupContent;

            public Styles()
            {
                NeverShowTutorialsOnStartupContent = new GUIContent("Never show MARS tutorials on startup",
                    "When enabled, Unity will never show tutorials when a new Unity MARS project is opened");
            }
        }

        const string k_PreferencesPath = "Preferences/MARS/Tutorials";
        const float k_SettingsLabelWidth = 215;

        static Styles s_Styles;
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        [SettingsProvider]
        public static SettingsProvider CreateMarsTutorialPreferencesProvider() { return new MarsTutorialPreferencesProvider(); }

        MarsTutorialPreferencesProvider(string path = k_PreferencesPath, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = k_SettingsLabelWidth;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var value = EditorGUILayout.Toggle(styles.NeverShowTutorialsOnStartupContent, MarsTutorialStartup.ShouldNotShowOnStartup());
                if (check.changed)
                    MarsTutorialStartup.SetNeverShowOnStartup(value);
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }
    }
}
