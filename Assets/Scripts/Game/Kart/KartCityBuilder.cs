using System;
using System.Collections.Generic;
using MixVerse.Game.Model.Kart;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MixVerse.Game.Kart
{
    public sealed class KartCityBuilder
    {
        private const float Step = 2.5f;
        private const float SidewalkWidth = 3.6f;
        private const float SidewalkHeight = 0.2f;
        private const float GroundHeight = -0.35f;
        private const float MaxGroundExtent = 90f;
        private const float TunnelHeight = 9f;
        private const float FacadeCellWidth = 3.2f;
        private const float FacadeCellHeight = 3.6f;
        private const int FacadeCells = 32;
        private const float BucketSize = 40f;
        private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
        private readonly KartStageView _stage;
        private readonly KartRaceSettings _settings;
        private readonly KartPresentationAssets _assets;
        private readonly KartMaterialLibrary _materials;
        private readonly TMP_FontAsset _font;
        private readonly Random _random = new Random(1116);
        private readonly Dictionary<GameObject, Bounds> _bounds = new Dictionary<GameObject, Bounds>();
        private readonly Dictionary<Color, Material> _neonText = new Dictionary<Color, Material>();
        private readonly List<Material> _facades = new List<Material>();
        private readonly Dictionary<int, Transform> _buckets = new Dictionary<int, Transform>();
        private readonly Color _neonCyan = new Color(0.15f, 2.6f, 3f);
        private readonly Color _neonMagenta = new Color(3.2f, 0.25f, 2.4f);
        private readonly Color[] _neon =
        {
            new Color(0.2f, 3f, 3.2f), new Color(3.4f, 0.35f, 2.5f), new Color(3.4f, 1.4f, 0.25f),
            new Color(0.6f, 1.1f, 3.8f), new Color(3.6f, 0.4f, 0.45f), new Color(1.6f, 3.4f, 0.6f)
        };
        private readonly string[] _signWords =
        {
            "接待", "残業", "昇進", "決算", "稟議", "忖度", "営業中", "居酒屋", "カラオケ",
            "株式会社", "定時退社", "麻雀", "拉麺", "焼肉", "取引先", "賞与", "薬局", "終電"
        };
        private float[] _leftClearance;
        private float[] _rightClearance;
        private Transform _root;
        private float _tunnelStart;
        private float _tunnelEnd;
        private float _edge;
        private float _end;

        public KartCityBuilder(KartStageView stage, KartRaceSettings settings, KartPresentationAssets assets, KartMaterialLibrary materials, TMP_FontAsset font)
        {
            _stage = stage;
            _settings = settings;
            _assets = assets;
            _materials = materials;
            _font = font;
        }

        public Transform Build()
        {
            _root = new GameObject("Cyberpunk city").transform;
            _root.SetParent(_stage.transform, false);
            _root.gameObject.SetActive(false);
            _tunnelStart = _settings.courseLength / 2f;
            _tunnelEnd = _settings.courseLength * 4f / 6f;
            _edge = _settings.roadHalfWidth + SidewalkWidth + 0.4f;
            _end = _settings.courseLength + 50f;
            _leftClearance = Clearances(-1);
            _rightClearance = Clearances(1);
            CreateFacades();
            BuildRoad();
            BuildGround();
            BuildTunnel();
            for (var side = -1; side <= 1; side += 2)
            {
                BuildFrontage(side);
                BuildSkyline(side);
            }
            BuildTerminalTower();
            BuildStreetLights();
            BuildOverheadCables();
            BuildGallery();
            foreach (var collider in _root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            _root.gameObject.SetActive(true);
            return _root;
        }

        private void BuildRoad()
        {
            var halfWidth = _settings.roadHalfWidth;
            var asphalt = _materials.Lit(new Color(0.05f, 0.052f, 0.06f), 0.82f);
            var paint = _materials.Lit(new Color(0.78f, 0.78f, 0.74f), 0.55f, 0f, new Color(0.12f, 0.12f, 0.11f));
            var amber = _materials.Lit(new Color(0.9f, 0.62f, 0.2f), 0.55f, 0f, new Color(0.3f, 0.18f, 0.04f));
            var curb = _materials.Lit(new Color(0.22f, 0.22f, 0.24f), 0.4f);
            var sidewalk = _assets != null && _assets.pavementMaterial != null ? _assets.pavementMaterial : _materials.Lit(new Color(0.17f, 0.17f, 0.19f), 0.55f);
            Surface("Asphalt", asphalt, 0f, _end, _ => -halfWidth, _ => halfWidth, 0f);
            Surface("Center line", amber, 0f, _end, _ => -0.09f, _ => 0.09f, 0.012f, d => d % 9f < 4.5f);
            for (var side = -1; side <= 1; side += 2)
            {
                var s = side;
                Surface("Edge line", paint, 0f, _end, _ => s * (halfWidth - 0.45f), _ => s * (halfWidth - 0.3f), 0.012f);
                Wall("Curb", curb, 0f, _end, s * (halfWidth + 0.25f), 0f, SidewalkHeight, s);
                Wall("Curb neon", _materials.Unlit(s < 0 ? _neonCyan : _neonMagenta), 0f, _end, s * (halfWidth + 0.24f), 0.07f, 0.12f, s);
                Surface("Sidewalk", sidewalk, 0f, _end, _ => s * (halfWidth + 0.25f), d => s * (halfWidth + (InTunnel(d, 0f) ? 1f : SidewalkWidth)), SidewalkHeight);
            }
        }

        private void BuildGround()
        {
            var ground = _materials.Lit(new Color(0.035f, 0.035f, 0.045f), 0.62f);
            var inner = _settings.roadHalfWidth + SidewalkWidth - 0.05f;
            Surface("Ground", ground, 0f, _end, d => -GroundExtent(d, -1), _ => -inner, GroundHeight, d => !InTunnel(d, 0f));
            Surface("Ground", ground, 0f, _end, _ => inner, d => GroundExtent(d, 1), GroundHeight, d => !InTunnel(d, 0f));
            var lowest = float.MaxValue;
            foreach (var point in _stage.Path) lowest = Mathf.Min(lowest, point.y);
            var middle = _stage.Point(_settings.courseLength * 0.5f);
            middle.y = lowest - 1.2f;
            Primitive("Base ground", PrimitiveType.Quad, _root, middle, new Vector3(4000f, 4000f, 1f), ground)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void BuildTunnel()
        {
            var halfWidth = _settings.roadHalfWidth;
            var wall = _materials.Lit(new Color(0.1f, 0.1f, 0.115f), 0.45f);
            var start = _tunnelStart;
            var end = _tunnelEnd;
            Wall("Tunnel wall", wall, start, end, -(halfWidth + 1f), 0f, TunnelHeight, -1);
            Wall("Tunnel wall", wall, start, end, halfWidth + 1f, 0f, TunnelHeight, 1);
            Surface("Tunnel ceiling", wall, start, end, _ => halfWidth + 1f, _ => -(halfWidth + 1f), TunnelHeight);
            for (var side = -1; side <= 1; side += 2)
            {
                var glow = _materials.Unlit(side < 0 ? _neonCyan : _neonMagenta);
                Wall("Tunnel neon", glow, start, end, side * (halfWidth + 0.98f), 1.05f, 1.2f, side);
                Wall("Tunnel neon", glow, start, end, side * (halfWidth + 0.98f), 7.4f, 7.5f, side);
            }
            var lamp = _materials.Unlit(new Color(2.6f, 2.8f, 3f));
            for (var d = start + 4f; d < end; d += 10f)
            {
                var bucket = Bucket(d);
                var rotation = Quaternion.LookRotation(Forward(d));
                for (var side = -1; side <= 1; side += 2)
                    Primitive("Tunnel lamp", PrimitiveType.Cube, bucket, At(d, side * 2.4f, TunnelHeight - 0.08f), new Vector3(0.35f, 0.08f, 2.6f), lamp).transform.localRotation = rotation;
                if (((int)((d - start) / 10f)) % 3 == 0) PointLight(bucket, At(d, 0f, TunnelHeight - 1.2f), new Color(0.75f, 0.85f, 1f), 3.5f, 18f);
            }
            for (var d = start - 2f; d < end + 2f; d += 10f)
            {
                var along = d + 5f;
                var bucket = Bucket(along);
                var rotation = Quaternion.LookRotation(Forward(along));
                for (var side = -1; side <= 1; side += 2)
                    Block(bucket, At(along, side * (halfWidth + 1.6f + 14f), GroundHeight), rotation, new Vector3(28f, 46f, 10.4f), Facade());
                Block(bucket, At(along, 0f, TunnelHeight + 0.3f), rotation, new Vector3(halfWidth * 2f + 3.4f, 36f, 10.4f), Facade());
            }
        }

        private void BuildFrontage(int side)
        {
            var d = 2f;
            while (d < _end)
            {
                if (InTunnel(d, 12f))
                {
                    d = _tunnelEnd + 12f;
                    continue;
                }
                var used = PlaceBuilding(d, side);
                d += used > 0f ? used + (float)_random.NextDouble() * 1.2f : 3f;
            }
        }

        private float PlaceBuilding(float distance, int side)
        {
            var prefabs = _assets != null ? _assets.buildingPrefabs : null;
            for (var attempt = 0; prefabs != null && prefabs.Length > 0 && attempt < 4; attempt++)
            {
                var prefab = prefabs[_random.Next(prefabs.Length)];
                if (prefab == null) continue;
                var bounds = PrefabBounds(prefab);
                var yaw = _random.Next(4) * 90f;
                var turned = Mathf.Abs(yaw - 90f) < 1f || Mathf.Abs(yaw - 270f) < 1f;
                var length = turned ? bounds.size.x : bounds.size.z;
                var depth = turned ? bounds.size.z : bounds.size.x;
                if (!TryFootprint(distance, side, length, depth, out var rotation, out var center)) continue;
                var instance = Spawn(Bucket(distance), prefab, Vector3.zero, rotation * Quaternion.Euler(0f, yaw, 0f));
                var offset = instance.transform.localRotation * new Vector3(bounds.center.x, 0f, bounds.center.z);
                instance.transform.localPosition = new Vector3(center.x - offset.x, center.y - bounds.min.y - 0.3f, center.z - offset.z);
                AddSign(distance, length, side);
                return length;
            }
            return PlaceBlock(distance, side);
        }

        private float PlaceBlock(float distance, int side)
        {
            var length = 7f + (float)_random.NextDouble() * 9f;
            for (var depth = 14f; depth >= 5f; depth -= 3f)
            {
                if (!TryFootprint(distance, side, length, depth, out var rotation, out var center)) continue;
                var height = 9f + (float)_random.NextDouble() * 28f;
                Block(Bucket(distance), center + Vector3.up * GroundHeight, rotation, new Vector3(depth, height, length), Facade());
                AddSign(distance, length, side);
                return length;
            }
            return 0f;
        }

        private bool TryFootprint(float distance, int side, float length, float depth, out Quaternion rotation, out Vector3 center)
        {
            var along = distance + length * 0.5f;
            var forward = Forward(along);
            var right = Vector3.Cross(Vector3.up, forward);
            rotation = Quaternion.LookRotation(forward, Vector3.up);
            center = _stage.Point(along) + right * side * (_edge + depth * 0.5f);
            for (var x = -1; x <= 1; x++)
                for (var z = -1; z <= 1; z++)
                {
                    var sample = center + right * (x * depth * 0.5f) + forward * (z * length * 0.5f);
                    if (DistanceToRoad(sample) < _edge - 0.3f) return false;
                }
            return true;
        }

        private void BuildSkyline(int side)
        {
            for (var d = 0f; d < _end + 20f; d += 14f + (float)_random.NextDouble() * 12f)
            {
                var width = 14f + (float)_random.NextDouble() * 16f;
                var depth = 14f + (float)_random.NextDouble() * 14f;
                var height = 40f + (float)_random.NextDouble() * 110f;
                var lateral = _edge + 24f + (float)_random.NextDouble() * 50f + depth * 0.5f;
                var forward = Forward(d);
                var right = Vector3.Cross(Vector3.up, forward);
                var center = _stage.Point(d) + right * side * lateral;
                if (DistanceToRoad(center) < _edge + 12f + Mathf.Max(width, depth) * 0.75f) continue;
                var rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, (float)_random.NextDouble() * 24f - 12f, 0f);
                var size = new Vector3(depth, height, width);
                var bucket = Bucket(d);
                Block(bucket, center + Vector3.up * (GroundHeight - 2f), rotation, size, Facade());
                if (_random.NextDouble() < 0.45) AddScreen(bucket, center + Vector3.up * (GroundHeight - 2f), rotation, size, side);
                if (_random.NextDouble() < 0.6)
                    Primitive("Aviation light", PrimitiveType.Cube, bucket, center + Vector3.up * (height + GroundHeight - 1.6f), Vector3.one * 0.8f, _materials.Unlit(new Color(4f, 0.25f, 0.2f)));
            }
        }

        private void BuildTerminalTower()
        {
            var distance = _settings.courseLength + 55f;
            var forward = Forward(distance);
            var center = _stage.Point(distance) + forward * 18f;
            var rotation = Quaternion.LookRotation(forward);
            var size = new Vector3(90f, 140f, 16f);
            var bucket = Bucket(distance);
            Block(bucket, center + Vector3.up * GroundHeight, rotation, size, Facade());
            var screen = ScreenMaterial();
            if (screen == null) return;
            var quad = Primitive("Terminal screen", PrimitiveType.Quad, bucket, center + Vector3.up * 22f - forward * (size.z * 0.5f + 0.3f), new Vector3(30f, 17f, 1f), screen);
            quad.transform.localRotation = rotation;
        }

        private void BuildStreetLights()
        {
            var prefabs = _assets != null ? _assets.streetLightPrefabs : null;
            var index = 0;
            for (var d = 12f; d < _end; d += 26f, index++)
            {
                if (InTunnel(d, 4f)) continue;
                var bucket = Bucket(d);
                var side = index % 2 == 0 ? -1 : 1;
                var position = At(d, side * (_settings.roadHalfWidth + 0.9f), SidewalkHeight);
                var rotation = Quaternion.LookRotation(Forward(d));
                var prefab = prefabs != null && prefabs.Length > 0 ? prefabs[index % prefabs.Length] : null;
                if (prefab != null)
                {
                    var lamp = Spawn(bucket, prefab, position, rotation);
                    foreach (var light in lamp.GetComponentsInChildren<Light>(true))
                    {
                        light.shadows = LightShadows.None;
                        light.range = light.type == LightType.Spot ? 20f : 9f;
                        light.intensity = light.type == LightType.Spot ? 16f : 4f;
                        if (light.type == LightType.Spot) light.spotAngle = 120f;
                    }
                    continue;
                }
                Primitive("Lamp pole", PrimitiveType.Cube, bucket, position + Vector3.up * 3f, new Vector3(0.18f, 6f, 0.18f), _materials.Lit(new Color(0.08f, 0.08f, 0.09f), 0.6f, 0.8f));
                var head = At(d, side * (_settings.roadHalfWidth - 0.6f), 6f);
                Primitive("Lamp head", PrimitiveType.Cube, bucket, head, new Vector3(1.1f, 0.12f, 0.4f), _materials.Unlit(new Color(3f, 2.6f, 2f)));
                SpotLight(bucket, head - Vector3.up * 0.2f, Quaternion.Euler(90f, 0f, 0f), new Color(1f, 0.85f, 0.65f), 16f, 20f, 120f);
            }
        }

        private void BuildOverheadCables()
        {
            var prefabs = _assets != null ? _assets.overheadCablePrefabs : null;
            if (prefabs == null || prefabs.Length == 0) return;
            for (var d = 30f; d < _settings.courseLength; d += 34f + (float)_random.NextDouble() * 22f)
            {
                if (InTunnel(d, 10f)) continue;
                var prefab = prefabs[_random.Next(prefabs.Length)];
                if (prefab == null) continue;
                var bounds = PrefabBounds(prefab);
                if (Mathf.Max(bounds.size.x, bounds.size.z) < _settings.roadHalfWidth * 2f + 2f) continue;
                var rotation = Quaternion.LookRotation(Forward(d)) * Quaternion.Euler(0f, bounds.size.z >= bounds.size.x ? 90f : 0f, 0f);
                var target = _stage.Point(d) + Vector3.up * Mathf.Max(8.5f + (float)_random.NextDouble() * 3f, 6.5f + bounds.extents.y);
                Spawn(Bucket(d), prefab, target - rotation * bounds.center, rotation);
            }
        }

        private void BuildGallery()
        {
            var suit = _materials.Lit(new Color(0.12f, 0.14f, 0.2f), 0.35f);
            var boss = _materials.Lit(new Color(0.55f, 0.42f, 0.12f), 0.45f);
            var skin = _materials.Lit(new Color(0.9f, 0.7f, 0.55f), 0.3f);
            for (var d = _settings.courseLength / 3f + 6f; d < _tunnelStart - 14f; d += 11f)
                for (var side = -1; side <= 1; side += 2)
                {
                    var bucket = Bucket(d);
                    var rotation = Quaternion.LookRotation(-Vector3.Cross(Vector3.up, Forward(d)) * side);
                    for (var n = 0; n < 3; n++)
                    {
                        var foot = At(d + n * 1.3f, side * (_settings.roadHalfWidth + 1.5f + n % 2 * 0.8f), SidewalkHeight);
                        Primitive("Gallery suit", PrimitiveType.Capsule, bucket, foot + Vector3.up * 0.8f, new Vector3(0.65f, 0.8f, 0.65f), n == 1 ? boss : suit).transform.localRotation = rotation;
                        Primitive("Gallery head", PrimitiveType.Sphere, bucket, foot + Vector3.up * 1.8f, Vector3.one * 0.5f, skin);
                        var stick = Primitive("Glow stick", PrimitiveType.Cylinder, bucket, foot + Vector3.up * 2.05f + rotation * Vector3.right * 0.35f,
                            new Vector3(0.07f, 0.35f, 0.07f), _materials.Unlit(_neon[(int)(d + n) % _neon.Length]));
                        stick.transform.localRotation = rotation * Quaternion.Euler(0f, 0f, 20f);
                    }
                }
        }

        private void AddSign(float start, float length, int side)
        {
            if (_random.NextDouble() > 0.6) return;
            var along = start + length * (0.2f + (float)_random.NextDouble() * 0.6f);
            var word = _signWords[_random.Next(_signWords.Length)];
            var color = _neon[_random.Next(_neon.Length)];
            var height = word.Length * 1.05f + 0.5f;
            var root = new GameObject("Neon sign " + word).transform;
            root.SetParent(Bucket(along), false);
            root.localPosition = At(along, side * (_edge - 0.9f), 4.2f + (float)_random.NextDouble() * 5f + height * 0.5f);
            root.localRotation = Quaternion.LookRotation(Forward(along));
            Primitive("Frame", PrimitiveType.Cube, root, new Vector3(0f, 0f, 0.04f), new Vector3(1.4f, height + 0.16f, 0.08f), _materials.Unlit(color * 0.45f));
            Primitive("Board", PrimitiveType.Cube, root, Vector3.zero, new Vector3(1.24f, height, 0.14f), _materials.Lit(new Color(0.02f, 0.02f, 0.03f), 0.6f));
            var label = new GameObject("Neon text", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.09f);
            label.font = _font;
            label.fontSharedMaterial = NeonText(color);
            label.text = string.Join("\n", word.ToCharArray());
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 11f;
            label.lineSpacing = -18f;
            label.rectTransform.sizeDelta = new Vector2(1.1f, height - 0.2f);
        }

        private void AddScreen(Transform parent, Vector3 foot, Quaternion rotation, Vector3 size, int side)
        {
            var material = ScreenMaterial();
            if (material == null) return;
            var width = Mathf.Min(size.z * 0.8f, 9f + (float)_random.NextDouble() * 8f);
            var height = width * (_random.NextDouble() < 0.5 ? 0.6f : 1.6f);
            var elevation = Mathf.Clamp(size.y * 0.35f, height * 0.5f + 12f, size.y - height * 0.5f - 2f);
            var local = new Vector3(-side * (size.x * 0.5f + 0.3f), elevation, 0f);
            var quad = Primitive("Holo screen", PrimitiveType.Quad, parent, foot + rotation * local, new Vector3(width, height, 1f), material);
            quad.transform.localRotation = rotation * Quaternion.LookRotation(new Vector3(side, 0f, 0f));
        }

        private Material ScreenMaterial()
        {
            var screens = _assets != null ? _assets.screenMaterials : null;
            if (screens == null || screens.Length == 0) return null;
            return screens[_random.Next(screens.Length)];
        }

        private Material NeonText(Color color)
        {
            if (_neonText.TryGetValue(color, out var material)) return material;
            material = new Material(_font.material);
            material.SetColor(FaceColorId, color);
            _stage.GeneratedAssets.Add(material);
            _neonText.Add(color, material);
            return material;
        }

        private void CreateFacades()
        {
            var palettes = new[]
            {
                new[] { new Color(1f, 0.72f, 0.42f), new Color(1f, 0.85f, 0.6f) },
                new[] { new Color(0.55f, 0.8f, 1f), new Color(0.75f, 0.95f, 1f) },
                new[] { new Color(1f, 0.45f, 0.85f), new Color(0.5f, 0.9f, 1f), new Color(1f, 0.78f, 0.5f) },
                new[] { new Color(0.8f, 0.85f, 1f), new Color(1f, 0.6f, 0.35f) }
            };
            foreach (var palette in palettes) _facades.Add(FacadeMaterial(palette));
        }

        private Material FacadeMaterial(Color[] palette)
        {
            const int cell = 8;
            var size = FacadeCells * cell;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, name = "Facade albedo" };
            var emission = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, name = "Facade emission" };
            var wallColor = new Color(0.07f, 0.075f, 0.09f);
            var glassColor = new Color(0.1f, 0.12f, 0.16f);
            var albedoPixels = new Color[size * size];
            var emissionPixels = new Color[size * size];
            for (var cy = 0; cy < FacadeCells; cy++)
            {
                var floorLit = _random.NextDouble() < 0.12;
                for (var cx = 0; cx < FacadeCells; cx++)
                {
                    var lit = floorLit || _random.NextDouble() < 0.28;
                    var tint = palette[_random.Next(palette.Length)] * (0.35f + (float)_random.NextDouble() * 0.65f);
                    for (var y = 0; y < cell; y++)
                        for (var x = 0; x < cell; x++)
                        {
                            var window = x >= 1 && x <= 6 && y >= 2 && y <= 6;
                            var index = (cy * cell + y) * size + cx * cell + x;
                            albedoPixels[index] = window ? glassColor : wallColor;
                            emissionPixels[index] = window && lit ? tint : Color.black;
                        }
                }
            }
            albedo.SetPixels(albedoPixels);
            albedo.Apply(true, true);
            emission.SetPixels(emissionPixels);
            emission.Apply(true, true);
            return _materials.LitTextured(_materials.Register(albedo), _materials.Register(emission), new Color(2.2f, 2.2f, 2.2f), 0.55f);
        }

        private Material Facade() => _facades[_random.Next(_facades.Count)];

        private void Block(Transform parent, Vector3 foot, Quaternion rotation, Vector3 size, Material material)
        {
            var obj = new GameObject("City block", typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = foot + rotation * (Vector3.up * size.y * 0.5f);
            obj.transform.localRotation = rotation;
            obj.GetComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private Mesh BoxMesh(Vector3 size)
        {
            var half = size * 0.5f;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var faces = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
            foreach (var normal in faces)
            {
                var up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                var side = Vector3.Cross(up, normal);
                var width = Mathf.Abs(Vector3.Dot(side, size));
                var height = Mathf.Abs(Vector3.Dot(up, size));
                var center = Vector3.Scale(normal, half);
                var index = vertices.Count;
                vertices.Add(center - side * width * 0.5f - up * height * 0.5f);
                vertices.Add(center - side * width * 0.5f + up * height * 0.5f);
                vertices.Add(center + side * width * 0.5f + up * height * 0.5f);
                vertices.Add(center + side * width * 0.5f - up * height * 0.5f);
                var u = width / (FacadeCellWidth * FacadeCells);
                var v = height / (FacadeCellHeight * FacadeCells);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(u, v));
                uvs.Add(new Vector2(u, 0f));
                triangles.AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
            }
            var mesh = new Mesh { name = "City block" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _stage.GeneratedAssets.Add(mesh);
            return mesh;
        }

        private void Surface(string name, Material material, float start, float end, Func<float, float> from, Func<float, float> to, float height, Func<float, bool> include = null)
            => Sheet(name, material, start, end, d => new Vector2(from(d), height), d => new Vector2(to(d), height), include);

        // side は壁がどちら側にあるか。道路の中心を向く面を表にする
        private void Wall(string name, Material material, float start, float end, float lane, float bottom, float top, int side)
        {
            var lower = new Vector2(lane, bottom);
            var upper = new Vector2(lane, top);
            Sheet(name, material, start, end, _ => side > 0 ? lower : upper, _ => side > 0 ? upper : lower, null);
        }

        private void Sheet(string name, Material material, float start, float end, Func<float, Vector2> from, Func<float, Vector2> to, Func<float, bool> include)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (var d = start; d < end - 0.01f; d += Step)
            {
                var next = Mathf.Min(end, d + Step);
                if (include != null && !include((d + next) * 0.5f)) continue;
                var a = from(d);
                var b = from(next);
                var c = to(next);
                var e = to(d);
                var index = vertices.Count;
                vertices.Add(At(d, a.x, a.y));
                vertices.Add(At(next, b.x, b.y));
                vertices.Add(At(next, c.x, c.y));
                vertices.Add(At(d, e.x, e.y));
                uvs.Add(new Vector2(0f, d * 0.25f));
                uvs.Add(new Vector2(0f, next * 0.25f));
                uvs.Add(new Vector2(Vector2.Distance(b, c) * 0.25f, next * 0.25f));
                uvs.Add(new Vector2(Vector2.Distance(a, e) * 0.25f, d * 0.25f));
                triangles.AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
            }
            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            _stage.GeneratedAssets.Add(mesh);
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(_root, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private GameObject Spawn(Transform parent, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            return instance;
        }

        // 建物や街灯など、コース沿いに点在するオブジェクトを距離帯ごとにまとめる。プレイヤーから遠い帯はKartStageViewがSetActive(false)にして描画・カリング負荷を減らす
        private Transform Bucket(float distance)
        {
            var index = Mathf.FloorToInt(distance / BucketSize);
            if (_buckets.TryGetValue(index, out var bucket)) return bucket;
            bucket = new GameObject("City segment " + index * BucketSize).transform;
            bucket.SetParent(_root, false);
            _buckets.Add(index, bucket);
            _stage.Segments.Add(new KeyValuePair<float, GameObject>(index * BucketSize + BucketSize * 0.5f, bucket.gameObject));
            return bucket;
        }

        private Bounds PrefabBounds(GameObject prefab)
        {
            if (_bounds.TryGetValue(prefab, out var bounds)) return bounds;
            var root = prefab.transform;
            var first = true;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh.bounds;
                for (var corner = 0; corner < 8; corner++)
                {
                    var local = mesh.center + Vector3.Scale(mesh.extents, new Vector3(corner & 1, (corner >> 1) & 1, (corner >> 2) & 1) * 2f - Vector3.one);
                    var point = Vector3.Scale(root.InverseTransformPoint(filter.transform.TransformPoint(local)), root.localScale);
                    if (first) bounds = new Bounds(point, Vector3.zero);
                    else bounds.Encapsulate(point);
                    first = false;
                }
            }
            _bounds.Add(prefab, bounds);
            return bounds;
        }

        private GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            var renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return obj;
        }

        private void PointLight(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject("City light", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private void SpotLight(Transform parent, Vector3 position, Quaternion rotation, Color color, float intensity, float range, float angle)
        {
            var light = new GameObject("City spot", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.transform.localRotation = rotation;
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = angle;
            light.shadows = LightShadows.None;
        }

        private float[] Clearances(int side)
        {
            var result = new float[Mathf.CeilToInt(_end / 5f) + 2];
            for (var i = 0; i < result.Length; i++)
            {
                var d = i * 5f;
                var center = _stage.Point(d);
                var forward = Forward(d);
                var right = Vector3.Cross(Vector3.up, forward);
                var best = MaxGroundExtent * 2f;
                for (var j = 0; j < _stage.Path.Count; j++)
                {
                    if (Mathf.Abs(j * 5f - d) < 25f) continue;
                    var offset = _stage.Path[j] - center;
                    offset.y = 0f;
                    if (Mathf.Abs(Vector3.Dot(offset, forward)) > 14f) continue;
                    var lateral = Vector3.Dot(offset, right) * side;
                    if (lateral > 0f) best = Mathf.Min(best, lateral);
                }
                result[i] = best;
            }
            return result;
        }

        private float GroundExtent(float distance, int side)
        {
            var clearances = side < 0 ? _leftClearance : _rightClearance;
            var index = Mathf.Clamp(Mathf.RoundToInt(distance / 5f), 0, clearances.Length - 1);
            return Mathf.Clamp(clearances[index] * 0.5f, _settings.roadHalfWidth + SidewalkWidth + 1f, MaxGroundExtent);
        }

        private float DistanceToRoad(Vector3 point)
        {
            var best = float.MaxValue;
            var path = _stage.Path;
            point.y = 0f;
            for (var i = 0; i + 1 < path.Count; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                a.y = 0f;
                b.y = 0f;
                var segment = b - a;
                var t = Mathf.Clamp01(Vector3.Dot(point - a, segment) / Mathf.Max(0.0001f, segment.sqrMagnitude));
                best = Mathf.Min(best, (a + segment * t - point).sqrMagnitude);
            }
            return Mathf.Sqrt(best);
        }

        private bool InTunnel(float distance, float margin) => distance > _tunnelStart - margin && distance < _tunnelEnd + margin;

        private Vector3 Forward(float distance)
        {
            var forward = _stage.Point(distance + Step) - _stage.Point(distance - Step);
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private Vector3 At(float distance, float lane, float height)
            => _stage.Point(distance) + Vector3.Cross(Vector3.up, Forward(distance)) * lane + Vector3.up * height;
    }
}
