using System.Collections.Generic;
using UnityEngine;

namespace MagicUtilities
{
    public static class RandomUtilities
    {
        // Returns random positions on the XZ plane, keeping a minimum distance between them
        public static List<Vector3> GetRandomPositions(Vector3 center, float radius, int count, float minDistance)
        {
            List<Vector3> positions = new();
            int maxAttempts = count * 20;

            for (int i = 0; i < maxAttempts && positions.Count < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

                bool tooClose = false;
                foreach (var p in positions)
                {
                    if (Vector3.Distance(candidate, p) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    positions.Add(candidate);
            }

            return positions;
        }
    }
}