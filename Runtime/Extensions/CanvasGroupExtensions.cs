using UnityEngine;

namespace MagicUtilities
{
    public static class CanvasGroupExtensions
    {
        // Toggles alpha, interactable and raycasts at once
        public static void SetEnabled(this CanvasGroup canvasGroup, bool enabled)
        {
            canvasGroup.alpha = enabled ? 1f : 0f;
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }
    }
}