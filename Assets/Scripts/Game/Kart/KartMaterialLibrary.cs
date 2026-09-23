using System.Collections.Generic;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    public sealed class KartMaterialLibrary
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private readonly Dictionary<Color, Material> _unlit = new Dictionary<Color, Material>();
        private readonly Dictionary<(Color, float, float, Color), Material> _lit = new Dictionary<(Color, float, float, Color), Material>();
        private readonly List<Object> _generated;
        private readonly Shader _unlitShader;
        private readonly Shader _litShader;

        public KartMaterialLibrary(List<Object> generated)
        {
            _generated = generated;
            _unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? _unlitShader;
        }

        public Material Unlit(Color color)
        {
            if (_unlit.TryGetValue(color, out var material)) return material;
            material = Register(new Material(_unlitShader) { color = color });
            _unlit.Add(color, material);
            return material;
        }

        public Material Lit(Color color, float smoothness = 0.3f, float metallic = 0f, Color emission = default)
        {
            var key = (color, smoothness, metallic, emission);
            if (_lit.TryGetValue(key, out var material)) return material;
            material = Register(new Material(_litShader));
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetFloat(MetallicId, metallic);
            if (emission.maxColorComponent > 0f) EnableEmission(material, emission);
            _lit.Add(key, material);
            return material;
        }

        public Material LitTextured(Texture2D baseMap, Texture2D emissionMap, Color emission, float smoothness)
        {
            var material = Register(new Material(_litShader));
            material.SetTexture(BaseMapId, baseMap);
            material.SetColor(BaseColorId, Color.white);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetTexture(EmissionMapId, emissionMap);
            EnableEmission(material, emission);
            return material;
        }

        public Texture2D Register(Texture2D texture)
        {
            _generated.Add(texture);
            return texture;
        }

        private Material Register(Material material)
        {
            material.enableInstancing = true;
            _generated.Add(material);
            return material;
        }

        private void EnableEmission(Material material, Color emission)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor(EmissionColorId, emission);
        }
    }
}
