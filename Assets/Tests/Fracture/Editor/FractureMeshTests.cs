using System.Collections.Generic;
using MixVerse.Fracture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MixVerse.FractureTests
{
    public sealed class FractureMeshTests
    {
        [TestCase(FractureShape.Cube, 2)]
        [TestCase(FractureShape.Cube, 6)]
        [TestCase(FractureShape.Sphere, 6)]
        [TestCase(FractureShape.Sphere, 10)]
        public void FragmentsAreClosedAndOutwardFacing(FractureShape shape, int resolution)
        {
            var mesh = new FractureMeshFactory().Create(shape, resolution);
            try
            {
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                var colors = mesh.colors;
                var centers = new List<Vector3>();
                mesh.GetUVs(1, centers);
                Assert.That(vertices.Length, Is.EqualTo(resolution * resolution * resolution * 24));
                for (var fragment = 0; fragment < resolution * resolution * resolution; fragment++)
                {
                    var edges = new Dictionary<(Vector3, Vector3), int>();
                    for (var t = fragment * 36; t < fragment * 36 + 36; t += 3)
                    {
                        var a = vertices[triangles[t]];
                        var b = vertices[triangles[t + 1]];
                        var c = vertices[triangles[t + 2]];
                        Assert.That(Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3 - centers[triangles[t]]), Is.GreaterThan(0));
                        AddEdge(edges, a, b); AddEdge(edges, b, c); AddEdge(edges, c, a);
                    }
                    foreach (var edge in edges)
                    {
                        Assert.That(edge.Value, Is.EqualTo(1));
                        Assert.That(edges.ContainsKey((edge.Key.Item2, edge.Key.Item1)), Is.True);
                    }
                }
                for (var i = 0; i < vertices.Length; i++)
                {
                    if (shape == FractureShape.Sphere && colors[i].r > 0.5f)
                        Assert.That(vertices[i].magnitude, Is.EqualTo(1).Within(0.00001f));
                    Assert.That(mesh.bounds.Contains(vertices[i]), Is.True);
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void ShaderCompilesWithProgressProperty()
        {
            var shader = Shader.Find("MixVerse/SwordFractureURP");
            Assert.That(shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            var material = new Material(shader);
            try { Assert.That(material.HasProperty("_Progress"), Is.True); }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void PlaybackStopsAtOneAndScrubbingRewindsAndClamps()
        {
            var root = new GameObject("Fracture presenter test");
            root.SetActive(false);
            try
            {
                var presenter = new FractureDemoPresenter(root.AddComponent<FractureDemoView>());
                presenter.SetProgress(-2);
                Assert.That(presenter.Progress, Is.Zero);
                presenter.Play();
                presenter.Tick(1.25f);
                Assert.That(presenter.Progress, Is.EqualTo(0.5f).Within(0.0001f));
                presenter.SetProgress(0.2f);
                presenter.Tick(1);
                Assert.That(presenter.Progress, Is.EqualTo(0.2f));
                Assert.That(presenter.IsPlaying, Is.False);
                presenter.Play();
                presenter.Tick(10);
                Assert.That(presenter.Progress, Is.EqualTo(1));
                Assert.That(presenter.IsPlaying, Is.False);
                presenter.SetProgress(2);
                Assert.That(presenter.Progress, Is.EqualTo(1));
                presenter.SetProgress(0);
                Assert.That(presenter.Progress, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private void AddEdge(Dictionary<(Vector3, Vector3), int> edges, Vector3 a, Vector3 b)
        {
            edges.TryGetValue((a, b), out var count);
            edges[(a, b)] = count + 1;
        }
    }
}
