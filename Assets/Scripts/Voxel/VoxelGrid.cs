using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixVerse
{
    /// <summary>1個のボクセルが壊れたときの結果。座標・ローカル中心・衝撃の強さ(0〜1)を持つ。</summary>
    public readonly struct VoxelBreakResult
    {
        public Vector3Int Coordinate { get; }
        public Vector3 LocalCenter { get; }
        public float Strength { get; }

        public VoxelBreakResult(Vector3Int coordinate, Vector3 localCenter, float strength)
        {
            Coordinate = coordinate;
            LocalCenter = localCenter;
            Strength = strength;
        }
    }

    /// <summary>
    /// 立方体を小さな立方体(ボクセル)の3次元グリッドとして持ち、衝撃を受けた範囲を壊す。
    ///
    /// 衝撃はクリック地点を100%とし、指定した半径の縁に向かって0%まで弱まる。
    /// ボクセルごとにランダムな耐久力を持たせておき、その場の衝撃の強さが耐久力を上回った
    /// ボクセルだけを壊すことで、縁がきれいな球にならず自然にギザギザな穴になる。
    ///
    /// MonoBehaviour に依存しないので、VoxelDestructibleCube が保持して使う。
    /// </summary>
    public sealed class VoxelGrid
    {
        private readonly Vector3Int _dimensions;
        private readonly float _voxelSize;
        private readonly bool[,,] _alive;
        private readonly float[,,] _durability;

        public Vector3Int Dimensions => _dimensions;
        public float VoxelSize => _voxelSize;

        /// <summary>グリッド全体を包むローカル空間でのサイズ。</summary>
        public Vector3 LocalSize => new Vector3(_dimensions.x, _dimensions.y, _dimensions.z) * _voxelSize;

        public VoxelGrid(Vector3Int dimensions, float voxelSize, int randomSeed)
        {
            _dimensions = new Vector3Int(
                Mathf.Max(1, dimensions.x),
                Mathf.Max(1, dimensions.y),
                Mathf.Max(1, dimensions.z));

            _voxelSize = Mathf.Max(0.001f, voxelSize);

            _alive = new bool[_dimensions.x, _dimensions.y, _dimensions.z];
            _durability = new float[_dimensions.x, _dimensions.y, _dimensions.z];

            var random = new System.Random(randomSeed);

            for (var x = 0; x < _dimensions.x; x++)
            for (var y = 0; y < _dimensions.y; y++)
            for (var z = 0; z < _dimensions.z; z++)
            {
                _alive[x, y, z] = true;
                _durability[x, y, z] = (float)random.NextDouble();
            }
        }

        public bool IsAlive(Vector3Int coordinate) => _alive[coordinate.x, coordinate.y, coordinate.z];

        /// <summary>ボクセル中心のローカル座標。グリッド全体の中心が原点になるように並べる。</summary>
        public Vector3 GetLocalCenter(Vector3Int coordinate)
        {
            return new Vector3(
                (coordinate.x - ((_dimensions.x - 1) * 0.5f)) * _voxelSize,
                (coordinate.y - ((_dimensions.y - 1) * 0.5f)) * _voxelSize,
                (coordinate.z - ((_dimensions.z - 1) * 0.5f)) * _voxelSize);
        }

        /// <summary>
        /// ローカル座標 localPoint を中心に半径 radius の衝撃を与え、壊れたボクセルを返す。
        /// 中心ほど強く、radius の縁に近いほど弱い衝撃になる。
        /// </summary>
        public List<VoxelBreakResult> ApplyImpact(Vector3 localPoint, float radius)
        {
            var broken = new List<VoxelBreakResult>();

            if (radius <= 0f)
            {
                return broken;
            }

            for (var x = 0; x < _dimensions.x; x++)
            for (var y = 0; y < _dimensions.y; y++)
            for (var z = 0; z < _dimensions.z; z++)
            {
                if (!_alive[x, y, z])
                {
                    continue;
                }

                var coordinate = new Vector3Int(x, y, z);
                var localCenter = GetLocalCenter(coordinate);
                var distance = Vector3.Distance(localCenter, localPoint);

                if (distance > radius)
                {
                    continue;
                }

                var strength = 1f - (distance / radius);

                if (strength < _durability[x, y, z])
                {
                    continue;
                }

                _alive[x, y, z] = false;
                broken.Add(new VoxelBreakResult(coordinate, localCenter, strength));
            }

            return broken;
        }
    }
}
