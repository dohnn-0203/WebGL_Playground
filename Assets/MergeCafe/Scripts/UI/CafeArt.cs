using UnityEngine;

namespace MergeCafe.UI
{
    /// <summary>
    /// Procedurally generated cafe-themed decoration sprites (coffee cup, bean,
    /// gradients). All drawn from math into textures at runtime — no image files,
    /// so the repository and public WebGL build stay 100% license-clean.
    /// The pixel generators are public so an editor tool can export PNG previews.
    /// </summary>
    public static class CafeArt
    {
        private static Sprite _coffeeCup;
        private static Sprite _coffeeBean;
        private static Sprite _verticalGradient;
        private static Sprite _radialGlow;

        public static Sprite CoffeeCup => _coffeeCup != null ? _coffeeCup : (_coffeeCup = MakeSprite(CoffeeCupPixels(160), 160));
        public static Sprite CoffeeBean => _coffeeBean != null ? _coffeeBean : (_coffeeBean = MakeSprite(CoffeeBeanPixels(96), 96));

        /// <summary>White, opaque at top → transparent at bottom. Tint via Image.color for a soft sheen.</summary>
        public static Sprite VerticalGradient =>
            _verticalGradient != null ? _verticalGradient : (_verticalGradient = MakeSprite(VerticalGradientPixels(4, 128), 4, 128));

        /// <summary>White, opaque at center → transparent at edges. Warm cozy glow behind the board.</summary>
        public static Sprite RadialGlow =>
            _radialGlow != null ? _radialGlow : (_radialGlow = MakeSprite(RadialGlowPixels(128), 128));

        // ---- Pixel generators (public for editor PNG preview) ----

        /// <summary>A steaming coffee cup silhouette: saucer + tapered body + handle + two steam wisps.</summary>
        public static Color32[] CoffeeCupPixels(int size)
        {
            var px = new Color32[size * size];
            float s = size / 160f;               // design was authored at 160px
            float cx = 70f * s;                  // body centre x (left of centre to leave room for handle)

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f;
                    float fy = y + 0.5f;
                    float a = 0f;

                    // Saucer: flat ellipse near the bottom.
                    a = Mathf.Max(a, Ellipse(fx, fy, cx, 30f * s, 60f * s, 10f * s, 1.2f));

                    // Cup body: trapezoid, wider at the rim, narrower at the base, with soft edges.
                    float bodyBottom = 40f * s, bodyTop = 104f * s;
                    if (fy >= bodyBottom - 2f && fy <= bodyTop + 2f)
                    {
                        float t = Mathf.InverseLerp(bodyBottom, bodyTop, fy);
                        float halfW = Mathf.Lerp(30f * s, 40f * s, t);
                        float edge = halfW - Mathf.Abs(fx - cx);
                        float vBody = Mathf.Clamp01(edge / (2f * s));
                        float vTopBot = Mathf.Clamp01((fy - (bodyBottom - 2f)) / (2f * s)) *
                                        Mathf.Clamp01(((bodyTop + 2f) - fy) / (2f * s));
                        a = Mathf.Max(a, vBody * vTopBot);
                    }

                    // Rim ellipse at the top of the body.
                    a = Mathf.Max(a, Ellipse(fx, fy, cx, bodyTop, 40f * s, 8f * s, 1.2f));

                    // Handle: an annulus on the right side of the body.
                    float hx = cx + 42f * s, hy = 72f * s;
                    float dh = Mathf.Sqrt((fx - hx) * (fx - hx) + (fy - hy) * (fy - hy));
                    float ring = 1f - Mathf.Clamp01((Mathf.Abs(dh - 20f * s) - 6f * s) / (2f * s));
                    if (fx > cx) a = Mathf.Max(a, ring);

                    // Steam: two wavy vertical wisps rising from the rim.
                    a = Mathf.Max(a, Steam(fx, fy, cx - 12f * s, 116f * s, 150f * s, s, 0f));
                    a = Mathf.Max(a, Steam(fx, fy, cx + 12f * s, 116f * s, 150f * s, s, Mathf.PI));

                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            return px;
        }

        /// <summary>A coffee bean: an ellipse with a curved central crease.</summary>
        public static Color32[] CoffeeBeanPixels(int size)
        {
            var px = new Color32[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float rx = size * 0.34f, ry = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float body = Ellipse(fx, fy, cx, cy, rx, ry, 1.5f);

                    // S-shaped crease: subtract a thin sinusoidal channel.
                    float nx = (fx - cx) / rx;
                    float creaseX = cx + Mathf.Sin(((fy - cy) / ry) * 1.4f) * rx * 0.35f;
                    float crease = 1f - Mathf.Clamp01((Mathf.Abs(fx - creaseX) - 1.5f) / 2f);
                    if (Mathf.Abs(nx) < 0.95f)
                        body *= 1f - 0.85f * crease;

                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(body) * 255f));
                }
            }
            return px;
        }

        public static Color32[] VerticalGradientPixels(int width, int height)
        {
            var px = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                // Row 0 is the bottom in texture space → transparent; top row opaque.
                byte alpha = (byte)(Mathf.Clamp01(y / (float)(height - 1)) * 255f);
                for (int x = 0; x < width; x++)
                    px[y * width + x] = new Color32(255, 255, 255, alpha);
            }
            return px;
        }

        public static Color32[] RadialGlowPixels(int size)
        {
            var px = new Color32[size * size];
            float c = size * 0.5f;
            float maxD = c;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / maxD;
                    float a = Mathf.Clamp01(1f - d);
                    a *= a; // ease-out for a softer falloff
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            return px;
        }

        // ---- helpers ----

        private static float Ellipse(float fx, float fy, float cx, float cy, float rx, float ry, float feather)
        {
            float nx = (fx - cx) / rx;
            float ny = (fy - cy) / ry;
            float d = Mathf.Sqrt(nx * nx + ny * ny);
            return Mathf.Clamp01((1f - d) * rx / feather);
        }

        private static float Steam(float fx, float fy, float baseX, float yStart, float yEnd, float s, float phase)
        {
            if (fy < yStart || fy > yEnd)
                return 0f;
            float wave = baseX + Mathf.Sin((fy - yStart) * 0.09f / s + phase) * 8f * s;
            float fade = Mathf.Clamp01((yEnd - fy) / (yEnd - yStart)); // thins out as it rises
            float core = 1f - Mathf.Clamp01((Mathf.Abs(fx - wave) - 2.2f * s) / (2f * s));
            return core * fade * 0.7f;
        }

        private static Sprite MakeSprite(Color32[] pixels, int size) => MakeSprite(pixels, size, size);

        private static Sprite MakeSprite(Color32[] pixels, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
