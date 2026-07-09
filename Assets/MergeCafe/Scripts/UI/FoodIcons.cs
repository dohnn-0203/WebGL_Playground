using System.Collections.Generic;
using MergeCafe.Data;
using UnityEngine;

namespace MergeCafe.UI
{
    /// <summary>
    /// Procedurally drawn, multi-colour food and appliance icons (license-clean, no
    /// image files). A tiny software rasterizer composites simple primitives; each
    /// item / generator has its own recognizable composition. Sprites are cached.
    /// </summary>
    public static class FoodIcons
    {
        public const int IconSize = 128;

        private static readonly Dictionary<int, Sprite> ItemCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<ItemType, Sprite> GeneratorCache = new Dictionary<ItemType, Sprite>();

        public static Sprite Item(ItemType type, int level)
        {
            int key = (int)type * 100 + level;
            if (!ItemCache.TryGetValue(key, out Sprite sprite))
            {
                sprite = ToSprite(BuildItem(type, level, IconSize), IconSize);
                ItemCache[key] = sprite;
            }
            return sprite;
        }

        public static Sprite Generator(ItemType type)
        {
            if (!GeneratorCache.TryGetValue(type, out Sprite sprite))
            {
                sprite = ToSprite(BuildGenerator(type, IconSize), IconSize);
                GeneratorCache[type] = sprite;
            }
            return sprite;
        }

        // ================= Icon compositions =================

        public static Color32[] BuildItem(ItemType type, int level, int size)
        {
            var c = new IconCanvas(size);
            switch (type)
            {
                case ItemType.Coffee: Coffee(c, level); break;
                case ItemType.Bread: Bread(c, level); break;
                default: Dessert(c, level); break;
            }
            return c.ToPixels();
        }

        public static Color32[] BuildGenerator(ItemType type, int size)
        {
            var c = new IconCanvas(size);
            switch (type)
            {
                case ItemType.Coffee: CoffeeMachine(c); break;
                case ItemType.Bread: Oven(c); break;
                default: Fridge(c); break;
            }
            return c.ToPixels();
        }

        // ---- Coffee family (mug based) ----
        private static readonly Color Saucer = H("E8D5B5");
        private static readonly Color Mug = H("D8C4A8");
        private static readonly Color MugDark = H("B79A76");
        private static readonly Color Brew = H("4E342E");
        private static readonly Color Foam = H("F3E4CE");

        private static void Coffee(IconCanvas c, int level)
        {
            if (level == 1)
            {
                // Roasted beans.
                c.Bean(52, 60, 26, 34, 20f, H("6F4E37"));
                c.Bean(80, 48, 24, 30, -25f, H("5B3A29"));
                c.Bean(66, 82, 20, 26, 10f, H("7B5540"));
                return;
            }

            // Saucer + mug.
            c.Ellipse(64, 26, 46, 11, Saucer);
            c.Ellipse(64, 26, 30, 6, H("D5BE99"));
            Color body = Lerp(Mug, MugDark, (level - 2) / 3f);
            c.RoundedRect(58, 62, 30, 30, 9, body);
            c.Ellipse(58, 90, 30, 8, Brew);                 // coffee surface
            c.Crescent(96, 62, 20, 20, 90, 62, 11, 11, body); // handle

            if (level >= 3) { c.Steam(46, 100, 128); c.Steam(70, 100, 128); }        // americano: steam
            if (level >= 4) { c.Ellipse(58, 90, 22, 6, Foam); c.Heart(58, 90, 9, H("C8926A")); } // latte foam art
            if (level >= 5)
            {
                c.RoundedRect(58, 104, 30, 8, 4, H("C0392B"));   // lid
                c.RoundedRect(70, 118, 4, 16, 2, H("ECEFF1"));   // straw
                c.Star(58, 74, 9, H("F2C14E"));                  // special star
            }
        }

        // ---- Bread family ----
        private static void Bread(IconCanvas c, int level)
        {
            switch (level)
            {
                case 1: // dough
                    c.Ellipse(64, 58, 40, 34, H("F1E2C0"));
                    c.Ellipse(52, 66, 10, 7, H("FBF3DD"));
                    break;
                case 2: // small bun
                    c.Ellipse(64, 60, 42, 30, H("E3A24B"));
                    c.Ellipse(56, 66, 12, 8, H("F0C06A"));       // highlight
                    c.Curve(64, 64, 26, H("C77C2E"));            // score
                    break;
                case 3: // croissant
                    c.Crescent(64, 62, 46, 34, 64, 44, 34, 24, H("E0912F"));
                    c.Curve(50, 60, 12, H("B96C1E"));
                    c.Curve(64, 66, 12, H("B96C1E"));
                    c.Curve(78, 60, 12, H("B96C1E"));
                    break;
                case 4: // sandwich (triangle)
                    c.Triangle(24, 34, 104, 34, 64, 96, H("E8B04B"));  // bread
                    c.Triangle(30, 44, 98, 44, 64, 42, H("7CB342"));   // lettuce
                    c.Triangle(34, 54, 94, 54, 64, 52, H("D7534A"));   // tomato/ham
                    c.Line(24, 34, 104, 34, 5, H("C77C2E"));           // crust
                    break;
                default: // dessert plate
                    c.Ellipse(64, 34, 50, 12, H("ECEFF1"));
                    c.Ellipse(64, 34, 40, 8, H("CFD8DC"));
                    c.RoundedRect(50, 54, 14, 16, 4, H("E3A24B"));   // croissant-ish
                    c.Ellipse(80, 52, 16, 12, H("F48FB1"));         // cake
                    c.Circle(80, 62, 4, H("D7263D"));               // cherry
                    break;
            }
        }

