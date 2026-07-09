using UnityEngine;

namespace MergeCafe.UI
{
    /// <summary>
    /// Tiny software rasterizer used by <see cref="FoodIcons"/> to composite simple
    /// anti-aliased primitives into an RGBA buffer. Coordinates are authored in a
    /// 128px, y-up space and scaled to the actual canvas size.
    /// </summary>
    public sealed class IconCanvas
    {
        private readonly int _size;
        private readonly float _s;      // scale from 128-space to pixels
        private readonly Color[] _px;

        public IconCanvas(int size)
        {
            _size = size;
            _s = size / 128f;
            _px = new Color[size * size];
        }

        public Color32[] ToPixels()
        {
            var outp = new Color32[_px.Length];
            for (int i = 0; i < _px.Length; i++)
            {
                Color c = _px[i];
                outp[i] = new Color32(
                    (byte)(Mathf.Clamp01(c.r) * 255f),
                    (byte)(Mathf.Clamp01(c.g) * 255f),
                    (byte)(Mathf.Clamp01(c.b) * 255f),
                    (byte)(Mathf.Clamp01(c.a) * 255f));
            }
            return outp;
        }

        // ---- core blend ----
        private void Blend(int x, int y, Color color, float cov)
        {
            if (x < 0 || y < 0 || x >= _size || y >= _size)
                return;
            cov *= color.a;
            if (cov <= 0f)
                return;
            if (cov > 1f) cov = 1f;

            int i = y * _size + x;
            Color dst = _px[i];
            float outA = cov + dst.a * (1f - cov);
            if (outA <= 0.0001f)
            {
                _px[i] = new Color(0, 0, 0, 0);
                return;
            }
            float inv = 1f / outA;
            _px[i] = new Color(
                (color.r * cov + dst.r * dst.a * (1f - cov)) * inv,
                (color.g * cov + dst.g * dst.a * (1f - cov)) * inv,
                (color.b * cov + dst.b * dst.a * (1f - cov)) * inv,
                outA);
        }

        // ---- primitives (128-space coords) ----

        public void Ellipse(float cx, float cy, float rx, float ry, Color color)
        {
            cx *= _s; cy *= _s; rx *= _s; ry *= _s;
            float feather = Mathf.Min(rx, ry);
            int x0 = Mathf.FloorToInt(cx - rx - 1), x1 = Mathf.CeilToInt(cx + rx + 1);
            int y0 = Mathf.FloorToInt(cy - ry - 1), y1 = Mathf.CeilToInt(cy + ry + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float nx = (x + 0.5f - cx) / rx;
                    float ny = (y + 0.5f - cy) / ry;
                    float nd = Mathf.Sqrt(nx * nx + ny * ny);
                    Blend(x, y, color, Mathf.Clamp01((1f - nd) * feather));
                }
            }
        }

        public void Circle(float cx, float cy, float r, Color color) => Ellipse(cx, cy, r, r, color);

