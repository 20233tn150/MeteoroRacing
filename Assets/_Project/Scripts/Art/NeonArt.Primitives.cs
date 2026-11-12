using System.Collections.Generic;
using UnityEngine;

namespace MeteoroRacing
{
    /// <summary>
    /// Fábrica de arte procedural. Genera sprites neón en tiempo de ejecución para que
    /// el proyecto sea jugable sin importar assets externos. Sustituible por arte real.
    /// </summary>
    public static partial class NeonArt
    {
        static Sprite _white, _circle, _ring, _glow, _rounded, _digit5;
        static readonly Dictionary<string, Sprite> Tiles = new Dictionary<string, Sprite>();

        public static Sprite White
        {
            get { if (_white == null) _white = MakeSolid(); return _white; }
        }

        public static Sprite Circle
        {
            get { if (_circle == null) _circle = MakeCircle(); return _circle; }
        }

        public static Sprite Ring
        {
            get { if (_ring == null) _ring = MakeRing(); return _ring; }
        }

        public static Sprite Glow
        {
            get { if (_glow == null) _glow = MakeGlow(); return _glow; }
        }

        public static Sprite Rounded
        {
            get { if (_rounded == null) _rounded = MakeRounded(); return _rounded; }
        }

        public static Sprite Digit5
        {
            get { if (_digit5 == null) _digit5 = MakeDigit5(); return _digit5; }
        }

        // ------------------------------------------------------------------ primitivas

