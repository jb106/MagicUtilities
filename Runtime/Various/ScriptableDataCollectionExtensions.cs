namespace MagicUtilities
{
    public static class ScriptableDataCollectionExtensions
    {
        // Element with this persistent id, null if none
        public static T GetByPersistentId<T>(this ScriptableDataCollection<T> collection, string persistentId)
            where T : PersistentScriptableObject
        {
            if (collection == null || string.IsNullOrEmpty(persistentId)) return null;

            foreach (var element in collection.Elements)
            {
                if (element != null && element.PersistentId == persistentId) return element;
            }

            return null;
        }
    }
}