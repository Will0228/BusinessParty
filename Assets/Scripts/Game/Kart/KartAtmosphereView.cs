using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MixVerse.Game.Kart
{
    public sealed class KartAtmosphereView : MonoBehaviour
    {
        private const float CityFogDensity = 0.0105f;
        private const float TunnelFogDensity = 0.02f;
        public ParticleSystem Rain;
        private readonly Color _cityFog = new Color(0.075f, 0.055f, 0.13f);
        private readonly Color _tunnelFog = new Color(0.02f, 0.025f, 0.035f);
        private bool _fog;
        private FogMode _fogMode;
        private Color _fogColor;
        private float _fogDensity;
        private AmbientMode _ambientMode;
        private Color _ambientLight;
        private Material _skybox;
        private DefaultReflectionMode _reflectionMode;
        private Texture _reflection;
        private float _reflectionIntensity;
        private Cubemap _fallbackReflection;
        private VolumeProfile _profile;
        private bool _captured;
        private bool _inTunnel;

        public Color CityFog => _cityFog;

        public void Apply(Material skybox)
        {
            _fog = RenderSettings.fog;
            _fogMode = RenderSettings.fogMode;
            _fogColor = RenderSettings.fogColor;
            _fogDensity = RenderSettings.fogDensity;
            _ambientMode = RenderSettings.ambientMode;
            _ambientLight = RenderSettings.ambientLight;
            _skybox = RenderSettings.skybox;
            _reflectionMode = RenderSettings.defaultReflectionMode;
            _reflection = RenderSettings.customReflectionTexture;
            _reflectionIntensity = RenderSettings.reflectionIntensity;
            _captured = true;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _cityFog;
            RenderSettings.fogDensity = CityFogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.09f, 0.16f);
            if (skybox != null) RenderSettings.skybox = skybox;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = skybox != null && skybox.HasTexture("_Tex") && skybox.GetTexture("_Tex") is Cubemap sky ? sky : FallbackReflection();
            RenderSettings.reflectionIntensity = 0.8f;
            BuildPostProcessing();
        }

        public void SetTunnel(bool inTunnel)
        {
            if (_inTunnel == inTunnel) return;
            _inTunnel = inTunnel;
            RenderSettings.fogColor = inTunnel ? _tunnelFog : _cityFog;
            RenderSettings.fogDensity = inTunnel ? TunnelFogDensity : CityFogDensity;
            if (Rain == null) return;
            var emission = Rain.emission;
            emission.enabled = !inTunnel;
        }

        private Cubemap FallbackReflection()
        {
            _fallbackReflection = new Cubemap(8, TextureFormat.RGBA32, false);
            var pixels = new Color[8 * 8];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = _cityFog;
            for (var face = 0; face < 6; face++) _fallbackReflection.SetPixels(pixels, (CubemapFace)face);
            _fallbackReflection.Apply();
            return _fallbackReflection;
        }

        private void BuildPostProcessing()
        {
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = _profile.Add<Bloom>(true);
            bloom.threshold.value = 0.85f;
            bloom.intensity.value = 1.35f;
            bloom.scatter.value = 0.72f;
            _profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;
            var color = _profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.3f;
            color.contrast.value = 16f;
            color.saturation.value = 12f;
            color.colorFilter.value = Color.white;
            color.hueShift.value = 0f;
            var toning = _profile.Add<SplitToning>(true);
            toning.shadows.value = new Color(0.25f, 0.55f, 0.75f);
            toning.highlights.value = new Color(0.95f, 0.45f, 0.7f);
            toning.balance.value = -15f;
            var vignette = _profile.Add<Vignette>(true);
            vignette.intensity.value = 0.32f;
            vignette.smoothness.value = 0.45f;
            vignette.color.value = Color.black;
            vignette.center.value = new Vector2(0.5f, 0.5f);
            vignette.rounded.value = false;
            _profile.Add<ChromaticAberration>(true).intensity.value = 0.12f;
            var blur = _profile.Add<MotionBlur>(true);
            blur.intensity.value = 0.22f;
            blur.quality.value = MotionBlurQuality.Medium;
            blur.clamp.value = 0.05f;
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.sharedProfile = _profile;
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
            if (_fallbackReflection != null) Destroy(_fallbackReflection);
            if (!_captured) return;
            RenderSettings.fog = _fog;
            RenderSettings.fogMode = _fogMode;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogDensity = _fogDensity;
            RenderSettings.ambientMode = _ambientMode;
            RenderSettings.ambientLight = _ambientLight;
            RenderSettings.skybox = _skybox;
            RenderSettings.defaultReflectionMode = _reflectionMode;
            RenderSettings.customReflectionTexture = _reflection;
            RenderSettings.reflectionIntensity = _reflectionIntensity;
        }
    }
}
