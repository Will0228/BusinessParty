using System.Collections.Generic;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    public sealed class KartBuildingClearance
    {
        private readonly List<(Vector3 center, Quaternion rotation, Vector2 extents)> _footprints =
            new List<(Vector3, Quaternion, Vector2)>();

        public bool TryReserve(IReadOnlyList<Vector3> path, Vector3 center, Quaternion rotation,
            float depth, float length, float clearance)
        {
            if (OverlapsRoad(path, center, rotation, depth, length, clearance)) return false;
            var extents = new Vector2(depth, length) * 0.5f;
            foreach (var footprint in _footprints)
            {
                var offset = center - footprint.center;
                offset.y = 0f;
                if (Separated(offset, rotation, extents, footprint.rotation, footprint.extents, rotation * Vector3.right)
                    || Separated(offset, rotation, extents, footprint.rotation, footprint.extents, rotation * Vector3.forward)
                    || Separated(offset, rotation, extents, footprint.rotation, footprint.extents, footprint.rotation * Vector3.right)
                    || Separated(offset, rotation, extents, footprint.rotation, footprint.extents, footprint.rotation * Vector3.forward)) continue;
                return false;
            }
            _footprints.Add((center, rotation, extents));
            return true;
        }

        private bool Separated(Vector3 offset, Quaternion a, Vector2 extentA, Quaternion b, Vector2 extentB, Vector3 axis)
        {
            var radiusA = Mathf.Abs(Vector3.Dot(a * Vector3.right, axis)) * extentA.x
                + Mathf.Abs(Vector3.Dot(a * Vector3.forward, axis)) * extentA.y;
            var radiusB = Mathf.Abs(Vector3.Dot(b * Vector3.right, axis)) * extentB.x
                + Mathf.Abs(Vector3.Dot(b * Vector3.forward, axis)) * extentB.y;
            return Mathf.Abs(Vector3.Dot(offset, axis)) > radiusA + radiusB + 0.3f;
        }

        public bool OverlapsRoad(IReadOnlyList<Vector3> path, Vector3 center, Quaternion rotation,
            float depth, float length, float clearance)
        {
            var inverse = Quaternion.Inverse(rotation);
            // 高低差があっても道路上を建物で塞がないよう、XZ平面で余白付きの敷地と線分を判定する。
            var extentX = depth * 0.5f + clearance;
            var extentZ = length * 0.5f + clearance;
            for (var i = 0; i + 1 < path.Count; i++)
            {
                var a = inverse * (path[i] - center);
                var b = inverse * (path[i + 1] - center);
                var middle = (a + b) * 0.5f;
                var half = (b - a) * 0.5f;
                if (Mathf.Abs(middle.x) > extentX + Mathf.Abs(half.x)) continue;
                if (Mathf.Abs(middle.z) > extentZ + Mathf.Abs(half.z)) continue;
                if (Mathf.Abs(middle.x * half.z - middle.z * half.x)
                    > extentX * Mathf.Abs(half.z) + extentZ * Mathf.Abs(half.x)) continue;
                return true;
            }
            return false;
        }
    }
}
