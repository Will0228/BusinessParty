using UnityEngine;

namespace MixVerse
{
    /// <summary>切り取る形。CrossSectionClipShader の _ClipShape と値をそろえている。</summary>
    public enum CrossSectionClipShape
    {
        Plane = 0,
        Box = 1,
        Sphere = 2,
    }

    /// <summary>
    /// 触れられる側（オブジェクト B）に付けて、相手をどこで切るかを表す体積。
    ///
    /// 見た目は持たず、Transform と形だけを CrossSectionClipTarget へ渡す。
    /// Cube / Sphere のプリミティブにそのまま付ければ、見えている大きさと切り口が一致する。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CrossSectionClipVolume : MonoBehaviour
    {
        [SerializeField] private CrossSectionClipShape _shape = CrossSectionClipShape.Box;

        [Tooltip("ローカルの大きさ。1 のままなら Unity の Cube / Sphere の見た目とちょうど一致する。")]
        [SerializeField] private Vector3 _size = Vector3.one;

        public CrossSectionClipShape Shape
        {
            get => _shape;
            set => _shape = value;
        }

        public Vector3 Size
        {
            get => _size;
            set => _size = value;
        }

        /// <summary>ワールドでの半径。Box なら各軸の半分の長さ。</summary>
        public Vector3 WorldExtents
        {
            get
            {
                var scale = transform.lossyScale;

                return new Vector3(
                    Mathf.Abs(_size.x * scale.x),
                    Mathf.Abs(_size.y * scale.y),
                    Mathf.Abs(_size.z * scale.z)) * 0.5f;
            }
        }

        /// <summary>Plane のときの切断面の法線。体積の Y 軸を使う。</summary>
        public Vector3 PlaneNormal => transform.up;

        /// <summary>
        /// 相手の AABB がこの体積に触れているか。
        ///
        /// 実際の切り口はシェーダーがピクセル単位で出すので、ここは触れ始めを拾えれば十分。
        /// 取りこぼすと発光が遅れて出るため、判定は広めに倒している。
        /// </summary>
        public bool Intersects(Bounds worldBounds)
        {
            switch (_shape)
            {
                case CrossSectionClipShape.Plane:
                {
                    var normal = PlaneNormal;
                    var extents = worldBounds.extents;
                    var reach =
                        Mathf.Abs(normal.x) * extents.x +
                        Mathf.Abs(normal.y) * extents.y +
                        Mathf.Abs(normal.z) * extents.z;

                    return Mathf.Abs(Vector3.Dot(worldBounds.center - transform.position, normal)) <= reach;
                }

                case CrossSectionClipShape.Sphere:
                {
                    var extents = WorldExtents;
                    var radius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));

                    return (worldBounds.ClosestPoint(transform.position) - transform.position).sqrMagnitude
                           <= radius * radius;
                }

                default:
                    return worldBounds.Intersects(CreateEnclosingBounds());
            }
        }

        /// <summary>回転した体積を包む AABB。回した箱をそのまま Bounds では表せないので外接させる。</summary>
        private Bounds CreateEnclosingBounds()
        {
            var extents = WorldExtents;
            var right = transform.right;
            var up = transform.up;
            var forward = transform.forward;

            var half = new Vector3(
                Mathf.Abs(right.x) * extents.x + Mathf.Abs(up.x) * extents.y + Mathf.Abs(forward.x) * extents.z,
                Mathf.Abs(right.y) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(forward.y) * extents.z,
                Mathf.Abs(right.z) * extents.x + Mathf.Abs(up.z) * extents.y + Mathf.Abs(forward.z) * extents.z);

            return new Bounds(transform.position, half * 2f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            var extents = WorldExtents;

            switch (_shape)
            {
                case CrossSectionClipShape.Plane:
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(extents.x * 4f, 0f, extents.z * 4f));
                    Gizmos.DrawLine(Vector3.zero, Vector3.up * Mathf.Max(extents.y, 0.5f));
                    break;

                case CrossSectionClipShape.Sphere:
                    Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)));
                    break;

                default:
                    Gizmos.DrawWireCube(Vector3.zero, extents * 2f);
                    break;
            }
        }
    }
}
