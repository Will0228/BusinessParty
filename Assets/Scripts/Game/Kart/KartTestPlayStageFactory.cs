using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MixVerse.Game.Kart
{
    // 操作ガイドのテストプレイ用ステージ。街並みや CPU は置かず、広い平地にプレイヤーのカートだけを置く
    public sealed class KartTestPlayStageFactory
    {
        private const float GroundSize = 4000f;
        private const float GroundTile = 8f;
        private const int GridTextureSize = 128;

        private readonly Color _paint = new Color(0.23f, 0.94f, 0.88f);
        private readonly Color _sky = new Color(0.03f, 0.05f, 0.09f);
        private readonly Color _groundBase = new Color(0.06f, 0.09f, 0.13f);
        private readonly Color _groundLine = new Color(0.14f, 0.5f, 0.58f);

        public KartTestPlayStageView Create()
        {
            var assets = Resources.Load<KartPresentationAssets>("KartPresentation");
            var root = new GameObject("KartTestPlayStage");
            root.transform.position = new Vector3(-10000f, 0f, 10000f);
            var stage = root.AddComponent<KartTestPlayStageView>();
            var materials = new KartMaterialLibrary(stage.GeneratedAssets);

            stage.Ground = BuildGround(stage, materials);
            stage.GroundTile = GroundTile;
            stage.Kart = BuildKart(root.transform, materials);
            var glow = new Material(Resources.Load<Shader>("KartExplosionParticle"));
            glow.SetFloat("_Ring", 0f);
            glow.SetFloat("_DstBlend", (float)BlendMode.One);
            stage.GeneratedAssets.Add(glow);
            stage.DriftSparks = new[] { DriftSpark(stage.Kart, -1f, glow), DriftSpark(stage.Kart, 1f, glow) };
            stage.Sun = BuildSun(root.transform);

            var camera = new GameObject("TestPlayCamera", typeof(Camera));
            camera.transform.SetParent(root.transform, false);
            stage.Camera = camera.GetComponent<Camera>();
            stage.Camera.clearFlags = CameraClearFlags.SolidColor;
            stage.Camera.backgroundColor = _sky;
            stage.Camera.nearClipPlane = 0.15f;
            stage.Camera.farClipPlane = 700f;
            var cameraData = stage.Camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            stage.ChaseCamera = new KartChaseCamera(camera.transform);

            stage.DriftClip = assets != null ? assets.driftSeClip : null;
            stage.DriftAudio = camera.AddComponent<AudioSource>();
            stage.DriftAudio.playOnAwake = false;
            stage.EngineAudio = camera.AddComponent<AudioSource>();
            stage.EngineAudio.playOnAwake = false;
            stage.EngineAudio.loop = true;
            stage.EngineAudio.clip = assets != null ? assets.engineSeClip : null;
            return stage;
        }

        private Transform BuildGround(KartTestPlayStageView stage, KartMaterialLibrary materials)
        {
            var texture = materials.Register(GridTexture());
            var material = materials.Unlit(Color.white);
            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one * (GroundSize / GroundTile);
            // Plane は 10m 四方なので、GroundSize になるよう拡大する
            return Shape(stage.transform, "Ground", PrimitiveType.Plane, Vector3.zero,
                new Vector3(GroundSize / 10f, 1f, GroundSize / 10f), material).transform;
        }

        private Texture2D GridTexture()
        {
            var texture = new Texture2D(GridTextureSize, GridTextureSize, TextureFormat.RGBA32, true)
            {
                name = "Test play grid",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 16,
            };
            var pixels = new Color[GridTextureSize * GridTextureSize];
            for (var y = 0; y < GridTextureSize; y++)
            {
                for (var x = 0; x < GridTextureSize; x++)
                {
                    var line = x < 3 || y < 3;
                    pixels[y * GridTextureSize + x] = line ? _groundLine : _groundBase;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        // 見た目は本番のプレイヤーカート（KartStageFactory.BuildKarts）に合わせている
        private Transform BuildKart(Transform root, KartMaterialLibrary materials)
        {
            var kart = new GameObject("Kart Player").transform;
            kart.SetParent(root, false);
            var paint = materials.Lit(_paint * 0.8f, 0.85f, 0.25f, _paint * 0.06f);
            var trim = materials.Lit(new Color(0.03f, 0.04f, 0.06f), 0.7f, 0.4f);
            var tire = materials.Lit(new Color(0.03f, 0.03f, 0.035f), 0.25f);
            Shape(kart, "Chassis", PrimitiveType.Cube, Vector3.zero, new Vector3(1.6f, 0.45f, 2.5f), paint);
            Shape(kart, "Nose", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.75f), new Vector3(1.25f, 0.4f, 0.9f), paint);
            Shape(kart, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.4f, -0.45f), new Vector3(0.85f, 0.7f, 0.7f), trim);
            Shape(kart, "Driver suit", PrimitiveType.Capsule, new Vector3(0f, 0.75f, -0.1f), new Vector3(0.6f, 0.45f, 0.6f),
                materials.Lit(new Color(0.14f, 0.2f, 0.3f), 0.35f));
            Shape(kart, "Driver helmet", PrimitiveType.Sphere, new Vector3(0f, 1.3f, -0.05f), Vector3.one * 0.65f, paint);
            Shape(kart, "Visor", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.24f), new Vector3(0.5f, 0.18f, 0.12f),
                materials.Lit(new Color(0.02f, 0.03f, 0.05f), 0.95f, 0.6f));
            for (var side = -1; side <= 1; side += 2)
            {
                for (var axle = -1; axle <= 1; axle += 2)
                {
                    var wheel = Shape(kart, "Wheel", PrimitiveType.Cylinder, new Vector3(side * 0.88f, -0.1f, axle * 0.8f),
                        new Vector3(0.65f, 0.18f, 0.65f), tire);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }

                Shape(kart, "Headlight", PrimitiveType.Cube, new Vector3(side * 0.42f, 0.3f, 1.21f),
                    new Vector3(0.34f, 0.14f, 0.05f), materials.Unlit(new Color(4f, 3.8f, 3.4f)));
                Shape(kart, "Tail light", PrimitiveType.Cube, new Vector3(side * 0.55f, 0.12f, -1.26f),
                    new Vector3(0.38f, 0.12f, 0.05f), materials.Unlit(new Color(4f, 0.12f, 0.08f)));
            }

            return kart;
        }

        private Light BuildSun(Transform root)
        {
            var light = new GameObject("Test play sun", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localRotation = Quaternion.Euler(50f, 30f, 0f);
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private ParticleSystem DriftSpark(Transform kart, float side, Material material)
        {
            var obj = new GameObject("Drift spark");
            obj.transform.SetParent(kart, false);
            obj.transform.localPosition = new Vector3(side * 0.88f, -0.4f, -0.8f);
            obj.transform.localRotation = Quaternion.LookRotation(new Vector3(side * 0.6f, 0.35f, -1f).normalized, Vector3.up);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.26f);
            main.gravityModifier = 2.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 180;
            var emission = particles.emission;
            emission.enabled = false;
            emission.rateOverTime = 120f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.18f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.8f;
            renderer.velocityScale = 0.1f;
            particles.Play();
            return particles;
        }

        private GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale,
            Material material)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
    }
}
