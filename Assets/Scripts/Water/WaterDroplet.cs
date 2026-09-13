using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 水面へ落ちる水滴。着水すると自分の大きさと落下速度を WaterWaveSurface に伝えて波紋を起こし、消える。
    ///
    /// Rigidbody で物理的に落とす想定だが、Rigidbody が無ければ Transform の移動量から
    /// 自分で落下速度を計算するので、Tween などで動かす水滴にもそのまま使える。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WaterDroplet : MonoBehaviour
    {
        [Tooltip("波紋を起こす水面。空なら衝突した相手から WaterWaveSurface を探す。")]
        [SerializeField] private WaterWaveSurface _targetSurface;

        [Tooltip("水滴の半径。0 以下なら自分の SphereCollider の半径(ワールドスケール込み)を使う。")]
        [SerializeField] private float _radius;

        [Tooltip("着水したら自分を消す。")]
        [SerializeField] private bool _destroyOnImpact = true;

        private Rigidbody _rigidbody;
        private Vector3 _previousPosition;
        private float _trackedSpeed;
        private bool _hasSplashed;

        public void SetTargetSurface(WaterWaveSurface surface)
        {
            _targetSurface = surface;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _previousPosition = transform.position;
        }

        private void Update()
        {
            // Rigidbody を持たない(Tween などで動かす)水滴のぶんだけ、自前で落下速度を追う
            if (_rigidbody != null)
            {
                return;
            }

            _trackedSpeed = (transform.position - _previousPosition).magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
            _previousPosition = transform.position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            TrySplash(collision.collider, collision.GetContact(0).point);
        }

        private void OnTriggerEnter(Collider other)
        {
            TrySplash(other, transform.position);
        }

        private void TrySplash(Collider hitCollider, Vector3 contactPoint)
        {
            if (_hasSplashed)
            {
                return;
            }

            var surface = _targetSurface != null ? _targetSurface : hitCollider.GetComponentInParent<WaterWaveSurface>();

            if (surface == null)
            {
                return;
            }

            _hasSplashed = true;
            surface.Splash(contactPoint, GetRadius(), GetImpactSpeed());

            if (_destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }

        private float GetRadius()
        {
            if (_radius > 0f)
            {
                return _radius;
            }

            if (TryGetComponent<SphereCollider>(out var sphere))
            {
                var scale = transform.lossyScale;
                return sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            }

            return 0.05f;
        }

        private float GetImpactSpeed()
        {
            return _rigidbody != null ? _rigidbody.linearVelocity.magnitude : _trackedSpeed;
        }
    }
}