        public void RoundedRect(float cx, float cy, float halfW, float halfH, float radius, Color color)
        {
            cx *= _s; cy *= _s; halfW *= _s; halfH *= _s; radius *= _s;
            radius = Mathf.Min(radius, Mathf.Min(halfW, halfH));
            int x0 = Mathf.FloorToInt(cx - halfW - 1), x1 = Mathf.CeilToInt(cx + halfW + 1);
            int y0 = Mathf.FloorToInt(cy - halfH - 1), y1 = Mathf.CeilToInt(cy + halfH + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx) - (halfW - radius);
                    float dy = Mathf.Abs(y + 0.5f - cy) - (halfH - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) +
                                               Mathf.Max(dy, 0) * Mathf.Max(dy, 0));
                    float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                    float dist = outside + inside - radius;
                    Blend(x, y, color, Mathf.Clamp01(0.5f - dist));
                }
            }
        }

        public void Trapezoid(float cx, float cy, float halfBottom, float halfTop, float height, Color color)
        {
            cx *= _s; cy *= _s; halfBottom *= _s; halfTop *= _s; height *= _s;
            float bottom = cy - height * 0.5f, top = cy + height * 0.5f;
            float maxHalf = Mathf.Max(halfBottom, halfTop);
            int x0 = Mathf.FloorToInt(cx - maxHalf - 1), x1 = Mathf.CeilToInt(cx + maxHalf + 1);
            int y0 = Mathf.FloorToInt(bottom - 1), y1 = Mathf.CeilToInt(top + 1);
            for (int y = y0; y <= y1; y++)
            {
                float t = Mathf.Clamp01((y + 0.5f - bottom) / height);
                float halfW = Mathf.Lerp(halfBottom, halfTop, t);
                for (int x = x0; x <= x1; x++)
                {
                    float covX = Mathf.Clamp01(halfW - Mathf.Abs(x + 0.5f - cx) + 0.5f);
                    float covY = Mathf.Clamp01(Mathf.Min(y + 0.5f - bottom + 0.5f, top - (y + 0.5f) + 0.5f));
                    Blend(x, y, color, covX * covY);
                }
            }
        }

        public void Triangle(float ax, float ay, float bx, float by, float cx2, float cy2, Color color)
        {
            ax *= _s; ay *= _s; bx *= _s; by *= _s; cx2 *= _s; cy2 *= _s;
            float area = (bx - ax) * (cy2 - ay) - (by - ay) * (cx2 - ax);
            if (area < 0) { float tx = bx, ty = by; bx = cx2; by = cy2; cx2 = tx; cy2 = ty; }

            int x0 = Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx2)) - 1);
            int x1 = Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx2)) + 1);
            int y0 = Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy2)) - 1);
            int y1 = Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy2)) + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float e0 = EdgeDist(ax, ay, bx, by, px, py);
                    float e1 = EdgeDist(bx, by, cx2, cy2, px, py);
                    float e2 = EdgeDist(cx2, cy2, ax, ay, px, py);
                    float cov = Mathf.Clamp01(Mathf.Min(e0, Mathf.Min(e1, e2)) + 0.5f);
                    Blend(x, y, color, cov);
                }
            }
        }

        public void Line(float x0f, float y0f, float x1f, float y1f, float thickness, Color color)
        {
            x0f *= _s; y0f *= _s; x1f *= _s; y1f *= _s; thickness *= _s;
            float half = thickness * 0.5f;
            int bx0 = Mathf.FloorToInt(Mathf.Min(x0f, x1f) - half - 1);
            int bx1 = Mathf.CeilToInt(Mathf.Max(x0f, x1f) + half + 1);
            int by0 = Mathf.FloorToInt(Mathf.Min(y0f, y1f) - half - 1);
            int by1 = Mathf.CeilToInt(Mathf.Max(y0f, y1f) + half + 1);
            for (int y = by0; y <= by1; y++)
            {
                for (int x = bx0; x <= bx1; x++)
                {
                    float d = SegDist(x + 0.5f, y + 0.5f, x0f, y0f, x1f, y1f);
                    Blend(x, y, color, Mathf.Clamp01(half - d + 0.5f));
                }
            }
        }

        public void Crescent(float cx, float cy, float rx, float ry,
            float hcx, float hcy, float hrx, float hry, Color color)
        {
            cx *= _s; cy *= _s; rx *= _s; ry *= _s;
            hcx *= _s; hcy *= _s; hrx *= _s; hry *= _s;
            float feather = Mathf.Min(rx, ry);
            float hf = Mathf.Min(hrx, hry);
            int x0 = Mathf.FloorToInt(cx - rx - 1), x1 = Mathf.CeilToInt(cx + rx + 1);
            int y0 = Mathf.FloorToInt(cy - ry - 1), y1 = Mathf.CeilToInt(cy + ry + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float nd = Mathf.Sqrt(Sq((x + 0.5f - cx) / rx) + Sq((y + 0.5f - cy) / ry));
                    float covOuter = Mathf.Clamp01((1f - nd) * feather);
                    if (covOuter <= 0) continue;
                    float hnd = Mathf.Sqrt(Sq((x + 0.5f - hcx) / hrx) + Sq((y + 0.5f - hcy) / hry));
                    float covHole = Mathf.Clamp01((1f - hnd) * hf);
                    Blend(x, y, color, covOuter * (1f - covHole));
                }
            }
        }

        public void Bean(float cx, float cy, float rx, float ry, float angleDeg, Color color)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            float pcx = cx * _s, pcy = cy * _s, prx = rx * _s, pry = ry * _s;
            float feather = Mathf.Min(prx, pry);
            float maxR = Mathf.Max(prx, pry);
            Color crease = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, color.a);

            int x0 = Mathf.FloorToInt(pcx - maxR - 1), x1 = Mathf.CeilToInt(pcx + maxR + 1);
            int y0 = Mathf.FloorToInt(pcy - maxR - 1), y1 = Mathf.CeilToInt(pcy + maxR + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - pcx, dy = y + 0.5f - pcy;
                    float lx = dx * cos + dy * sin;    // rotate by -angle
                    float ly = -dx * sin + dy * cos;
                    float nd = Mathf.Sqrt(Sq(lx / prx) + Sq(ly / pry));
                    float cov = Mathf.Clamp01((1f - nd) * feather);
                    if (cov <= 0) continue;
                    float creaseX = Mathf.Sin(ly / pry * 1.4f) * prx * 0.35f;
                    float creaseCov = Mathf.Clamp01(1f - (Mathf.Abs(lx - creaseX) - 1f) / 1.5f);
                    Blend(x, y, Color.Lerp(color, crease, creaseCov * 0.9f), cov);
                }
            }
        }

        public void Heart(float cx, float cy, float size, Color color)
        {
            float h = size;
            Circle(cx - h * 0.5f, cy + h * 0.45f, h * 0.55f, color);
            Circle(cx + h * 0.5f, cy + h * 0.45f, h * 0.55f, color);
            Triangle(cx - h, cy + h * 0.5f, cx + h, cy + h * 0.5f, cx, cy - h, color);
        }

        public void Star(float cx, float cy, float r, Color color)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0) ? r : r * 0.42f;
                pts[i] = new Vector2(cx + Mathf.Cos(ang) * rad, cy + Mathf.Sin(ang) * rad);
            }
            FillPolygon(pts, color);
        }

        public void Curve(float cx, float cy, float halfWidth, Color color)
        {
            const int steps = 16;
            float px = 0, py = 0;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps * 2f - 1f;
                float x = cx + t * halfWidth;
                float y = cy - (1f - t * t) * halfWidth * 0.4f;
                if (i > 0)
                    Line(px, py, x, y, 3f, color);
                px = x; py = y;
            }
        }

        public void Steam(float baseX, float yStart, float yEnd)
        {
            Color steam = new Color(1f, 1f, 1f, 0.55f);
            for (float y = yStart; y <= yEnd; y += 2.5f)
            {
                float x = baseX + Mathf.Sin((y - yStart) * 0.16f) * 6f;
                float fade = Mathf.Clamp01((yEnd - y) / (yEnd - yStart));
                Circle(x, y, 2.4f, new Color(steam.r, steam.g, steam.b, steam.a * fade));
            }
        }

        public void Lines(float cx, float cy, float halfBottom, float halfTop, float height, Color color)
        {
            float bottom = cy - height * 0.5f, top = cy + height * 0.5f;
            for (int k = -2; k <= 2; k++)
            {
                float fx = k / 2.5f;
                float xb = cx + fx * halfBottom;
                float xt = cx + fx * halfTop;
                Line(xb, bottom + 2f, xt, top - 2f, 2f, color);
            }
        }

        public void Snowflake(float cx, float cy, float r, Color color)
        {
            for (int i = 0; i < 3; i++)
            {
                float ang = i * Mathf.PI / 3f;
                float dx = Mathf.Cos(ang) * r, dy = Mathf.Sin(ang) * r;
                Line(cx - dx, cy - dy, cx + dx, cy + dy, 2f, color);
            }
        }

        // ---- polygon fill (even-odd, 2x2 supersampled) ----
        public void FillPolygon(Vector2[] pts, Color color)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 p in pts)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            int x0 = Mathf.FloorToInt(minX * _s - 1), x1 = Mathf.CeilToInt(maxX * _s + 1);
            int y0 = Mathf.FloorToInt(minY * _s - 1), y1 = Mathf.CeilToInt(maxY * _s + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float px = (x + 0.25f + sx * 0.5f) / _s;
                            float py = (y + 0.25f + sy * 0.5f) / _s;
                            if (InPolygon(pts, px, py)) hit++;
                        }
                    if (hit > 0)
                        Blend(x, y, color, hit / 4f);
                }
            }
        }

        private static bool InPolygon(Vector2[] pts, float px, float py)
        {
            bool inside = false;
            int n = pts.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((pts[i].y > py) != (pts[j].y > py)) &&
                    (px < (pts[j].x - pts[i].x) * (py - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static float EdgeDist(float ax, float ay, float bx, float by, float px, float py)
        {
            float len = Mathf.Sqrt(Sq(bx - ax) + Sq(by - ay));
            if (len < 0.0001f) return 0f;
            return ((bx - ax) * (py - ay) - (by - ay) * (px - ax)) / len;
        }

        private static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float len2 = dx * dx + dy * dy;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / len2);
            float cx = ax + t * dx, cy = ay + t * dy;
            return Mathf.Sqrt(Sq(px - cx) + Sq(py - cy));
        }

        private static float Sq(float v) => v * v;
    }
}
