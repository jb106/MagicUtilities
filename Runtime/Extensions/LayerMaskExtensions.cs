using UnityEngine;

namespace MagicUtilities
{
    public static class LayerMaskExtensions
    {
        // True if the mask contains the given layer index
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        // True if the mask contains the object's layer
        public static bool Contains(this LayerMask mask, GameObject go)
        {
            return mask.Contains(go.layer);
        }
    }
}