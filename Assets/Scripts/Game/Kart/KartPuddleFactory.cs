using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixVerse.Game.Kart
{
    // 路面の起伏とカーブに沿わせた格子メッシュを作り、水溜りシェーダーを貼る。輪郭の揺らぎは UV の seed からシェーダー側で作る
    public sealed class KartPuddleFactory
    {
        private const int Columns = 8;
        private const int Rows = 8;
        private const float SurfaceLift = 0.02f;
        private static readonly int SkyCubeId = Shader.PropertyToID("_SkyCube");
        private static readonly int SkyIntensityId = Shader.PropertyToID("_SkyIntensity");
        private static readonly int SkyTextureId = Shader.PropertyToID("_Tex");
        private static readonly int SkyExposureId = Shader.PropertyToID("_Exposure");
        private readonly KartStageView _stage;
        private readonly Material _material;

        // 画面外へ抜けた反射は夜空で埋めるので、レースのスカイボックスと同じキューブマップと露出を渡す
        public KartPuddleFactory(KartStageView stage, Material skybox)
        {
            _stage = stage;
            _material = new Material(Resources.Load<Shader>("KartPuddle"));
            _stage.GeneratedAssets.Add(_material);
            if (skybox == null || !skybox.HasTexture(SkyTextureId)) return;
            _material.SetTexture(SkyCubeId, skybox.GetTexture(SkyTextureId));
            if (skybox.HasFloat(SkyExposureId)) _material.SetFloat(SkyIntensityId, skybox.GetFloat(SkyExposureId));
        }

        public void Create(Transform parent, float distance, float lane, Vector2 size, float seed)
        {
            var center = SurfacePoint(distance, lane);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector4>();
            var triangles = new List<int>();
            for (var row = 0; row <= Rows; row++)
            {
                var v = row / (float)Rows * 2f - 1f;
                for (var column = 0; column <= Columns; column++)
                {
                    var u = column / (float)Columns * 2f - 1f;
                    vertices.Add(SurfacePoint(distance + v * size.y * 0.5f, lane + u * size.x * 0.5f) - center);
                    uvs.Add(new Vector4(u, v, seed, 0f));
                }
            }
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var back = row * (Columns + 1) + column;
                    var front = back + Columns + 1;
                    triangles.AddRange(new[] { back, front, front + 1, back, front + 1, back + 1 });
                }
            }
            var mesh = new Mesh { name = "Puddle" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _stage.GeneratedAssets.Add(mesh);
            var puddle = new GameObject("Puddle", typeof(MeshFilter), typeof(MeshRenderer));
            puddle.transform.SetParent(parent, false);
            puddle.transform.localPosition = center;
            puddle.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = puddle.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
        }

        private Vector3 SurfacePoint(float distance, float lane) => _stage.Point(distance, lane) + Vector3.up * SurfaceLift;
    }
}
