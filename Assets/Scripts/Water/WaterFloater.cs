using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 水面に浮かぶオブジェクト。静止位置(アンカー)における波の変位(上下だけでなく、
    /// 震源から見た水平方向のうねりも含む)を目標地点にして、ばね・ダンパーで追いかける。
    ///
    /// 目標地点へ直接スナップさせるのではなく速度(慣性)を積分するので、波が収まって
    /// 目標地点が静止したあとも、それまでに乗っていた勢いでしばらく揺れてから止まる。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaterFloater : MonoBehaviour
    {
        [Tooltip("追従させる水面。空なら同じシーンの WaterWaveSurface を自動で探す。")]
        [SerializeField] private WaterWaveSurface _surface;

        [Tooltip("静止時に水面からさらにどれだけ上に浮かせるか(物体の半分の厚みぶんなど)。")]
        [SerializeField] private float _floatHeight;

        [Tooltip("波の変位に引き寄せられる強さ(ばね定数)。大きいほど波にきびきび追従する。")]
        [SerializeField] private float _stiffness = 30f;

        [Tooltip("揺れを抑える強さ(減衰)。大きいほど早く静止する。0に近いほどいつまでも揺れ続ける。")]
        [SerializeField] private float _damping = 6f;

        [Tooltip("水平方向(x, z)のうねりをどれだけ強調するか。1で計算どおり、大きいほど横に大きく揺れる。")]
        [SerializeField] private float _horizontalInfluence = 3f;

        [Tooltip("浮かんでいる位置の周りに出す白い泡の半径。")]
        [SerializeField] private float _foamRadius = 0.35f;

        private Vector3 _anchor;
        private Vector3 _velocity;

        /// <summary>浮かんでいる位置の周りに出す白い泡の半径。WaterWaveSurface が毎フレーム参照する。</summary>
        public float FoamRadius => _foamRadius;

        private void Awake()
        {
            if (_surface == null)
            {
                _surface = FindFirstObjectByType<WaterWaveSurface>(FindObjectsInactive.Include);
            }

            _anchor = transform.position;
        }

        private void OnEnable()
        {
            _surface?.RegisterFloater(this);
        }

        private void OnDisable()
        {
            _surface?.UnregisterFloater(this);
        }

        // ばね・ダンパーの数値積分は刻み幅が一定のほうが安定するので FixedUpdate で行う。
        private void FixedUpdate()
        {
            if (_surface == null)
            {
                return;
            }

            var offset = _surface.SampleSurfaceOffset(_anchor);
            offset.x *= _horizontalInfluence;
            offset.z *= _horizontalInfluence;

            var target = _anchor + offset + (Vector3.up * _floatHeight);

            var displacement = target - transform.position;
            var acceleration = (displacement * _stiffness) - (_velocity * _damping);

            var deltaTime = Time.fixedDeltaTime;
            _velocity += acceleration * deltaTime;
            transform.position += _velocity * deltaTime;
        }
    }
}
