using UnityEngine;

namespace MagicUtilities
{
    public static class CameraUtilities
    {
        // True if the world position is in front of the camera and inside the viewport
        public static bool IsInView(Camera cam, Vector3 worldPos)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

            bool isInFront = viewportPos.z > 0f;
            bool isInViewport =
                viewportPos.x >= 0f && viewportPos.x <= 1f &&
                viewportPos.y >= 0f && viewportPos.y <= 1f;

            return isInFront && isInViewport;
        }
    }
}