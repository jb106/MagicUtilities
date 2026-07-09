using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MagicUtilities
{
    public static class StringUtilities
    {
        private static readonly Regex SpriteTagRegex = new Regex(@"<sprite[^>]*>", RegexOptions.Compiled);

        // Shortens big numbers (12000 -> 12K, 3400000 -> 3.4M)
        public static string FormatNumber(int value)
        {
            if (value < 10_000)
                return value.ToString();

            if (value < 1_000_000)
            {
                float k = Mathf.Floor(value / 100f) / 10f;
                return FormatShrunk(k) + "K";
            }

            float m = Mathf.Floor(value / 100_000f) / 10f;
            return FormatShrunk(m) + "M";
        }

        // Formats seconds as "1h05" or "12m"
        public static string FormatPlayTime(double seconds)
        {
            var ts = System.TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h{ts.Minutes:D2}" : $"{ts.Minutes}m";
        }

        // Wraps a string in a rich text color tag
        public static string InsertColorTag(string s, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{s}</color>";
        }

        // Wraps every <sprite> tag with a size tag
        public static string WrapSpritesWithSize(string text, int size)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return SpriteTagRegex.Replace(text, m => $"<size={size}>{m.Value}</size>");
        }

        // Same but also applies a vertical offset
        public static string WrapSpritesWithSize(string text, int size, float voffset)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return SpriteTagRegex.Replace(text, m => $"<size={size}><voffset={voffset}>{m.Value}</voffset></size>");
        }

        // Drops the decimal when the value is whole (1 -> "1", 1.5 -> "1.5")
        private static string FormatShrunk(float v)
        {
            bool whole = Mathf.Approximately(v, Mathf.Floor(v));
            return v.ToString(whole ? "0" : "0.0", CultureInfo.InvariantCulture);
        }
    }
}