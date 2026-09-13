using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// マーチングキューブ法でポリゴン化した、なめらかな見た目のまま自由に壊せる立方体。
    ///
    /// 実体はVoxelDensityFieldが持つ密度場で、衝撃を受けるとその場を削り、
    /// MarchingCubesMesherでメッシュを組み直す。ボクセルを個別のオブジェクトとして
    /// 持たないので、Minecraftのようなカクカクした見た目にならず、削れた面もなめらかになる。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class VoxelDestructibleCube : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("密度場を区切るセルの数(縦・横・奥行き)。大きいほどなめらかだが重くなる。")]
        [SerializeField] private Vector3Int _resolution = new(24, 24, 24);

        [Tooltip("立方体全体の大きさ。")]
        [SerializeField] private Vector3 _boxSize = new(2.4f, 2.4f, 2.4f);

        [Header("Look")]
        [SerializeField] private Material _material;

        [Header("Dig")]
        [Tooltip("衝撃の中心で削る深さの、半径に対する倍率。大きいほど1回の衝撃で貫通しやすい。")]
        [SerializeField] private float _digDepthMultiplier = 2f;

        private VoxelDensityField _field;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            GetComponent<MeshRenderer>().sharedMaterial = _material;
            _meshCollider = GetOrAddComponent<MeshCollider>();

            Rebuild();
        }

        private void OnDestroy()
        {
            DestroyMesh(_mesh);
        }

        /// <summary>今の状態を捨てて、壊れていない状態から密度場を組み直す。</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            _field = new VoxelDensityField(_resolution, _boxSize);
            RegenerateMesh();
        }

        /// <summary>
        /// ワールド座標 worldPoint を中心に半径 radius の衝撃を与える。
        /// 中心ほど深く削れ、そこから離れるほど浅く(弱く)なる。
        /// </summary>
        public void ApplyImpact(Vector3 worldPoint, float radius)
        {
            var localPoint = transform.InverseTransformPoint(worldPoint);

            var scale = transform.lossyScale;
            var uniformScale = Mathf.Max(0.0001f, (scale.x + scale.y + scale.z) / 3f);
            var localRadius = radius / uniformScale;

            _field.ApplyImpact(localPoint, localRadius, localRadius * _digDepthMultiplier);
            RegenerateMesh();
        }

        private void RegenerateMesh()
        {
            var newMesh = MarchingCubesMesher.Generate(_field);

            DestroyMesh(_mesh);

            _mesh = newMesh;
            _meshFilter.sharedMesh = _mesh;
            _meshCollider.sharedMesh = _mesh;
        }

        /// <summary>
        /// Editor の ContextMenu からの Rebuild など、編集モードで呼ばれることもあるため、
        /// 再生中かどうかで Destroy / DestroyImmediate を使い分ける。
        /// </summary>
        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            return TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();
        }
    }
}
