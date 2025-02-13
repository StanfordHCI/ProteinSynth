using UnityEditor;

namespace Unity.MARS.Tutorials.Editor
{
    static class GameTemplateTutorialUtils
    {
        internal const string CrystalsReplicatorName = "Place crystals on all reasonable sized surfaces";
        internal const string BuildSurfaceReplicatorName = "Put colliders on all walkable surfaces";
        internal const string ARCharacterName = "Unit-E";

        const string k_GameSimpleSceneGUID = "e9e3e4e415d47d149a622673b24b1a66";

        internal static string GetGameSimpleScenePath() => AssetDatabase.GUIDToAssetPath(k_GameSimpleSceneGUID);
    }
}
