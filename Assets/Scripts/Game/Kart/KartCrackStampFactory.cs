using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixVerse.Game.Kart
{
    public sealed class KartCrackStampFactory
    {
        private readonly KartStageView _stage;
        private readonly Material _material;
        private readonly float _radius;
        private readonly float _roadHalfWidth;
        private static readonly Color EdgeColor = new Color(0.66f, 0.59f, 0.48f, 0.72f);
        private static readonly Color CoreColor = new Color(0.015f, 0.018f, 0.022f, 0.98f);

        private readonly Vector2[][][] _patterns =
        {
            new[]
            {
                new[] { P(-0.95f, -0.22f), P(-0.58f, -0.1f), P(-0.2f, 0.05f), P(0.16f, 0.17f), P(0.52f, 0.13f), P(0.94f, 0.32f) },
                new[] { P(-0.2f, 0.05f), P(-0.13f, 0.38f), P(-0.31f, 0.7f), P(-0.25f, 0.96f) },
                new[] { P(0.52f, 0.13f), P(0.58f, -0.25f), P(0.77f, -0.54f) }
            },
            new[]
            {
                new[] { P(0f, -0.98f), P(-0.12f, -0.6f), P(0.03f, -0.2f), P(0.02f, 0.17f), P(-0.24f, 0.53f), P(-0.18f, 0.98f) },
                new[] { P(0.03f, -0.2f), P(0.37f, -0.05f), P(0.64f, -0.16f), P(0.98f, -0.02f) },
                new[] { P(0.02f, 0.17f), P(0.34f, 0.43f), P(0.54f, 0.78f) }
            },
            new[]
            {
                new[] { P(-0.84f, 0.7f), P(-0.55f, 0.45f), P(-0.44f, 0.1f), P(-0.07f, -0.08f), P(0.12f, -0.39f), P(0.5f, -0.59f), P(0.88f, -0.88f) },
                new[] { P(-0.44f, 0.1f), P(-0.67f, -0.18f), P(-0.88f, -0.37f) },
                new[] { P(-0.07f, -0.08f), P(0.27f, 0.13f), P(0.61f, 0.08f), P(0.89f, 0.35f) }
            },
            new[]
            {
                new[] { P(-0.94f, -0.67f), P(-0.6f, -0.53f), P(-0.32f, -0.23f), P(0.1f, -0.15f), P(0.42f, 0.18f), P(0.81f, 0.29f) },
                new[] { P(-0.32f, -0.23f), P(-0.5f, 0.08f), P(-0.36f, 0.44f), P(-0.56f, 0.85f) },
                new[] { P(0.1f, -0.15f), P(0.28f, -0.51f), P(0.18f, -0.91f) },
                new[] { P(0.42f, 0.18f), P(0.7f, 0.56f), P(0.94f, 0.72f) }
            }
        };

        public KartCrackStampFactory(KartStageView stage, Material material, float radius, float roadHalfWidth)
        {
            _stage = stage;
            _material = material;
            _radius = radius;
            _roadHalfWidth = roadHalfWidth;
        }

        public void Create(KartExplosionView effect, float distance, float lane, int seed)
        {
            var random = new System.Random(unchecked(seed * 397 ^ Environment.TickCount));
            var stampCount = 4 + random.Next(2);
            var firstPattern = random.Next(_patterns.Length);
            for (var i = 0; i < stampCount; i++)
            {
                var pattern = _patterns[(firstPattern + i) % _patterns.Length];
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var scale = _radius * (i == 0 ? 0.9f : 0.42f + (float)random.NextDouble() * 0.38f);
                var offset = i == 0 ? Vector2.zero : new Vector2(((float)random.NextDouble() - 0.5f) * _radius, ((float)random.NextDouble() - 0.5f) * _radius);
                var vertices = new List<Vector3>();
                var colors = new List<Color>();
                var triangles = new List<int>();
                for (var path = 0; path < pattern.Length; path++)
                {
                    var points = pattern[path];
                    for (var segment = 0; segment < points.Length - 1; segment++)
                    {
                        var a = offset + Rotate(points[segment], angle) * scale;
                        var b = offset + Rotate(points[segment + 1], angle) * scale;
                        var direction = (b - a).normalized;
                        var side = new Vector2(-direction.y, direction.x);
                        var taperA = Mathf.Clamp01((segment + 0.6f) / 1.7f);
                        var taperB = Mathf.Clamp01((points.Length - segment - 1.4f) / 1.7f);
                        var width = path == 0 ? 0.14f : 0.06f;
                        AddBand(vertices, colors, triangles, a, b, side, width + 0.065f, width + 0.065f, EdgeColor, distance, lane);
                        AddBand(vertices, colors, triangles, a, b, side, width * taperA, width * taperB, CoreColor, distance, lane);
                    }
                }
                var mesh = new Mesh { name = $"Crack pattern {i + 1}", vertices = vertices.ToArray(), colors = colors.ToArray(), triangles = triangles.ToArray() };
                mesh.RecalculateBounds();
                var stamp = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                stamp.transform.SetParent(effect.transform, false);
                stamp.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = stamp.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                effect.AddGroundCrack(renderer, mesh);
            }
        }

        private void AddBand(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector2 a, Vector2 b,
            Vector2 side, float widthA, float widthB, Color color, float distance, float lane)
        {
            var first = vertices.Count;
            vertices.Add(SurfacePoint(a + side * widthA, distance, lane));
            vertices.Add(SurfacePoint(a - side * widthA, distance, lane));
            vertices.Add(SurfacePoint(b + side * widthB, distance, lane));
            vertices.Add(SurfacePoint(b - side * widthB, distance, lane));
            for (var i = 0; i < 4; i++) colors.Add(color);
            triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 1);
            triangles.Add(first + 1); triangles.Add(first + 2); triangles.Add(first + 3);
        }

        private Vector3 SurfacePoint(Vector2 point, float distance, float lane)
        {
            var lateral = Mathf.Clamp(lane + point.x, -_roadHalfWidth + 0.2f, _roadHalfWidth - 0.2f);
            var world = _stage.Point(distance + point.y, lateral) + Vector3.up * 0.075f;
            var origin = _stage.Point(distance, lane) + Vector3.up * 0.8f;
            return Quaternion.Inverse(_stage.DirectionAt(distance)) * (world - origin);
        }

        private Vector2 Rotate(Vector2 point, float angle)
        {
            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        private static Vector2 P(float x, float y) => new Vector2(x, y);
    }
}
