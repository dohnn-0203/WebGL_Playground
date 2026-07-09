using System.Collections.Generic;
using MergeCafe.UI;
using UnityEngine;

namespace MergeCafe.Suika
{
    /// <summary>
    /// Procedurally drawn round fruit sprites (with cute faces) for the watermelon
    /// game. Uses the shared <see cref="IconCanvas"/> rasterizer; sprites are cached.
    /// The circle is drawn at ~0.58 world radius (at 100 PPU) so callers can scale the
    /// fruit object to the physical radius.
    /// </summary>
    public static class SuikaFruitSprites
    {
        public const float SpriteRadius = 0.58f; // world units at 100 PPU, radius 58px of 128

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
        private static Sprite _solid;

        public static Sprite Fruit(int level)
        {
            if (!Cache.TryGetValue(level, out Sprite s))
            {
                s = ToSprite(Pixels(level, 128), 128);
                Cache[level] = s;
            }
            return s;
        }

        /// <summary>Plain 1px white sprite for walls / bars (tint via SpriteRenderer.color).</summary>
        public static Sprite Solid
        {
            get
            {
                if (_solid == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(px);
                    tex.Apply();
                    _solid = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return _solid;
            }
        }

        public static Color32[] Pixels(int level, int size)
        {
            var c = new IconCanvas(size);
            Color body = SuikaCatalog.Color(level);

            c.Circle(64, 60, 58, body);
            c.Ellipse(46, 84, 22, 13, Lighten(body, 0.4f)); // glossy highlight

            Details(c, level, body);
            Face(c);
            return c.ToPixels();
        }

        private static void Details(IconCanvas c, int level, Color body)
        {
            Color leaf = H("4CAF50");
            switch (level)
            {
                case 2: // strawberry seeds + leaf
                    c.Ellipse(64, 118, 16, 8, leaf);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        c.Circle(64 + Mathf.Cos(a) * 26f, 56 + Mathf.Sin(a) * 26f, 2.4f, H("FFF2CC"));
                    }
                    break;
                case 9: // pineapple: crosshatch + crown
                    for (int k = -2; k <= 2; k++)
                    {
                        c.Line(64 + k * 20, 12, 64 + k * 20 + 24, 108, 2f, H("C79A24"));
                        c.Line(64 + k * 20, 12, 64 + k * 20 - 24, 108, 2f, H("C79A24"));
                    }
                    c.Triangle(52, 108, 76, 108, 64, 128, leaf);
                    c.Triangle(40, 104, 60, 104, 50, 124, H("66BB6A"));
                    c.Triangle(68, 104, 88, 104, 78, 124, H("66BB6A"));
                    break;
                case 10: // melon net
                    for (int k = -2; k <= 2; k++)
                        c.Line(64 + k * 18, 8, 64 + k * 18, 112, 1.6f, H("7CB342"));
                    c.Curve(64, 70, 46, H("7CB342"));
                    c.Curve(64, 44, 46, H("7CB342"));
                    break;
                case 11: // watermelon stripes
                    for (int k = -2; k <= 2; k++)
                        c.Line(64 + k * 22, 8, 64 + k * 22, 112, 5f, H("2E6B32"));
                    break;
                default: // simple leaf + stem for the round fruits
                    c.Line(64, 108, 66, 124, 3f, H("7B5E3B"));
                    c.Ellipse(78, 116, 14, 7, leaf);
                    break;
            }
        }

        private static void Face(IconCanvas c)
        {
            Color dark = new Color(0.18f, 0.12f, 0.10f, 0.9f);
            c.Circle(50, 66, 5.5f, dark);
            c.Circle(78, 66, 5.5f, dark);
            c.Circle(48.5f, 68f, 2f, Color.white);
            c.Circle(76.5f, 68f, 2f, Color.white);
            c.Curve(64, 50, 12, dark); // smile
        }

        private static Color Lighten(Color c, float t) =>
            Color.Lerp(c, Color.white, t);

        private static Color H(string rgb) =>
            ColorUtility.TryParseHtmlString("#" + rgb, out Color c) ? c : Color.magenta;

        private static Sprite ToSprite(Color32[] pixels, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
