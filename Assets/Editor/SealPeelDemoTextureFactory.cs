using UnityEngine;

namespace MixVerse.EditorTools
{
    internal sealed class SealPeelDemoTextureFactory
    {
        public Texture2D Create(bool revealed)
        {
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = revealed ? "B - Underneath" : "A - Sticker",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var ink = new Color(0.06f, 0.18f, 0.24f);
            var paper = new Color(0.95f, 0.94f, 0.87f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                var color = revealed ? new Color(0.95f, 0.65f, 0.25f) : new Color(0.12f, 0.47f, 0.64f);
                if (uv.x < 0.035f || uv.x > 0.965f || uv.y < 0.035f || uv.y > 0.965f) color = paper;
                if (revealed)
                {
                    var distance = Vector2.Distance(uv, new Vector2(0.68f, 0.56f));
                    if (distance < 0.21f) color = ink;
                    if (distance < 0.15f) color = paper;
                    if (uv.y < 0.34f && uv.y > 0.08f && uv.x > 0.08f && uv.x < 0.92f)
                        color = Mathf.Repeat(uv.x + uv.y, 0.12f) < 0.06f ? ink : paper;
                }
                else
                {
                    if (Vector2.Distance(uv, new Vector2(0.73f, 0.68f)) < 0.14f) color = new Color(1f, 0.72f, 0.3f);
                    if (uv.y < 0.5f - Mathf.Abs(uv.x - 0.4f) * 0.75f && uv.y > 0.08f && uv.x > 0.08f && uv.x < 0.92f) color = ink;
                    if (uv.y < 0.34f - Mathf.Abs(uv.x - 0.7f) * 0.55f && uv.y > 0.08f && uv.x > 0.08f && uv.x < 0.92f) color = new Color(0.27f, 0.69f, 0.66f);
                }

                var letter = (uv - new Vector2(0.11f, 0.64f)) / 0.23f;
                var mark = revealed ? IsB(letter) : IsA(letter);
                if (mark) color = paper;
                if (uv.x > 0.1f && uv.x < 0.48f && uv.y > 0.54f && uv.y < 0.56f) color = paper;
                pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private bool IsA(Vector2 p)
        {
            return Segment(p, new Vector2(0f, 0f), new Vector2(0.4f, 1f), 0.085f)
                || Segment(p, new Vector2(0.4f, 1f), new Vector2(0.8f, 0f), 0.085f)
                || Segment(p, new Vector2(0.15f, 0.35f), new Vector2(0.65f, 0.35f), 0.075f);
        }

        private bool IsB(Vector2 p)
        {
            var lower = Vector2.Distance(p, new Vector2(0.25f, 0.25f));
            var upper = Vector2.Distance(p, new Vector2(0.25f, 0.75f));
            return Segment(p, Vector2.zero, Vector2.up, 0.08f)
                || (p.x >= 0.22f && ((lower > 0.17f && lower < 0.33f) || (upper > 0.17f && upper < 0.33f)))
                || Segment(p, new Vector2(0f, 0.5f), new Vector2(0.25f, 0.5f), 0.08f)
                || Segment(p, Vector2.zero, new Vector2(0.25f, 0f), 0.08f)
                || Segment(p, Vector2.up, new Vector2(0.25f, 1f), 0.08f);
        }

        private bool Segment(Vector2 p, Vector2 a, Vector2 b, float width)
        {
            var edge = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude);
            return Vector2.Distance(p, a + edge * t) < width;
        }
    }
}
