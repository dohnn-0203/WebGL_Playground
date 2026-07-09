using System.Collections.Generic;
using MergeCafe.UI;
using UnityEngine;

namespace MergeCafe.Bubble
{
    /// <summary>
    /// Procedurally drawn glossy bubble sprites (colored circle + rim + highlight) via
    /// the shared <see cref="IconCanvas"/> rasterizer. Sprites are cached. Circle drawn
    /// at ~0.58 world radius (100 PPU) so callers scale the object to the play radius.
    /// </summary>
    public static class BubbleSprites
    {
        public const float SpriteRadius = 0.58f;

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();
        private static Sprite _solid;

        public static Sprite Bubble(int color)
        {
            if (!Cache.TryGetValue(color, out Sprite s))
            {
                s = ToSprite(Pixels(color, 128), 128);
                Cache[color] = s;
            }
            return s;
        }

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

        public static Color32[] Pixels(int color, int size)
        {
            var c = new IconCanvas(size);
            Color body = BubbleCatalog.Color(color);
            Color rim = Color.Lerp(body, Color.black, 0.28f);

            c.Circle(64, 64, 60, rim);
            c.Circle(64, 64, 54, body);
            c.Circle(64, 56, 44, Color.Lerp(body, Color.white, 0.12f)); // soft inner sheen
            c.Ellipse(48, 86, 17, 10, new Color(1f, 1f, 1f, 0.7f));       // glossy highlight
            return c.ToPixels();
        }

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
