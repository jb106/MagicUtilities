#if UNITY_EDITOR
using UnityEditor;

namespace MagicUtilities
{
    // Refreshes every ScriptableDataCollection when assets are created, deleted or moved
    internal class ScriptableDataCollectionPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!HasAssetChange(importedAssets) && !HasAssetChange(deletedAssets) && !HasAssetChange(movedAssets)) return;

            // Deferred: the AssetDatabase is not safe to query or save during an import
            EditorApplication.delayCall -= RefreshAll;
            EditorApplication.delayCall += RefreshAll;
        }

        [MenuItem("Tools/MagicUtilities/Refresh Data Collections")]
        public static void RefreshAll()
        {
            EditorApplication.delayCall -= RefreshAll;

            foreach (var type in TypeCache.GetTypesDerivedFrom<IRefreshableDataCollection>())
            {
                if (type.IsAbstract) continue;

                foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset is IRefreshableDataCollection collection) collection.RefreshElements();
                }
            }
        }

        private static bool HasAssetChange(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".asset")) return true;
            }

            return false;
        }
    }
}
#endif