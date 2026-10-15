using System.Collections.Generic;
using MixVerse.Game.Model.Kart;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MixVerse.Game.Kart
{
    public sealed class KartStageFactory
    {
        private readonly Color _navy = new Color(0.035f, 0.065f, 0.1f, 0.96f);
        private readonly Color _cyan = new Color(0.23f, 0.94f, 0.88f);
        private readonly Color _gold = new Color(1f, 0.75f, 0.25f);
        private readonly Color _coral = new Color(1f, 0.36f, 0.3f);
        private TMP_FontAsset _font;
        private KartStageView _stage;
        private Sprite _barSprite;
        private KartExplosionFactory _explosions;
        private KartPresentationAssets _assets;
        private KartMaterialLibrary _materials;

        public KartStageView Create(KartRaceSettings settings, Transform parent)
        {
            _assets = Resources.Load<KartPresentationAssets>("KartPresentation");
            _font = _assets != null ? _assets.japaneseFont : TMP_Settings.defaultFontAsset;
            var root = new GameObject("SettaiKartStage");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(10000f, 0f, 10000f);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            _stage = root.AddComponent<KartStageView>();
            _stage.Factory = this;
            _stage.Initialize(settings);
            _materials = new KartMaterialLibrary(_stage.GeneratedAssets);
            _explosions = new KartExplosionFactory(_stage, _font, settings.explosionRadius);
            _barSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _stage.GeneratedAssets.Add(_barSprite);
            BuildPath(settings);
            BuildCourse(settings);
            BuildKarts();
            var camera = new GameObject("RaceCamera", typeof(Camera), typeof(AudioListener), typeof(AudioSource));
            camera.transform.SetParent(root.transform, false);
            _stage.Camera = camera.GetComponent<Camera>();
            _stage.Camera.nearClipPlane = 0.15f;
            _stage.Camera.farClipPlane = 420f;
            _stage.Camera.fieldOfView = 60f;
            var cameraData = _stage.Camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            // 水溜りの画面空間反射がシーンの色と深度を読むため、品質設定に関わらず生成させる
            cameraData.requiresColorOption = CameraOverrideOption.On;
            cameraData.requiresDepthOption = CameraOverrideOption.On;
            _stage.ChaseCamera = new KartChaseCamera(camera.transform);
            _stage.BlowAwayCamera = new KartBlowAwayCamera(camera.transform);
            var atmosphere = root.AddComponent<KartAtmosphereView>();
            var skybox = _assets != null ? _assets.skybox : null;
            atmosphere.Apply(skybox);
            atmosphere.Rain = Rain(camera.transform);
            _stage.Atmosphere = atmosphere;
            _stage.Camera.clearFlags = skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            _stage.Camera.backgroundColor = atmosphere.CityFog;
            _stage.Listener = camera.GetComponent<AudioListener>();
            _stage.Listener.enabled = false;
            _stage.Audio = camera.GetComponent<AudioSource>();
            _stage.Audio.playOnAwake = false;
            _stage.RadioClip = Tone("Radio alert", 620f, 0.35f);
            _stage.AlertClip = Tone("Attack confirmed", 1100f, 0.12f);
            _stage.ExplosionClip = _assets != null ? _assets.explosionSeClip : null;
            _stage.RocketFireClip = _assets != null ? _assets.rocketFireSeClip : null;
            _stage.BlowAwayClip = _assets != null ? _assets.blowAwaySeClip : null;
            _stage.ItemGetClip = _assets != null ? _assets.itemGetSeClip : null;
            _stage.DistanceAlertClip = _assets != null ? _assets.distanceAlertSeClip : null;
            _stage.EngineClip = _assets != null ? _assets.engineSeClip : null;
            _stage.GameOverClip = _assets != null ? _assets.gameOverSeClip : null;
            // 上司との距離警告SE専用のAudioSource。閾値を超えている間ループ再生し続ける
            _stage.DistanceAlertAudio = camera.AddComponent<AudioSource>();
            _stage.DistanceAlertAudio.playOnAwake = false;
            _stage.DistanceAlertAudio.loop = true;
            _stage.DistanceAlertAudio.clip = _stage.DistanceAlertClip;
            // エンジンSE専用のAudioSource。走行中はループ再生し、速度に応じて音量とピッチを変える
            _stage.EngineAudio = camera.AddComponent<AudioSource>();
            _stage.EngineAudio.playOnAwake = false;
            _stage.EngineAudio.loop = true;
            _stage.EngineAudio.clip = _stage.EngineClip;
            _stage.BgmAudio = camera.AddComponent<AudioSource>();
            _stage.BgmAudio.playOnAwake = false;
            _stage.BgmAudio.loop = true;
            _stage.BgmAudio.volume = 0.4f;
            // SEの多重再生でボイス数上限に達してもBGMが仮想化(無音化)されないよう最優先にする
            _stage.BgmAudio.priority = 0;
            _stage.BgmAudio.clip = _assets != null ? _assets.bgmClip : null;
            if (_stage.BgmAudio.clip != null) _stage.BgmAudio.Play();
            _stage.TailgateZone = Shape(root.transform, "Tailgate debug range", PrimitiveType.Cube, Vector3.zero,
                new Vector3(3.2f, 0.025f, settings.tailgateDistance), _coral).transform;
            _stage.TailgateZone.gameObject.SetActive(false);
            BuildHud();
            return _stage;
        }

        private void BuildPath(KartRaceSettings settings)
        {
            var position = Vector3.zero;
            var layout = new KartCourseLayout(settings.courseLength);
            for (var i = 0; i <= Mathf.CeilToInt(settings.courseLength / 5f) + 12; i++)
            {
                var distance = i * 5f;
                position.y = layout.ElevationAt(distance);
                _stage.Path.Add(position);
                var heading = layout.HeadingAt(distance + 2.5f);
                var drop = layout.ElevationAt(distance + 5f) - position.y;
                var horizontalStep = Mathf.Sqrt(25f - drop * drop);
                position += new Vector3(Mathf.Sin(heading), 0f, Mathf.Cos(heading)) * horizontalStep;
            }
        }

        private void BuildCourse(KartRaceSettings settings)
        {
            var jump = new KartJumpRamp(settings);
            _stage.CityRoot = new KartCityBuilder(_stage, settings, jump, _assets, _materials, _font).Build();
            BuildJumpRamp(jump, settings);
            for (var section = 0; section < 6; section++)
            {
                var distance = section * settings.courseLength / 6f;
                var words = new[] { "SUMMIT / 山頂", "峠の下り", "社長・取引先 観戦中", "DOWNHILL SPIRAL", "DOWNHILL HAIRPIN", "麓へ / FINAL" };
                Gate(distance, words[section], section == 2 ? _coral : _cyan, settings);
            }
            Gate(settings.courseLength, "FINISH  /  上司に花を", _gold, settings);
            var finishWidth = settings.roadHalfWidth * 2f;
            var finishTileCount = Mathf.CeilToInt(finishWidth);
            var finishTileWidth = finishWidth / finishTileCount;
            for (var i = 0; i < finishTileCount; i++)
                RoadBox("Finish check", _stage.Point(settings.courseLength, -settings.roadHalfWidth + (i + 0.5f) * finishTileWidth) + Vector3.up * 0.02f, new Vector3(finishTileWidth * 0.98f, 0.05f, 1.5f), _stage.DirectionAt(settings.courseLength), i % 2 == 0 ? Color.white : Color.black);
        }

        // マリオカートのダッシュ板付きジャンプ台。斜面の上に光る矢印を並べ、踏み切り側に土台の壁を立てて楔形に見せる
        private void BuildJumpRamp(KartJumpRamp jump, KartRaceSettings settings)
        {
            var width = settings.roadHalfWidth * 2f - 0.4f;
            var slope = Mathf.Atan2(jump.Height, jump.Length) * Mathf.Rad2Deg;
            var surfaceLength = Mathf.Sqrt(jump.Length * jump.Length + jump.Height * jump.Height);
            var middle = (jump.Start + jump.Takeoff) * 0.5f;
            var root = new GameObject("Jump ramp").transform;
            root.SetParent(_stage.transform, false);
            _stage.Segments.Add(new KeyValuePair<float, GameObject>(middle, root.gameObject));
            var deck = new GameObject("Ramp deck").transform;
            deck.SetParent(root, false);
            deck.localPosition = _stage.Point(middle) + Vector3.up * (jump.Height * 0.5f);
            deck.localRotation = _stage.DirectionAt(middle) * Quaternion.Euler(-slope, 0f, 0f);
            var body = _materials.Lit(new Color(0.12f, 0.13f, 0.16f), 0.55f, 0.6f);
            var arrow = new Color(3f, 1.7f, 0.25f);
            var rail = new Color(0.2f, 2.4f, 2.8f);
            Shape(deck, "Deck", PrimitiveType.Cube, Vector3.down * 0.15f, new Vector3(width, 0.3f, surfaceLength), body);
            for (var side = -1; side <= 1; side += 2)
                Shape(deck, "Deck rail", PrimitiveType.Cube, new Vector3(side * width * 0.5f, 0.05f, 0f), new Vector3(0.18f, 0.18f, surfaceLength), rail);
            Shape(deck, "Lip stripe", PrimitiveType.Cube, new Vector3(0f, 0.02f, surfaceLength * 0.5f - 0.25f), new Vector3(width, 0.04f, 0.35f), arrow);
            for (var row = 0; row < 3; row++)
            {
                var z = -surfaceLength * 0.5f + 1.6f + row * 2f;
                for (var column = -1; column <= 1; column++)
                {
                    for (var side = -1; side <= 1; side += 2)
                    {
                        var chevron = Shape(deck, "Dash arrow", PrimitiveType.Cube, new Vector3(column * 3.5f + side * 0.6f, 0.02f, z - 0.35f),
                            new Vector3(1.4f, 0.03f, 0.3f), arrow);
                        chevron.transform.localRotation = Quaternion.Euler(0f, side * 30f, 0f);
                    }
                }
            }
            var lip = Shape(root, "Ramp lip wall", PrimitiveType.Cube, _stage.Point(jump.Takeoff - 0.15f) + Vector3.up * (jump.Height * 0.5f),
                new Vector3(width, jump.Height, 0.3f), body);
            lip.transform.localRotation = _stage.DirectionAt(jump.Takeoff);
            RoadBox("Landing stripe", _stage.Point(jump.Landing + 0.4f) + Vector3.up * 0.03f, new Vector3(width, 0.04f, 0.5f), _stage.DirectionAt(jump.Landing), arrow);
        }

        private ParticleSystem Rain(Transform camera)
        {
            var obj = new GameObject("Rain");
            obj.transform.SetParent(camera, false);
            obj.transform.localPosition = new Vector3(0f, 9f, 16f);
            var rain = obj.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.032f);
            main.startColor = new Color(0.6f, 0.72f, 1f, 0.28f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2500;
            var emission = rain.emission;
            emission.rateOverTime = 2600f;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 1f, 44f);
            var velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(1.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(-30f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _explosions.Glow;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0.035f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            rain.Play();
            return rain;
        }

        private void Gate(float distance, string title, Color color, KartRaceSettings settings)
        {
            var root = new GameObject("Gate " + title).transform;
            root.SetParent(_stage.transform, false);
            root.localPosition = _stage.Point(distance);
            root.localRotation = _stage.DirectionAt(distance);
            _stage.Gates.Add(new KeyValuePair<float, GameObject>(distance, root.gameObject));
            for (var side = -1; side <= 1; side += 2)
                Shape(root, "Gate post", PrimitiveType.Cube, new Vector3(side * (settings.roadHalfWidth + 0.6f), 3.5f, 0f),
                    new Vector3(0.3f, 7f, 0.3f), color);
            Shape(root, "Gate banner", PrimitiveType.Cube, Vector3.up * 6.5f,
                new Vector3(settings.roadHalfWidth * 2f + 1.5f, 1.6f, 0.3f), _navy);
            var label = WorldLabel(root, title, Color.white, 7f);
            label.transform.localPosition = new Vector3(0f, 6.5f, -0.2f);
            label.rectTransform.sizeDelta = new Vector2(13f, 2f);
        }

        private void BuildKarts()
        {
            _stage.Karts = new Transform[3];
            _stage.Tags = new Transform[3];
            _stage.TagLabels = new TextMeshPro[3];
            _stage.Runners = new KartRunnerView[3];
            _stage.BodyRenderers = new MeshRenderer[3][];
            _stage.BodyMaterials = new Material[3][];
            _stage.RainbowMaterial = _materials.Rainbow();
            _stage.StarAuras = new ParticleSystem[3];
            _stage.StarLights = new Light[3];
            _stage.RainbowRings = new MeshRenderer[3];
            _stage.Sparkles = new ParticleSystem[3];
            _stage.Stars = new ParticleSystem[3];
            var ringMesh = RingMesh(1.3f, 1.5f, 0.1f, 28);
            _stage.GeneratedAssets.Add(ringMesh);
            var starMesh = StarMesh();
            _stage.GeneratedAssets.Add(starMesh);
            var colors = new[] { _cyan, _gold, _coral };
            var runners = new KartRunnerFactory(_materials);
            for (var i = 0; i < 3; i++)
            {
                var kart = new GameObject("Kart " + (RacerId)i).transform;
                kart.SetParent(_stage.transform, false);
                _stage.Karts[i] = kart;
                _stage.Runners[i] = runners.Build(kart, colors[i]);
                Headlamp(kart);
                var bodyRenderers = kart.GetComponentsInChildren<MeshRenderer>();
                _stage.BodyRenderers[i] = bodyRenderers;
                var bodyMaterials = new Material[bodyRenderers.Length];
                for (var r = 0; r < bodyRenderers.Length; r++) bodyMaterials[r] = bodyRenderers[r].sharedMaterial;
                _stage.BodyMaterials[i] = bodyMaterials;
                _stage.StarAuras[i] = StarAuraEmitter(kart);
                _stage.StarLights[i] = StarLight(kart);
                _stage.RainbowRings[i] = RainbowRing(kart, ringMesh);
                _stage.Sparkles[i] = SparkleEmitter(kart);
                _stage.Stars[i] = StarBurstEmitter(kart, starMesh);
                if (i == (int)RacerId.Player)
                    _stage.DriftSparks = new[] { DriftSparkEmitter(kart, -1f), DriftSparkEmitter(kart, 1f) };
                var label = WorldLabel(_stage.transform, "", colors[i], 7f);
                _stage.Tags[i] = label.transform;
                _stage.TagLabels[i] = label;
            }
        }

        private void Headlamp(Transform kart)
        {
            var light = new GameObject("Headlamp", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(kart, false);
            light.transform.localPosition = new Vector3(0f, 0.45f, 1.3f);
            light.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 14f;
            light.range = 32f;
            light.spotAngle = 70f;
            light.shadows = LightShadows.None;
        }

        private Light StarLight(Transform kart)
        {
            var light = new GameObject("Star light", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(kart, false);
            light.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            return light;
        }

        // 昇給辞令の無敵中にカートの周りを渦巻くように舞う虹色の光の粒。色相はライフタイム全体でループさせている
        private ParticleSystem StarAuraEmitter(Transform parent)
        {
            var obj = new GameObject("Star aura");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 140;
            var emission = particles.emission;
            emission.enabled = false;
            emission.rateOverTime = 55f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.5f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.arc = 360f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(220f);
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(1.1f, 2.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.2f),
                new GradientColorKey(Color.green, 0.4f), new GradientColorKey(Color.cyan, 0.6f),
                new GradientColorKey(Color.blue, 0.8f), new GradientColorKey(Color.magenta, 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _explosions.Glow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            return particles;
        }

        // カートを帯状に取り巻くリングメッシュ。虹色シェーダーを貼るとオブジェクト座標に応じて縁全体に色相が流れる
        private Mesh RingMesh(float innerRadius, float outerRadius, float height, int segments)
        {
            var vertices = new Vector3[segments * 4];
            var triangles = new List<int>();
            for (var s = 0; s < segments; s++)
            {
                var angle = s / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var b = s * 4;
                vertices[b + 0] = dir * innerRadius + Vector3.up * height * 0.5f;
                vertices[b + 1] = dir * outerRadius + Vector3.up * height * 0.5f;
                vertices[b + 2] = dir * outerRadius - Vector3.up * height * 0.5f;
                vertices[b + 3] = dir * innerRadius - Vector3.up * height * 0.5f;
            }
            for (var s = 0; s < segments; s++)
            {
                var b = s * 4;
                var n = ((s + 1) % segments) * 4;
                triangles.AddRange(new[] { b + 0, n + 0, n + 1, b + 0, n + 1, b + 1 });
                triangles.AddRange(new[] { b + 1, n + 1, n + 2, b + 1, n + 2, b + 2 });
                triangles.AddRange(new[] { b + 2, n + 2, n + 3, b + 2, n + 3, b + 3 });
                triangles.AddRange(new[] { b + 3, n + 3, n + 0, b + 3, n + 0, b + 0 });
            }
            var mesh = new Mesh { name = "Rainbow ring", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private MeshRenderer RainbowRing(Transform kart, Mesh mesh)
        {
            var obj = new GameObject("Rainbow ring", typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(kart, false);
            obj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _stage.RainbowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        // 昇給辞令の無敵中にカート周辺へ舞うキラキラした瞬き粒子。虹色オーラより短命かつ小粒にして瞬く印象にしている
        private ParticleSystem SparkleEmitter(Transform parent)
        {
            var obj = new GameObject("Star sparkle");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 200;
            var emission = particles.emission;
            emission.enabled = false;
            emission.rateOverTime = 90f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.7f;
            shape.radiusThickness = 0.35f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.95f, 0.6f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _explosions.Glow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            return particles;
        }

        // ☆型の平面メッシュ。中心から外周・内周の頂点を交互に結んだ5角の星形
        private Mesh StarMesh()
        {
            const int points = 5;
            var vertices = new Vector3[points * 2 + 1];
            var triangles = new List<int>();
            vertices[0] = Vector3.zero;
            for (var p = 0; p < points * 2; p++)
            {
                var angle = p / (float)(points * 2) * Mathf.PI * 2f - Mathf.PI * 0.5f;
                var radius = p % 2 == 0 ? 0.5f : 0.2f;
                vertices[p + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }
            for (var p = 0; p < points * 2; p++)
            {
                var a = p + 1;
                var b = (p + 1) % (points * 2) + 1;
                triangles.Add(0); triangles.Add(a); triangles.Add(b);
            }
            var mesh = new Mesh { name = "Star", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // 昇給辞令の無敵中にプレイヤーの周りへ大量に散らばせる☆型パーティクル。虹色シェーダーでキラキラ色が変わりながら舞う
        private ParticleSystem StarBurstEmitter(Transform parent, Mesh starMesh)
        {
            var obj = new GameObject("Star burst");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.4f);
            main.startColor = Color.white;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 260;
            var emission = particles.emission;
            emission.enabled = false;
            emission.rateOverTime = 90f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.9f;
            shape.radiusThickness = 0.5f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(90f);
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = starMesh;
            renderer.sharedMaterial = _stage.RainbowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            return particles;
        }

        // 後輪の接地点から地面との摩擦火花を飛ばす。色は KartStageView がドリフトの溜め具合に応じて毎フレーム切り替える
        private ParticleSystem DriftSparkEmitter(Transform parent, float side)
        {
            var obj = new GameObject("Drift spark");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(side * 0.2f, -0.42f, -0.05f);
            obj.transform.localRotation = Quaternion.LookRotation(new Vector3(side * 0.6f, 0.35f, -1f).normalized, Vector3.up);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.26f);
            main.startColor = Color.white;
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
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _explosions.Glow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.8f;
            renderer.velocityScale = 0.1f;
            particles.Play();
            return particles;
        }

        public Transform CreateObject(KartStageView stage, TrackObject obj)
        {
            if (obj.Kind == TrackObjectKind.Explosion)
            {
                var severe = obj.Item == KartItem.Rocket || obj.Item == KartItem.SalaryOrder;
                var effect = _explosions.Create(severe, obj.Id);
                if (severe)
                {
                    _explosions.AddGroundCracks(effect, obj.Distance, obj.Lane, obj.Id);
                    stage.PlayExplosionSe();
                }
                stage.Explosions.Add(obj.Id, effect);
                return effect.transform;
            }
            if (obj.Kind == TrackObjectKind.Rocket)
            {
                stage.PlayRocketFireSe();
            }
            var color = obj.Kind == TrackObjectKind.Crate ? new Color(0.65f, 0.39f, 0.17f) :
                obj.Kind == TrackObjectKind.Rocket ? _coral : _cyan;
            var scale = obj.Kind == TrackObjectKind.Rocket ? new Vector3(0.35f, 0.35f, 1.4f) : Vector3.one * 1.5f;
            var shape = PrimitiveType.Cube;
            var root = Shape(stage.transform, obj.Kind.ToString(), shape, Vector3.zero, scale, color).transform;
            if (obj.Kind == TrackObjectKind.ItemBox)
            {
                var label = WorldLabel(root, obj.Item == KartItem.Rocket ? "弾" : "給", _navy, 7f);
                label.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            }
            return root;
        }

        public Transform CreateResultRocket()
        {
            var root = new GameObject("Result rocket").transform;
            root.SetParent(_stage.transform, false);
            Shape(root, "Warhead", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.9f), new Vector3(0.55f, 0.55f, 0.9f), _coral);
            Shape(root, "Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.5f, 0.85f, 0.5f), _navy)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (var side = -1; side <= 1; side += 2)
                Shape(root, "Fin", PrimitiveType.Cube, new Vector3(side * 0.35f, 0f, -0.65f), new Vector3(0.45f, 0.1f, 0.65f), _gold);
            return root;
        }

        public KartExplosionView CreateResultExplosion(int id, Vector3 position)
        {
            var effect = _explosions.Create(true, id, false);
            effect.transform.localPosition = position;
            return effect;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("Kart HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(_stage.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            var hudRoot = new GameObject("Race HUD", typeof(RectTransform));
            hudRoot.transform.SetParent(canvasObject.transform, false);
            var hudRect = (RectTransform)hudRoot.transform;
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
            _stage.RaceHud = hudRoot;
            var parent = hudRoot.transform;
            var rankingPanel = Panel(parent, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(270f, 218f), _navy);
            var rankingTitle = Label(parent, "SETTAI / KART", new Vector2(0f, 1f), new Vector2(42f, -38f), new Vector2(240f, 32f), 23, _cyan);
            _stage.Ranking = Label(parent, "", new Vector2(0f, 1f), new Vector2(42f, -87f), new Vector2(240f, 142f), 24, Color.white);
            rankingPanel.gameObject.SetActive(false);
            rankingTitle.gameObject.SetActive(false);
            _stage.Ranking.gameObject.SetActive(false);
            Panel(parent, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(260f, 105f), _navy);
            _stage.Section = Label(parent, "", new Vector2(1f, 1f), new Vector2(-42f, -40f), new Vector2(225f, 75f), 24, Color.white);
            Panel(parent, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 95f), _navy);
            _stage.Warning = Label(parent, "", new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(665f, 73f), 22, Color.white, TextAlignmentOptions.Center);
            _stage.Radio = Label(parent, "", new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(850f, 100f), 27, Color.white, TextAlignmentOptions.Center);
            Panel(parent, new Vector2(0f, 0f), new Vector2(24f, 82f), new Vector2(295f, 155f), _navy);
            _stage.Speed = Label(parent, "", new Vector2(0f, 0f), new Vector2(42f, 111f), new Vector2(260f, 120f), 24, _cyan);
            _stage.Gain = Bar(parent, new Vector2(0f, 0f), new Vector2(42f, 98f), new Vector2(258f, 6f), _cyan);
            var itemPanel = Panel(parent, new Vector2(1f, 1f), new Vector2(-24f, -150f), new Vector2(330f, 130f), _navy);
            var iconFrame = Panel(itemPanel.transform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(82f, 82f), _gold);
            Panel(iconFrame.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76f, 76f), new Color(0.1f, 0.18f, 0.23f));
            BuildItemIcons(iconFrame.transform);
            _stage.Item = Label(itemPanel.transform, "", new Vector2(0f, 0.5f), new Vector2(108f, 0f), new Vector2(210f, 92f), 19, _gold);
            var driftPanel = Panel(parent, new Vector2(1f, 0f), new Vector2(-24f, 82f), new Vector2(330f, 90f), _navy);
            _stage.Drift = Label(parent, "", new Vector2(1f, 0f), new Vector2(-42f, 102f), new Vector2(292f, 30f), 20, _cyan);
            _stage.DriftFill = Bar(parent, new Vector2(1f, 0f), new Vector2(-42f, 96f), new Vector2(292f, 5f), _cyan);
            driftPanel.gameObject.SetActive(false);
            _stage.Drift.gameObject.SetActive(false);
            _stage.DriftFill.gameObject.SetActive(false);
            _stage.Progress = Bar(parent, new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(1552f, 5f), _gold);
            _stage.Controls = Label(parent, "", new Vector2(0.5f, 0f), new Vector2(0f, 17f), new Vector2(1560f, 35f), 19, Color.white, TextAlignmentOptions.Center);
            var modal = Panel(canvasObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 650f), new Color(0.035f, 0.065f, 0.1f, 0.94f));
            _stage.Modal = modal.gameObject;
            _stage.ModalBackground = modal;
            _stage.ModalTitle = Label(modal.transform, "", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 90f), 50, _gold, TextAlignmentOptions.Center);
            _stage.ModalBody = Label(modal.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(1000f, 370f), 25, Color.white, TextAlignmentOptions.Center);
            _stage.ActionButton = Button(modal.transform, new Vector2(-145f, 45f), new Vector2(610f, 60f), _cyan, out var action);
            _stage.ActionLabel = action;
            _stage.ExitButton = Button(modal.transform, new Vector2(335f, 45f), new Vector2(235f, 60f), new Color(0.4f, 0.5f, 0.6f), out var exit);
            exit.text = "ホームへ [ ESC ]";
            _stage.Windshield = BuildWindshieldRain(canvasObject.transform);
        }

        // フロントガラスの水滴演出。HUD の一番手前に敷いて、車内から画面全体を見ているように見せる
        private KartWindshieldRainView BuildWindshieldRain(Transform canvasParent)
        {
            var obj = new GameObject("Windshield Rain", typeof(RectTransform), typeof(RawImage));
            obj.transform.SetParent(canvasParent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            var material = new Material(Resources.Load<Shader>("RainWindshieldShaderURP"));
            _stage.GeneratedAssets.Add(material);
            image.material = material;
            var view = obj.AddComponent<KartWindshieldRainView>();
            view.Image = image;
            view.Material = material;
            return view;
        }

        private void BuildItemIcons(Transform parent)
        {
            _stage.ItemIcons = new GameObject[3];
            _stage.EmptyItemIcon = Label(parent, "—", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(70f, 70f), 46, new Color(0.45f, 0.58f, 0.61f), TextAlignmentOptions.Center).gameObject;

            var rocket = IconGroup(parent, "Rocket icon");
            _stage.ItemIcons[(int)KartItem.Rocket] = rocket.gameObject;
            IconShape(rocket, new Vector2(-12f, -14f), new Vector2(15f, 17f), _coral, 35f);
            IconShape(rocket, new Vector2(12f, -14f), new Vector2(15f, 17f), _coral, -35f);
            IconShape(rocket, new Vector2(0f, -27f), new Vector2(11f, 14f), _gold);
            IconShape(rocket, new Vector2(0f, 0f), new Vector2(21f, 43f), Color.white);
            IconShape(rocket, new Vector2(0f, 22f), new Vector2(17f, 17f), _coral, 45f);
            IconShape(rocket, new Vector2(0f, 2f), new Vector2(9f, 9f), _cyan, 45f);

            var salary = IconGroup(parent, "Salary order icon");
            _stage.ItemIcons[(int)KartItem.SalaryOrder] = salary.gameObject;
            IconShape(salary, Vector2.zero, new Vector2(54f, 66f), new Color(1f, 0.94f, 0.67f), -5f);
            Label(salary, "昇給\n辞令", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 58f), 20, _coral, TextAlignmentOptions.Center);
        }

        private RectTransform IconGroup(Transform parent, string name)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            var rect = (RectTransform)group.transform;
            Rect(rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76f, 76f));
            return rect;
        }

        private Image IconShape(Transform parent, Vector2 position, Vector2 size, Color color, float angle = 0f)
        {
            var image = Panel(parent, new Vector2(0.5f, 0.5f), position, size, color);
            image.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return image;
        }

        private Button Button(Transform parent, Vector2 position, Vector2 size, Color color, out TextMeshProUGUI label)
        {
            var panel = Panel(parent, new Vector2(0.5f, 0f), position, size, color);
            panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            label = Label(panel.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, size, 23, _navy, TextAlignmentOptions.Center);
            return button;
        }
        private Image Bar(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Panel(parent, anchor, position, size, new Color(0.14f, 0.24f, 0.28f));
            var image = Panel(parent, anchor, position, size, color);
            image.sprite = _barSprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            return image;
        }
        private Image Panel(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Rect((RectTransform)obj.transform, anchor, position, size);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        private TextMeshProUGUI Label(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            Rect((RectTransform)obj.transform, anchor, position, size);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.font = _font; label.text = text; label.fontSize = fontSize; label.color = color;
            label.alignment = alignment; label.raycastTarget = false;
            return label;
        }
        private void Rect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private TextMeshPro WorldLabel(Transform parent, string text, Color color, float size)
        {
            var obj = new GameObject("Track label", typeof(TextMeshPro));
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<TextMeshPro>();
            label.font = _font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(8f, 2f);
            return label;
        }
        private void RoadBox(string name, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
        {
            Shape(_stage.transform, name, PrimitiveType.Cube, position, scale, color).transform.localRotation = rotation;
        }
        private GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color)
            => Shape(parent, name, primitive, position, scale, _materials.Unlit(color));
        private GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
        private AudioClip Tone(string name, float frequency, float duration)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * duration)];
            for (var i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i / (float)rate * frequency * 2f * Mathf.PI) * (1f - i / (float)samples.Length) * 0.25f;
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); _stage.GeneratedAssets.Add(clip);
            return clip;
        }
    }
}
