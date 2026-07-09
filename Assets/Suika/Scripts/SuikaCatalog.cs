using UnityEngine;

namespace MergeCafe.Suika
{
    /// <summary>
    /// The 11 fruits of the watermelon game (수박게임), smallest to largest, with their
    /// world radius, colour, name and merge score. Pure data, unit-testable.
    /// </summary>
    public static class SuikaCatalog
    {
        public const int Count = 11;

        /// <summary>Only the smallest fruits can be dropped by the player.</summary>
        public const int MaxDropLevel = 5;

        private static readonly string[] Names =
        {
            "체리", "딸기", "포도", "귤", "오렌지", "사과", "배", "복숭아", "파인애플", "멜론", "수박"
        };

        // World-space radii (units). Each step is a bit larger than the last.
        private static readonly float[] Radii =
        {
            0.30f, 0.40f, 0.52f, 0.66f, 0.82f, 1.00f, 1.20f, 1.42f, 1.68f, 1.98f, 2.30f
        };

        private static readonly Color[] Colors =
        {
            Hex("E23B45"), Hex("F0526A"), Hex("8E44AD"), Hex("F5A623"), Hex("F5851F"),
            Hex("E74C3C"), Hex("C6D64B"), Hex("F7A98C"), Hex("F2C14E"), Hex("A9D468"), Hex("3E8E41")
        };

        public static string Name(int level) => Names[Clamp(level) - 1];
        public static float Radius(int level) => Radii[Clamp(level) - 1];
        public static Color Color(int level) => Colors[Clamp(level) - 1];

        public static bool IsMax(int level) => level >= Count;

        /// <summary>Points awarded for forming a fruit of the given level (triangular numbers).</summary>
        public static int MergeScore(int newLevel)
        {
            int n = Clamp(newLevel);
            return n * (n + 1) / 2;
        }

        private static int Clamp(int level)
        {
            if (level < 1) return 1;
            if (level > Count) return Count;
            return level;
        }

        private static Color Hex(string rgb) =>
            ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : UnityEngine.Color.magenta;
    }
}
