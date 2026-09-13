using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixVerse
{
    /// <summary>
    /// VoxelDensityField をマーチングキューブ法でポリゴン化するHelper。
    ///
    /// セル8頂点の密度の符号パターン(cubeIndex, 0-255)から MarchingCubesTables を引き、
    /// 等値面(密度0)と交差する辺の上に頂点を線形補間で置いて三角形を組み立てる。
    /// 同じ位置に来る頂点はセルをまたいでも1つにまとめるので、法線をなめらかに平均化でき、
    /// ボクセル1個ずつが見えるカクカクした見た目にならない。
    /// </summary>
    internal static class MarchingCubesMesher
    {
        // 立方体8頂点のローカルオフセット(Bourke版の頂点番号に合わせる)
        private static readonly Vector3Int[] CornerOffsets =
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(1, 1, 0),
            new(0, 1, 0),
            new(0, 0, 1),
            new(1, 0, 1),
            new(1, 1, 1),
            new(0, 1, 1),
        };

        // 12本の辺が、上のCornerOffsetsのどの2頂点を結ぶか
        private static readonly int[,] EdgeCorners =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },
        };

        public static Mesh Generate(VoxelDensityField field)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var vertexLookup = new Dictionary<Vector3Int, int>();

            var cornerDensity = new float[8];
            var edgeVertex = new Vector3[12];
            var cellCount = field.PointCount - Vector3Int.one;

            for (var x = 0; x < cellCount.x; x++)
            for (var y = 0; y < cellCount.y; y++)
            for (var z = 0; z < cellCount.z; z++)
            {
                var origin = new Vector3Int(x, y, z);

                var cubeIndex = 0;
                for (var corner = 0; corner < 8; corner++)
                {
                    cornerDensity[corner] = field.GetDensity(origin + CornerOffsets[corner]);
                    if (cornerDensity[corner] < 0f)
                    {
                        cubeIndex |= 1 << corner;
                    }
                }

                var edgeMask = MarchingCubesTables.EdgeMask[cubeIndex];
                if (edgeMask == 0)
                {
                    continue;
                }

                for (var edge = 0; edge < 12; edge++)
                {
                    if ((edgeMask & (1 << edge)) == 0)
                    {
                        continue;
                    }

                    var cornerA = EdgeCorners[edge, 0];
                    var cornerB = EdgeCorners[edge, 1];

                    edgeVertex[edge] = InterpolateSurfacePoint(
                        field.GetLocalPosition(origin + CornerOffsets[cornerA]), cornerDensity[cornerA],
                        field.GetLocalPosition(origin + CornerOffsets[cornerB]), cornerDensity[cornerB]);
                }

                var triangulation = MarchingCubesTables.Triangulation[cubeIndex];
                for (var i = 0; i < triangulation.Length && triangulation[i] != -1; i += 3)
                {
                    triangles.Add(GetOrAddVertex(edgeVertex[triangulation[i]], vertices, vertexLookup));
                    triangles.Add(GetOrAddVertex(edgeVertex[triangulation[i + 1]], vertices, vertexLookup));
                    triangles.Add(GetOrAddVertex(edgeVertex[triangulation[i + 2]], vertices, vertexLookup));
                }
            }

            var mesh = new Mesh
            {
                indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 同じ位置の頂点はセルをまたいでも使い回す。共有することで RecalculateNormals が
        /// 隣接面をまたいで法線を平均化でき、なめらかな見た目になる。
        /// </summary>
        private static int GetOrAddVertex(Vector3 position, List<Vector3> vertices, Dictionary<Vector3Int, int> lookup)
        {
            const float quantize = 100000f;
            var key = new Vector3Int(
                Mathf.RoundToInt(position.x * quantize),
                Mathf.RoundToInt(position.y * quantize),
                Mathf.RoundToInt(position.z * quantize));

            if (lookup.TryGetValue(key, out var index))
            {
                return index;
            }

            index = vertices.Count;
            vertices.Add(position);
            lookup[key] = index;
            return index;
        }

        /// <summary>isolevel=0として、密度0になる位置を辺の上で線形補間する。</summary>
        private static Vector3 InterpolateSurfacePoint(Vector3 p1, float density1, Vector3 p2, float density2)
        {
            if (Mathf.Abs(density2 - density1) < 0.00001f)
            {
                return p1;
            }

            var t = -density1 / (density2 - density1);
            return p1 + (t * (p2 - p1));
        }
    }
}
