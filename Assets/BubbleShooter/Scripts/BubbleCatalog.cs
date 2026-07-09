using UnityEngine;

namespace MergeCafe.Bubble
{
    /// <summary>The bubble colours of the shooter game.</summary>
    public static class BubbleCatalog
    {
        public const int ColorCount = 6;

        private static readonly Color[] Colors =
        {
            Hex("E24B4B"), // red
            Hex("3F8EE0"), // blue
            Hex("5BC85B"), // green
            Hex("F2C94C"), // yellow
            Hex("A264C8"), // purple
            Hex("F0883A"), // orange
        };

        public static Color Color(int index)
        {
            int i = ((index % ColorCount) + ColorCount) % ColorCount;
            return Colors[i];
        }

        private static Color Hex(string rgb) =>
            ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : UnityEngine.Color.magenta;
    }
}
