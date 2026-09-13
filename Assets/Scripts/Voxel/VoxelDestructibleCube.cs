using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 小さな立方体(ボクセル)を並べて1つの立方体に見せ、衝撃を受けた場所に穴をあけられるオブジェクト。
    ///
    /// 実際にどのボクセルが壊れるかは VoxelGrid が判定する。ここはボクセルの生成・破片の飛散など
    /// 見た目の管理と、ワールド座標⇔ローカル座標の変換を受け持つ。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VoxelDestructibleCube : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("縦・横・奥行きのボクセル数。")]
        [SerializeField] private Vector3Int _dimensions = new Vector3Int(12, 12, 12);

        [Tooltip("ボクセル1個の大きさ(1辺の長さ)。")]
        [SerializeField] private float _voxelSize = 0.2f;

        [Tooltip("ボクセル同士の隙間。0だと隙間なし、大きいほど1個ずつの粒が見える。")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _voxelGap = 0.06f;

        [Tooltip("耐久力をランダムに割り振るための乱数シード。同じ値なら同じ壊れ方になる。")]
        [SerializeField] private int _randomSeed = 12345;

        [Header("Look")]
        [SerializeField] private Material _material;

        [Header("Debris")]
        [Tooltip("壊れたボクセルが飛び散る勢い。")]
        [SerializeField] private float _debrisForce = 2.5f;

        [Tooltip("飛び散った破片が消えるまでの時間(秒)。")]
        [SerializeField] private float _debrisLifetime = 2.5f;

        private VoxelGrid _grid;
        private GameObject[,,] _voxelObjects;
        private Transform _voxelRoot;
        private Mesh _voxelMesh;

        private void Awake()
        {
            Rebuild();
        }

        /// <summary>今の状態を捨てて、壊れていない状態からボクセルを組み直す。</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (_voxelRoot != null)
            {
                Destroy(_voxelRoot.gameObject);
            }

            _voxelMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _grid = new VoxelGrid(_dimensions, _voxelSize, _randomSeed);
            _voxelObjects = new GameObject[_grid.Dimensions.x, _grid.Dimensions.y, _grid.Dimensions.z];

            _voxelRoot = new GameObject("Voxels").transform;
            _voxelRoot.SetParent(transform, false);

            var scale = Vector3.one * (_voxelSize * (1f - _voxelGap));

            for (var x = 0; x < _grid.Dimensions.x; x++)
            for (var y = 0; y < _grid.Dimensions.y; y++)
            for (var z = 0; z < _grid.Dimensions.z; z++)
            {
                var coordinate = new Vector3Int(x, y, z);
                _voxelObjects[x, y, z] = CreateVoxel(coordinate, scale);
            }

            ApplyColliderBounds();
        }

        private GameObject CreateVoxel(Vector3Int coordinate, Vector3 scale)
        {
            var voxel = new GameObject($"Voxel_{coordinate.x}_{coordinate.y}_{coordinate.z}");
            voxel.transform.SetParent(_voxelRoot, false);
            voxel.transform.localPosition = _grid.GetLocalCenter(coordinate);
            voxel.transform.localScale = scale;

            var meshFilter = voxel.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = _voxelMesh;

            var meshRenderer = voxel.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;

            return voxel;
        }

        /// <summary>クリック判定に使う当たり判定を、ボクセル全体を包む大きさに合わせる。</summary>
        private void ApplyColliderBounds()
        {
            var collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            collider.center = Vector3.zero;
            collider.size = _grid.LocalSize;
        }

        /// <summary>
        /// ワールド座標 worldPoint を中心に半径 radius の衝撃を与え、範囲内のボクセルを壊す。
        /// worldNormal は破片が飛ぶ向き(衝撃を受けた面から手前に飛ばす)に使う。
        /// </summary>
        public void ApplyImpact(Vector3 worldPoint, Vector3 worldNormal, float radius)
        {
            var localPoint = transform.InverseTransformPoint(worldPoint);

            var scale = transform.lossyScale;
            var uniformScale = Mathf.Max(0.0001f, (scale.x + scale.y + scale.z) / 3f);
            var localRadius = radius / uniformScale;

            var broken = _grid.ApplyImpact(localPoint, localRadius);

            foreach (var result in broken)
            {
                SpawnDebris(result, worldPoint, worldNormal);
            }
        }

        private void SpawnDebris(VoxelBreakResult result, Vector3 worldImpactPoint, Vector3 worldNormal)
        {
            var voxel = _voxelObjects[result.Coordinate.x, result.Coordinate.y, result.Coordinate.z];
            if (voxel == null)
            {
                return;
            }

            _voxelObjects[result.Coordinate.x, result.Coordinate.y, result.Coordinate.z] = null;

            // ワールド座標を保ったまま切り離し、破片として単独で飛ばす
            voxel.transform.SetParent(null, true);

            var rigidbody = voxel.AddComponent<Rigidbody>();
            var collider = voxel.AddComponent<BoxCollider>();
            collider.size = Vector3.one;

            // 衝撃を受けた面から手前へ、かつ衝撃の中心から外側へ向かう向きに飛ばす
            var outward = voxel.transform.position - worldImpactPoint;
            var direction = ((worldNormal * 0.6f) + (outward.normalized * 0.4f)).normalized;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = worldNormal;
            }

            rigidbody.AddForce(direction * (_debrisForce * (0.5f + result.Strength)), ForceMode.Impulse);
            rigidbody.AddTorque(Random.insideUnitSphere * _debrisForce, ForceMode.Impulse);

            Destroy(voxel, _debrisLifetime);
        }
    }
}
