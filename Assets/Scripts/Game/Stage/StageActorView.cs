using UnityEngine;

namespace MixVerse.Game.Stage
{
    /// <summary>
    /// ステージに立つ Capsule 1 体。拍や判定に合わせて軽く跳ねる。
    /// </summary>
    public sealed class StageActorView : MonoBehaviour
    {
        private const float BounceSeconds = 0.22f;

        private Transform _body;
        private Vector3 _restLocalPosition;
        private float _bounceHeight;
        private float _elapsed = BounceSeconds;

        public void Initialize(Transform body, float bounceHeight)
        {
            _body = body;
            _restLocalPosition = body.localPosition;
            _bounceHeight = bounceHeight;
            _elapsed = BounceSeconds;
        }

        public void Bounce() => _elapsed = 0f;

        private void Update()
        {
            if (_body == null || _elapsed >= BounceSeconds)
            {
                return;
            }

            _elapsed += Time.deltaTime;

            // 跳ね終わりに sin が 0 へ戻るので、明示的に戻さなくても元の高さに収まる
            var rate = Mathf.Clamp01(_elapsed / BounceSeconds);
            _body.localPosition = _restLocalPosition + (Vector3.up * (Mathf.Sin(rate * Mathf.PI) * _bounceHeight));
        }
    }
}
