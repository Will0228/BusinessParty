using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 水面に浮かぶオブジェクト。自分がいる x, z の位置における波の高さを毎フレーム読み取り、
    /// y 座標だけをそれに追従させる(x, z は動かさない)。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaterFloater : MonoBehaviour
    {
        [Tooltip("高さを追従させる水面。空なら同じシーンの WaterWaveSurface を自動で探す。")]
        [SerializeField] private WaterWaveSurface _surface;

        [Tooltip("水面の高さに対して、さらにどれだけ上に浮かせるか(物体の半分の厚みぶんなど)。")]
        [SerializeField] private float _floatHeight;

        private void Awake()
        {
            if (_surface == null)
            {
                _surface = FindFirstObjectByType<WaterWaveSurface>(FindObjectsInactive.Include);
            }
        }

        // 波の高さは水面のメッシュ変形が終わったあとの値を読みたいので LateUpdate で追従させる。
        private void LateUpdate()
        {
            if (_surface == null)
            {
                return;
            }

            var position = transform.position;
            position.y = _surface.SampleHeight(position) + _floatHeight;
            transform.position = position;
        }
    }
}
