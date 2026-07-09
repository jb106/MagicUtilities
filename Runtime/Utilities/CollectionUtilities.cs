using System.Collections.Generic;
using UnityEngine;

namespace MagicUtilities
{
    public static class CollectionUtilities
    {
        // Returns a new list with the same elements (shallow copy)
        public static List<T> CopyList<T>(List<T> source)
        {
            List<T> copy = new List<T>(source.Count);
            foreach (T item in source)
                copy.Add(item);
            return copy;
        }

        // Shuffles the list in place (Fisher-Yates)
        public static void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
    }
}