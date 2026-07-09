using System.IO;
using MergeCafe.UI;
using UnityEditor;
using UnityEngine;

namespace MergeCafe.EditorTools
{
    /// <summary>
    /// Dev-only: renders the procedural cafe motifs to PNG so their shapes can be
    /// eyeballed without running the game. Output path is passed via -exportDir.
    /// Never ships in a build (Editor assembly).
    /// </summary>
    public static class DecorPreviewExporter
    {
        [MenuItem("MergeCafe/Export Decor Previews")]
        public static void Export()
        {
            string dir = GetArg("-exportDir") ?? "Temp/DecorPreviews";
            Directory.CreateDirectory(dir);

            Save(dir, "coffee_cup.png", CafeArt.CoffeeCupPixels(160), 160, 160);
            Save(dir, "coffee_bean.png", CafeArt.CoffeeBeanPixels(96), 96, 96);
            Save(dir, "radial_glow.png", CafeArt.RadialGlowPixels(128), 128, 128);

            // Preview the cup/bean composited on a dark backdrop, matching in-game look.
            SaveComposited(dir, "cup_on_dark.png", CafeArt.CoffeeCupPixels(160), 160,
                new Color32(0x22, 0x1A, 0x16, 0xFF), new Color32(0xF2, 0xC1, 0x4E, 0xFF), 0.5f);

            Debug.Log($"[MergeCafe] Decor previews exported to {Path.GetFullPath(dir)}");
        }

        private static void Save(string dir, string name, Color32[] pixels, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void SaveComposited(string dir, string name, Color32[] mask, int size,
            Color32 bg, Color32 fg, float alpha)
        {
            var outPixels = new Color32[size * size];
            for (int i = 0; i < outPixels.Length; i++)
            {
                float a = (mask[i].a / 255f) * alpha;
                outPixels[i] = new Color32(
                    (byte)Mathf.Lerp(bg.r, fg.r, a),
                    (byte)Mathf.Lerp(bg.g, fg.g, a),
                    (byte)Mathf.Lerp(bg.b, fg.b, a),
                    255);
            }
            Save(dir, name, outPixels, size, size);
        }

        private static string GetArg(string flag)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag)
                    return args[i + 1];
            return null;
        }
    }
}