        static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "NeonArt_Runtime"
            };
            return tex;
        }

        static Sprite ToSprite(Texture2D tex)
        {
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width, 0, SpriteMeshType.FullRect);
        }

        static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        static Sprite MakeSolid()
        {
            Texture2D tex = NewTexture(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            return ToSprite(tex);
        }

        static Sprite MakeCircle()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Smooth(0.46f, 0.5f, d);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            return ToSprite(tex);
        }

        static Sprite MakeRing()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = 1f - Smooth(0.46f, 0.5f, d);
                float inner = Smooth(0.34f, 0.38f, d);
                px[y * size + x] = new Color(1f, 1f, 1f, outer * inner);
            }
            tex.SetPixels(px);
            return ToSprite(tex);
        }

        static Sprite MakeGlow()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                float a = Mathf.Pow(1f - d, 2.4f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            return ToSprite(tex);
        }

        static Sprite MakeRounded()
        {
            const int size = 128;
            const float radius = 0.2f;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px0 = Mathf.Abs((x + 0.5f) / size - 0.5f);
                float py0 = Mathf.Abs((y + 0.5f) / size - 0.5f);
                float qx = px0 - (0.5f - radius);
                float qy = py0 - (0.5f - radius);
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                                           Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float a = 1f - Smooth(-0.008f, 0.008f, d);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            return ToSprite(tex);
        }

        static Sprite MakeDigit5()
        {
            // Matriz 5x7 del número 5, escalada x4.
            string[] pattern =
            {
                "11111",
                "10000",
                "10000",
                "11110",
                "00001",
                "00001",
                "11110"
            };
            const int scale = 4;
            int w = 5 * scale, h = 7 * scale;
            Texture2D tex = NewTexture(w, h);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int row = 0; row < 7; row++)
            for (int col = 0; col < 5; col++)
            {
                if (pattern[6 - row][col] != '1') continue;
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                {
                    int x = col * scale + sx;
                    int y = row * scale + sy;
                    px[y * w + x] = Color.white;
                }
            }
            tex.SetPixels(px);
            return ToSprite(tex);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Crea una pieza de sprite con tamaño en unidades de mundo.</summary>
        public static SpriteRenderer AddPart(Transform parent, string name, Sprite sprite, Color color,
            Vector2 size, Vector2 localPos, int sortingOrder, float rotation = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ------------------------------------------------------------------ tiles de fondo

        public static Sprite SpaceTile()
        {
            if (Tiles.TryGetValue("space", out Sprite cached)) return cached;

            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            var rng = new System.Random(7331);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float t = y / (float)(size - 1);
                Color c = Color.Lerp(new Color(0.015f, 0.01f, 0.045f), new Color(0.06f, 0.025f, 0.13f), t);
                px[y * size + x] = c;
            }

            for (int i = 0; i < 46; i++)
            {
                int sx = rng.Next(1, size - 1);
                int sy = rng.Next(1, size - 1);
                float b = 0.35f + (float)rng.NextDouble() * 0.65f;
                Color star = new Color(b, b, Mathf.Min(1f, b + 0.25f), 1f);
                px[sy * size + sx] = star;
                if (i % 4 == 0) px[sy * size + sx + 1] = WithAlpha(star, 0.5f);
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["space"] = sprite;
            return sprite;
        }

        public static Sprite GridTile()
        {
            if (Tiles.TryGetValue("grid", out Sprite cached)) return cached;

            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            Color fine = new Color(0.15f, 0.9f, 1f, 0.08f);
            Color bold = new Color(1f, 0.15f, 0.85f, 0.16f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool v = x % 16 == 0;
                bool h = y % 16 == 0;
                if (v || h) px[y * size + x] = fine;
                if (x % 64 == 0 || y % 64 == 0) px[y * size + x] = bold;
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["grid"] = sprite;
            return sprite;
        }

        public static Sprite RoadTile()
        {
            if (Tiles.TryGetValue("road", out Sprite cached)) return cached;

            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            var rng = new System.Random(99);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = ((float)rng.NextDouble() - 0.5f) * 0.02f;
                float band = (y % 8 == 0) ? 0.012f : 0f;
                float v = 0.028f + n + band;
                px[y * size + x] = new Color(v, v, v * 1.25f, 1f);
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["road"] = sprite;
            return sprite;
        }

        public static Sprite LaneDashTile()
        {
            if (Tiles.TryGetValue("dash", out Sprite cached)) return cached;

            const int size = 256;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            int[] lines = { 51, 102, 154, 205 };
            Color core = new Color(0.65f, 0.98f, 1f, 0.75f);
            Color halo = new Color(0.15f, 0.85f, 1f, 0.12f);

            foreach (int lx in lines)
            for (int y = 0; y < size; y++)
            {
                int phase = y % 64;
                if (phase >= 40) continue;
                for (int dx = -3; dx <= 3; dx++)
                {
                    int x = Mathf.Clamp(lx + dx, 0, size - 1);
                    if (Mathf.Abs(dx) <= 1) px[y * size + x] = core;
                    else px[y * size + x] = halo;
                }
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["dash"] = sprite;
            return sprite;
        }

        public static Sprite RailTile()
        {
            if (Tiles.TryGetValue("rail", out Sprite cached)) return cached;

            const int w = 64, h = 256;
            Texture2D tex = NewTexture(w, h);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            Color core = new Color(1f, 1f, 1f, 1f);
            Color glow = new Color(1f, 1f, 1f, 0.18f);

            for (int y = 0; y < h; y++)
            {
                int phase = y % 64;
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x - w * 0.5f);
                    if (d <= 2f) px[y * w + x] = WithAlpha(core, 0.35f);
                    if (phase < 34 && d <= 5f) px[y * w + x] = WithAlpha(core, phase < 26 ? 0.95f : 0.4f);
                    else if (phase < 34 && d <= 9f) px[y * w + x] = glow;
                }
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["rail"] = sprite;
            return sprite;
        }

        public static Sprite SpeedLinesTile()
        {
            if (Tiles.TryGetValue("speed", out Sprite cached)) return cached;

            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            var rng = new System.Random(4242);
            for (int i = 0; i < 22; i++)
            {
                int x = rng.Next(2, size - 2);
                int y = rng.Next(0, size);
                int len = rng.Next(14, 48);
                float a = 0.12f + (float)rng.NextDouble() * 0.28f;
                Color c = (i % 5 == 0) ? new Color(1f, 0.25f, 0.9f, a) : new Color(0.55f, 0.95f, 1f, a);
                for (int k = 0; k < len; k++)
                {
                    int yy = (y + k) % size;
                    px[yy * size + x] = c;
                    if (k < len / 2 && x + 1 < size) px[yy * size + x + 1] = WithAlpha(c, a * 0.4f);
                }
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["speed"] = sprite;
            return sprite;
        }

        public static Sprite WarningTile()
        {
            if (Tiles.TryGetValue("warning", out Sprite cached)) return cached;

            const int size = 64;
            Texture2D tex = NewTexture(size, size);
            var px = new Color[size * size];

            Color red = new Color(1f, 0.08f, 0.2f, 0.9f);
            Color dark = new Color(0.08f, 0.01f, 0.03f, 0.9f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int band = (x + y) % 24;
                px[y * size + x] = band < 12 ? red : dark;
            }

            tex.SetPixels(px);
            Sprite sprite = ToSprite(tex);
            Tiles["warning"] = sprite;
            return sprite;
        }
    }
}
