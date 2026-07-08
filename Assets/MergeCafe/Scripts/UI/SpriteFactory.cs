using UnityEngine;

namespace MergeCafe.UI
{
    /// <summary>
    /// Generates the few simple shapes the game needs (rounded rect, circle) as
    /// textures at runtime, so the repository contains zero image files.
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _roundedRect;
        private static Sprite _circle;

        /// <summary>White 9-sliceable rounded rectangle. Use with Image.type = Sliced.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect == null)
                    _roundedRect = CreateRoundedRect(64, 18);
                return _roundedRect;
            }
        }

        /// <summary>White anti-aliased circle.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                    _circle = CreateCircle(96);
                return _circle;
            }
        }

        private static Sprite CreateRoundedRect(int size, int radius)
        {
            Texture2D texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded rectangle centered in the texture.
                    float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) +
                                               Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                    float inside = Mathf.Min(Mathf.Max(px, py), 0f);
                    float distance = outside + inside - radius;

                    byte alpha = (byte)(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            int border = radius + 6;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private static Sprite CreateCircle(int size)
        {
            Texture2D texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float radius = half - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    byte alpha = (byte)(Mathf.Clamp01(0.5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}
