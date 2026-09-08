using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse
{
    /// <summary>
    /// 断面の見え方を確かめるための動作確認用のコンポーネント。
    /// 対象を体積へ出し入れしながら往復させ、キーで切り方を切り替える。
    ///
    /// Space: 往復の停止／再開
    /// H: 消す側を内側と外側で反転
    /// 1 / 2 / 3: 体積の形を Plane / Box / Sphere に変更
    /// </summary>
    public sealed class CrossSectionClipTester : MonoBehaviour
    {
        [SerializeField] private CrossSectionClipTarget _target;
        [SerializeField] private CrossSectionClipVolume _volume;

        [Header("Sweep")]
        [SerializeField] private bool _autoSweep = true;

        [SerializeField] private float _sweepSpeed = 1.2f;

        [Tooltip("体積の中心を通り過ぎる量。1 で中心まで、2 で反対側の同じ距離まで進む。")]
        [SerializeField] private float _sweepOvershoot = 2f;

        private Vector3 _sweepOrigin;
        private bool _wasTouching;

        private void Start()
        {
            if (_target == null || _volume == null)
            {
                Debug.LogWarning("[MixVerse] CrossSectionClipTester に対象と体積が設定されていません。");
                enabled = false;
                return;
            }

            _sweepOrigin = _target.transform.position;
        }

        private void Update()
        {
            ReadKeys();

            if (_autoSweep)
            {
                Sweep();
            }

            if (_target.IsTouching != _wasTouching)
            {
                _wasTouching = _target.IsTouching;
                Debug.Log($"[MixVerse] 断面: {(_wasTouching ? "接触中" : "非接触")}");
            }
        }

        private void ReadKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _autoSweep = !_autoSweep;
                Debug.Log($"[MixVerse] 往復: {(_autoSweep ? "再開" : "停止")}");
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                _target.HideInside = !_target.HideInside;
                Debug.Log($"[MixVerse] 消す側: {(_target.HideInside ? "体積の内側" : "体積の外側")}");
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SetShape(CrossSectionClipShape.Plane);
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SetShape(CrossSectionClipShape.Box);
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SetShape(CrossSectionClipShape.Sphere);
            }
        }

        private void SetShape(CrossSectionClipShape shape)
        {
            _volume.Shape = shape;
            Debug.Log($"[MixVerse] 体積の形: {shape}");
        }

        /// <summary>
        /// 開始位置から体積の中心へ向かって往復させる。
        /// 向きを体積との位置関係から取るので、対象をどこに置いても必ず体積を突き抜ける。
        /// </summary>
        private void Sweep()
        {
            var toVolume = _volume.transform.position - _sweepOrigin;
            var progress = 0.5f - 0.5f * Mathf.Cos(Time.time * _sweepSpeed);

            _target.transform.position = _sweepOrigin + toVolume * (progress * _sweepOvershoot);
        }
    }
}
