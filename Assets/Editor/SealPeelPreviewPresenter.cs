using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MixVerse.EditorTools
{
    internal interface ISealPeelPreviewPresenter : IDisposable
    {
        void Draw(Rect area, Texture imageA, Texture imageB, float progress, Vector2 direction, float radius, float liftAngle);
    }

    internal sealed class SealPeelPreviewPresenter : ISealPeelPreviewPresenter
    {
        private readonly Material _material;
        private readonly Texture2D _sampleA;
        private readonly Texture2D _sampleB;

        public SealPeelPreviewPresenter(SealPeelDemoTextureFactory textureFactory)
        {
            var shader = Shader.Find("Unlit/SealPeelShaderURP");
            if (shader != null)
            {
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            _sampleA = textureFactory.Create(false);
            _sampleB = textureFactory.Create(true);
        }

        public void Draw(Rect area, Texture imageA, Texture imageB, float progress, Vector2 direction, float radius, float liftAngle)
        {
            EditorGUI.DrawRect(area, new Color(0.16f, 0.17f, 0.19f));
            if (_material == null)
            {
                EditorGUI.HelpBox(area, "SealPeelShaderURP が見つかりません。", MessageType.Error);
                return;
            }

            var top = imageA != null ? imageA : _sampleA;
            var bottom = imageB != null ? imageB : _sampleB;
            var aspect = (float)top.width / Mathf.Max(1, top.height);
            var width = Mathf.Min(area.width - 24f, (area.height - 24f) * aspect);
            var height = width / aspect;
            var rect = new Rect(area.center.x - width * 0.5f, area.center.y - height * 0.5f, width, height);
            _material.SetTexture("_BottomTex", bottom);
            _material.SetFloat("_Progress", progress);
            _material.SetFloat("_Aspect", aspect);
            _material.SetVector("_PeelDirection", direction);
            _material.SetFloat("_BackWidth", radius);
            _material.SetFloat("_LiftAngle", liftAngle);
            EditorGUI.DrawPreviewTexture(rect, top, _material, ScaleMode.StretchToFill);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_sampleA);
            Object.DestroyImmediate(_sampleB);
        }
    }
}
