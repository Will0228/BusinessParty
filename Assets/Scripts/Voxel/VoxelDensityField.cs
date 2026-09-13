using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// マーチングキューブ法の入力となる密度場。
    ///
    /// 格子点ごとに「立方体の面までの符号付き距離」に近い値を持たせておく(面の上で0、
    /// 内側が正、外側が負)。衝撃を受けた分だけ差し引くことで、削れた分だけ密度が下がり、
    /// 0を下回った場所に穴があく。中心に近いほど深く削れ、半径の縁に近いほど浅くしか
    /// 削れないので、距離に応じて弱くなる衝撃をそのまま滑らかな凹みとして表現できる。
    ///
    /// マーチングキューブは面を閉じるために「立方体の外側」も1セルぶん余分にサンプルする
    /// 必要があるため、格子点の数は resolution よりひとまわり大きい。
    /// MonoBehaviour に依存しないので、VoxelDestructibleCube が保持して使う。
    /// </summary>
    public sealed class VoxelDensityField
    {
        private const int Margin = 1;

        private readonly Vector3 _boxSize;
        private readonly Vector3 _cellSize;
        private readonly float[,,] _density;

        public Vector3Int PointCount { get; }

        public VoxelDensityField(Vector3Int resolution, Vector3 boxSize)
        {
            var safeResolution = new Vector3Int(
                Mathf.Max(1, resolution.x),
                Mathf.Max(1, resolution.y),
                Mathf.Max(1, resolution.z));

            _boxSize = boxSize;
            _cellSize = new Vector3(
                boxSize.x / safeResolution.x,
                boxSize.y / safeResolution.y,
                boxSize.z / safeResolution.z);

            PointCount = safeResolution + (Vector3Int.one * (Margin * 2 + 1));
            _density = new float[PointCount.x, PointCount.y, PointCount.z];

            FillSolidBox();
        }

        private void FillSolidBox()
        {
            var halfSize = _boxSize * 0.5f;

            for (var x = 0; x < PointCount.x; x++)
            for (var y = 0; y < PointCount.y; y++)
            for (var z = 0; z < PointCount.z; z++)
            {
                var localPosition = GetLocalPosition(new Vector3Int(x, y, z));
                _density[x, y, z] = BoxSignedDistance(localPosition, halfSize);
            }
        }

        /// <summary>立方体の面までの符号付き距離に近い値。面の上で0、内側が正、外側が負。</summary>
        private static float BoxSignedDistance(Vector3 localPosition, Vector3 halfSize)
        {
            return Mathf.Min(
                halfSize.x - Mathf.Abs(localPosition.x),
                Mathf.Min(halfSize.y - Mathf.Abs(localPosition.y), halfSize.z - Mathf.Abs(localPosition.z)));
        }

        /// <summary>格子点のローカル座標。立方体の中心が原点になるように配置する。</summary>
        public Vector3 GetLocalPosition(Vector3Int point)
        {
            return new Vector3(
                ((point.x - Margin) * _cellSize.x) - (_boxSize.x * 0.5f),
                ((point.y - Margin) * _cellSize.y) - (_boxSize.y * 0.5f),
                ((point.z - Margin) * _cellSize.z) - (_boxSize.z * 0.5f));
        }

        public float GetDensity(Vector3Int point) => _density[point.x, point.y, point.z];

        /// <summary>
        /// ローカル座標 localPoint を中心に半径 radius の衝撃を与える。
        /// 中心(距離0)ではdigDepthぶん削り、半径の縁(距離=radius)に近づくほど浅くなる。
        /// </summary>
        public void ApplyImpact(Vector3 localPoint, float radius, float digDepth)
        {
            if (radius <= 0f)
            {
                return;
            }

            for (var x = 0; x < PointCount.x; x++)
            for (var y = 0; y < PointCount.y; y++)
            for (var z = 0; z < PointCount.z; z++)
            {
                var point = new Vector3Int(x, y, z);
                var distance = Vector3.Distance(GetLocalPosition(point), localPoint);

                if (distance > radius)
                {
                    continue;
                }

                var strength = 1f - (distance / radius);
                _density[x, y, z] -= strength * digDepth;
            }
        }
    }
}
