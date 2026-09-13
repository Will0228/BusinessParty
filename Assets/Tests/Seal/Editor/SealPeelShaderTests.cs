using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MixVerse.Seal.Tests
{
    public sealed class SealPeelShaderTests
    {
        private Material _material;
        private Texture2D _top;
        private Texture2D _bottom;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("Unlit/SealPeelShaderURP");
            Assert.That(shader, Is.Not.Null);
            _material = new Material(shader);
            _top = CreateTexture(Color.red);
            _bottom = CreateTexture(Color.blue);
            _material.SetTexture("_BottomTex", _bottom);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_top);
            Object.DestroyImmediate(_bottom);
        }

        [TestCase(1f, -1f, 1f)]
        [TestCase(-1f, 1f, 1f)]
        [TestCase(1f, 1f, 1f)]
        [TestCase(-1f, -1f, 1f)]
        [TestCase(1f, 0f, 2.5f)]
        [TestCase(0f, -1f, 0.4f)]
        [TestCase(0f, 0f, 1f)]
        public void EndpointsShowOnlyTheExpectedImage(float x, float y, float aspect)
        {
            _material.SetVector("_PeelDirection", new Vector4(x, y, 0f, 0f));
            _material.SetFloat("_Aspect", aspect);
            AssertInterior(Render(0f), Color.red);
            AssertInterior(Render(1f), Color.blue);
            Assert.That(ShaderUtil.ShaderHasError(_material.shader), Is.False);
        }

        [TestCase(0.01f)]
        [TestCase(0.075f)]
        [TestCase(0.25f)]
        public void DetachedSheetLeavesBeforeTheFinalFrame(float radius)
        {
            _material.SetFloat("_BackWidth", radius);
            var before = Render(0.999f);
            var after = Render(1f);
            for (var i = 0; i < before.Length; i++)
            {
                Assert.That(Vector4.Distance(before[i], after[i]), Is.LessThan(0.01f), $"Pixel {i}");
            }
        }

        [Test]
        public void MidPeelContainsBothImagesAndAGrayBack()
        {
            var pixels = Render(0.3f);
            Assert.That(pixels.Count(c => c.r > 0.9f && c.b < 0.1f), Is.GreaterThan(100));
            Assert.That(pixels.Count(c => c.b > 0.9f && c.r < 0.1f), Is.GreaterThan(100));
            Assert.That(pixels.Count(c => c.a > 0.9f && c.r > 0.2f && c.r < 0.8f
                && Mathf.Abs(c.r - c.g) < 0.01f && Mathf.Abs(c.r - c.b) < 0.01f), Is.GreaterThan(100));
        }

        [Test]
        public void TransparentStickerDoesNotProduceAGrayRectangleOrShadow()
        {
            _top.SetPixels(Enumerable.Repeat(Color.clear, 16).ToArray());
            _top.Apply();
            var peeled = Render(0.3f);
            var revealed = Render(1f);
            for (var i = 0; i < peeled.Length; i++)
            {
                Assert.That(Vector4.Distance(peeled[i], revealed[i]), Is.LessThan(0.01f), $"Pixel {i}");
            }
        }

        private Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(color, 16).ToArray());
            texture.Apply();
            return texture;
        }

        private Color[] Render(float progress)
        {
            const int size = 128;
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            try
            {
                _material.SetFloat("_Progress", progress);
                RenderTexture.active = target;
                GL.Clear(true, true, Color.clear);
                Graphics.Blit(_top, target, _material);
                texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                texture.Apply();
                return texture.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(texture);
            }
        }

        private void AssertInterior(Color[] pixels, Color expected)
        {
            for (var y = 30; y < 98; y++)
            for (var x = 30; x < 98; x++)
            {
                Assert.That(Vector4.Distance(pixels[y * 128 + x], expected), Is.LessThan(0.01f), $"Pixel ({x}, {y})");
            }
            Assert.That(pixels[0].a, Is.LessThan(0.01f));
        }
    }
}