        // ---- Dessert family ----
        private static void Dessert(IconCanvas c, int level)
        {
            switch (level)
            {
                case 1: // cream swirl
                    c.Ellipse(64, 30, 30, 8, H("F8BBD0"));
                    c.Circle(64, 44, 20, H("FFF3E0"));
                    c.Circle(64, 60, 15, H("FFF8F0"));
                    c.Circle(64, 74, 10, H("FFFFFF"));
                    break;
                case 2: // pudding (flan)
                    c.Ellipse(64, 30, 40, 10, H("ECEFF1"));
                    c.Trapezoid(64, 54, 40, 20, 26, H("F2C879"));   // custard
                    c.Ellipse(64, 74, 26, 8, H("E0A85C"));
                    c.Ellipse(64, 76, 20, 6, H("8D5524"));          // caramel top
                    break;
                case 3: // cupcake
                    c.Trapezoid(64, 40, 22, 34, 44, H("C77C2E"));   // wrapper
                    c.Lines(64, 40, 22, 34, 44, H("A9661F"));
                    c.Circle(52, 74, 16, H("F48FB1"));              // frosting
                    c.Circle(64, 82, 17, H("F06292"));
                    c.Circle(76, 74, 16, H("F48FB1"));
                    c.Circle(64, 100, 6, H("D7263D"));              // cherry
                    break;
                case 4: // cake slice
                    c.Triangle(30, 34, 30, 92, 104, 40, H("F3D9A0"));   // sponge
                    c.Line(30, 58, 96, 52, 8, H("F8BBD0"));             // cream layer
                    c.Line(30, 76, 60, 74, 8, H("F8BBD0"));
                    c.Ellipse(46, 92, 16, 8, H("F06292"));             // frosting top
                    c.Circle(46, 98, 6, H("D7263D"));
                    break;
                default: // signature cake
                    c.Ellipse(64, 34, 44, 10, H("ECEFF1"));
                    c.RoundedRect(64, 52, 40, 18, 4, H("F3D9A0"));
                    c.RoundedRect(64, 52, 42, 6, 3, H("F8BBD0"));
                    c.RoundedRect(64, 74, 32, 16, 4, H("AB47BC"));
                    c.Ellipse(64, 86, 32, 8, H("F48FB1"));
                    c.RoundedRect(64, 104, 3, 16, 1, H("FFF3E0"));      // candle
                    c.Circle(64, 122, 4, H("F2C14E"));                 // flame
                    c.Circle(50, 88, 4, H("D7263D"));
                    c.Circle(78, 88, 4, H("D7263D"));
                    break;
            }
        }

        // ---- Generators (appliances) ----
        private static void CoffeeMachine(IconCanvas c)
        {
            c.RoundedRect(64, 70, 40, 44, 8, H("6D8B9E"));
            c.RoundedRect(64, 92, 34, 14, 4, H("34495E"));      // top display
            c.RoundedRect(64, 40, 30, 16, 4, H("2C3E50"));      // spout recess
            c.RoundedRect(64, 52, 8, 10, 2, H("95A5A6"));       // spout
            c.RoundedRect(64, 30, 18, 12, 3, H("ECF0F1"));      // cup
            c.Ellipse(64, 36, 18, 4, H("4E342E"));
            c.Circle(80, 92, 4, H("E74C3C"));                   // button
        }

        private static void Oven(IconCanvas c)
        {
            c.RoundedRect(64, 60, 44, 46, 8, H("C0785A"));
            c.RoundedRect(64, 96, 40, 10, 3, H("8A4B34"));      // control bar
            c.Circle(46, 96, 5, H("2C3E50"));
            c.Circle(64, 96, 5, H("2C3E50"));
            c.Circle(82, 96, 5, H("2C3E50"));
            c.RoundedRect(64, 52, 34, 34, 6, H("8A4B34"));      // door
            c.RoundedRect(64, 52, 24, 20, 4, H("F2C14E"));      // window glow
            c.RoundedRect(64, 70, 30, 5, 2, H("5A2F20"));       // handle
        }

        private static void Fridge(IconCanvas c)
        {
            c.RoundedRect(64, 66, 38, 54, 7, H("BDD8E0"));
            c.Line(44, 66, 84, 66, 3, H("8FB3BE"));            // door split
            c.RoundedRect(50, 84, 5, 18, 2, H("5D7079"));      // upper handle
            c.RoundedRect(50, 52, 5, 18, 2, H("5D7079"));      // lower handle
            c.Snowflake(74, 88, 8, H("7FA8C9"));
        }

        // ================= helpers =================

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

        private static Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        private static Color H(string rgba)
        {
            if (rgba.Length == 8) // includes alpha
            {
                ColorUtility.TryParseHtmlString("#" + rgba, out Color ca);
                return ca;
            }
            return ColorUtility.TryParseHtmlString("#" + rgba, out Color c) ? c : Color.magenta;
        }
    }
}
