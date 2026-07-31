using System.Collections.Generic;
using UnityEngine;

namespace MagicUtilities
{
    public enum ComparisonType { Less, LessEqual, Equal, Greater, GreaterEqual }

    public static class MathUtilities
    {
        // Returns one of the values based on its weight
        // Configuration picked = PickWeighted(_configs, c => c.spawnWeight);
        public static T PickWeighted<T>(IList<T> items, System.Func<T, float> weight)
        {
            float total = 0f;
            for (int i = 0; i < items.Count; i++) total += weight(items[i]);
        
            float r = Random.value * total;
            float acc = 0f;
        
            for (int i = 0; i < items.Count; i++)
            {
                acc += weight(items[i]);
                if (r <= acc) return items[i];
            }
            return items[items.Count - 1];
        }

        // Exponential price curve: base * (1 + mult)^level
        public static int GetLevelPrice(int targetLevel, float basePrice, float multiplier)
        {
            float price = basePrice * Mathf.Pow(1f + multiplier, targetLevel);
            return Mathf.CeilToInt(price);
        }

        // True if only one bit is set
        public static bool IsSingleBit(int value) => value != 0 && (value & (value - 1)) == 0;

        // Compares two floats with the given operator
        public static bool Compare(float a, float b, ComparisonType comparison)
        {
            return comparison switch
            {
                ComparisonType.Less         => a < b,
                ComparisonType.LessEqual    => a <= b,
                ComparisonType.Equal        => Mathf.Approximately(a, b),
                ComparisonType.Greater      => a > b,
                ComparisonType.GreaterEqual => a >= b,
                _ => false,
            };
        }

        // Snaps a 2D input to one of the 8 directions (zero if inside the deadzone)
        public static Vector2Int Quantize8Dir(Vector2 input, float deadzoneSqr)
        {
            if (input.sqrMagnitude <= deadzoneSqr)
                return Vector2Int.zero;

            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            int sector = Mathf.FloorToInt((angle + 22.5f) / 45f) & 7;

            return sector switch
            {
                0 => new Vector2Int( 1,  0),
                1 => new Vector2Int( 1,  1),
                2 => new Vector2Int( 0,  1),
                3 => new Vector2Int(-1,  1),
                4 => new Vector2Int(-1,  0),
                5 => new Vector2Int(-1, -1),
                6 => new Vector2Int( 0, -1),
                _ => new Vector2Int( 1, -1),
            };
        }
    }
}