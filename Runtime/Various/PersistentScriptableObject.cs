using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicUtilities
{
    // ScriptableObject with a stable id that survives renames, moves and list reordering.
    // The id is the asset's own GUID, written once and never changed for a given asset.
    // Use it as the save-safe reference (unlike a collection index, which can shift).
    public abstract class PersistentScriptableObject : ScriptableObject
    {
        [SerializeField, ReadOnly] private string persistentId;

        // The asset GUID this id was born in; used to detect duplicates deterministically.
        [SerializeField, HideInInspector] private string ownerAssetGuid;

        public string PersistentId => persistentId;

#if UNITY_EDITOR
        // Handles inspector edits. Override to add your own logic, then call base.
        protected virtual void OnValidate()
        {
            ScheduleEnsureId();
        }

        // Deferred so the AssetDatabase is safe to query and the change can actually be saved.
        internal void ScheduleEnsureId()
        {
            UnityEditor.EditorApplication.delayCall -= EnsureId;
            UnityEditor.EditorApplication.delayCall += EnsureId;
        }

        private void EnsureId()
        {
            UnityEditor.EditorApplication.delayCall -= EnsureId;
            if (this == null) return;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath)) return; // not saved as an asset yet

            string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid)) return;

            bool changed = false;

            if (string.IsNullOrEmpty(persistentId))
            {
                // Brand new asset: adopt its GUID as the persistent id.
                persistentId = assetGuid;
                ownerAssetGuid = assetGuid;
                changed = true;
            }
            else if (string.IsNullOrEmpty(ownerAssetGuid))
            {
                // Existing asset from a previous system: keep its id, just record the owner.
                ownerAssetGuid = assetGuid;
                changed = true;
            }
            else if (ownerAssetGuid != assetGuid)
            {
                // This asset now lives in a different GUID than the one its id was born in,
                // i.e. it is a duplicate. Give the copy a fresh id; the original is untouched.
                persistentId = assetGuid;
                ownerAssetGuid = assetGuid;
                changed = true;
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            }
        }
#endif
    }

#if UNITY_EDITOR
    // Reliably assigns the id when an asset is created, duplicated or imported
    // (OnValidate does not fire consistently on first creation).
    internal class PersistentScriptableObjectPostprocessor : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".asset")) continue;

                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<PersistentScriptableObject>(path);
                if (so != null) so.ScheduleEnsureId();
            }
        }
    }
#endif
}
