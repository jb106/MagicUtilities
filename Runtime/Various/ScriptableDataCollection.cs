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

    // ScriptableObject holding a list of elements, addressable by a stable int ID (index).
    // Just inherit it: the list is serialized and auto-populated here, nothing to override.
    public abstract class ScriptableDataCollection<T> : ScriptableObject, IDataCollection<T>
    {
        [AssetList(AutoPopulate = true)]
        [SerializeField] protected List<T> elements = new();

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
    }
}