using System.Collections.Generic;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    public sealed class KartBuildingClearance
    {
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
