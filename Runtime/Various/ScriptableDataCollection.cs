using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicUtilities
{
    // A registry of elements addressable by a stable int ID (their index)
    public interface IDataCollection<T>
    {
        int GetElementID(T element);
        T GetElementByID(int elementID);
        int GetRandomElementIndex();
    }
    
    // Lets the editor refresh any collection without knowing its element type
    public interface IRefreshableDataCollection
    {
    #if UNITY_EDITOR
        void RefreshElements();
    #endif
    }

    // ScriptableObject holding a list of elements, addressable by a stable int ID (index).
    // Just inherit it: the list is serialized and auto-populated here, nothing to override.
    public abstract class ScriptableDataCollection<T> : ScriptableObject, IDataCollection<T>, IRefreshableDataCollection
    {
        [ReadOnly, SerializeField] protected List<T> elements = new();

        public IReadOnlyList<T> Elements => elements;

        public int Count => elements.Count;

        public T this[int index] => GetElementByID(index);

        // Index of the element, -1 if not found
        public int GetElementID(T element) => elements.IndexOf(element);

        // Element at the given ID, null/default if out of range
        public T GetElementByID(int elementID)
        {
            if (elementID < 0 || elementID >= elements.Count) return default;
            return elements[elementID];
        }

        // Random valid index, -1 if empty
        public int GetRandomElementIndex()
        {
            if (elements.Count == 0) return -1;
            return Random.Range(0, elements.Count);
        }

        // Random element, null/default if empty
        public T GetRandomElement()
        {
            int i = GetRandomElementIndex();
            return i < 0 ? default : elements[i];
        }

        public bool Contains(T element) => elements.Contains(element);
        
        // Auto collection refresh
    #if UNITY_EDITOR
        // Rebuilds the list from every asset of type T in the project, sorted by name so the order is stable
        [Button("Refresh")]
        public void RefreshElements()
        {
            var found = new List<T>();

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                // Sub-assets included, filtered on the real type
                foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T element && !found.Contains(element)) found.Add(element);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal((a as Object)?.name, (b as Object)?.name));

            // Only write when something changed, otherwise saving would re-trigger the postprocessor forever
            if (SameElements(found)) return;

            elements = found;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }

        private bool SameElements(List<T> other)
        {
            if (other.Count != elements.Count) return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (!Equals(other[i], elements[i])) return false;
            }

            return true;
        }
    #endif
    }
}