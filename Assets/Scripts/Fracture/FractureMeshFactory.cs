using System.Collections.Generic;
using UnityEngine;

namespace MixVerse.Fracture
{
    public enum FractureShape { Cube, Sphere }

    public sealed class FractureMeshFactory
    {
        public Mesh Create(FractureShape shape, int resolution)
        {
            resolution = Mathf.Clamp(resolution, 2, 10);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var centers = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            for (var z = 0; z < resolution; z++)
            for (var y = 0; y < resolution; y++)
            for (var x = 0; x < resolution; x++)
            {
                var cell = new Vector3Int(x, y, z);
                var center = Vector3.zero;
                for (var corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3(corner & 1, (corner >> 1) & 1, (corner >> 2) & 1);
                    center += Map(((Vector3)cell + offset) * (2f / resolution) - Vector3.one, shape) / 8f;
                }
                for (var axis = 0; axis < 3; axis++)
                for (var side = 0; side < 2; side++)
                {
                    var u = (axis + 1) % 3;
                    var v = (axis + 2) % 3;
                    var points = new Vector3[4];
                    for (var corner = 0; corner < 4; corner++)
                    {
                        var grid = (Vector3)cell;
                        grid[axis] += side;
                        grid[u] += corner == 1 || corner == 2 ? 1 : 0;
                        grid[v] += corner >= 2 ? 1 : 0;
                        points[corner] = Map(grid * (2f / resolution) - Vector3.one, shape);
                    }
                    var exterior = cell[axis] == (side == 0 ? 0 : resolution - 1);
                    var normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized * (side == 0 ? -1 : 1);
                    var start = vertices.Count;
                    for (var corner = 0; corner < 4; corner++)
                    {
                        vertices.Add(points[corner]);
                        normals.Add(exterior && shape == FractureShape.Sphere ? points[corner].normalized : normal);
                        centers.Add(center);
                        colors.Add(exterior ? Color.white : new Color(0, 0, 0, 1));
                    }
                    var order = side == 0 ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
                    foreach (var index in order) indices.Add(start + index);
                }
            }
            var mesh = new Mesh { name = $"{shape} - {resolution * resolution * resolution} closed fragments" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(1, centers);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 16f);
            return mesh;
        }

        private Vector3 Map(Vector3 point, FractureShape shape)
        {
            if (shape == FractureShape.Cube) return point;
            var square = Vector3.Scale(point, point);
            return new Vector3(
                point.x * Mathf.Sqrt(1f - square.y * 0.5f - square.z * 0.5f + square.y * square.z / 3f),
                point.y * Mathf.Sqrt(1f - square.z * 0.5f - square.x * 0.5f + square.z * square.x / 3f),
                point.z * Mathf.Sqrt(1f - square.x * 0.5f - square.y * 0.5f + square.x * square.y / 3f));
        }
    }
}
