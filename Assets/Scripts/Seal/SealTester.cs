using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse.Seal
{
    /// <summary>
    /// キー入力で SealManager を再生する動作確認用のコンポーネント。既定では Space キーで再生する。
    /// </summary>
    public sealed class SealTester : MonoBehaviour
    {
        [SerializeField] private SealManager _sealManager;
        [SerializeField] private Key _playKey = Key.Space;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _sealManager == null)
            {
                return;
            }

            if (keyboard[_playKey].wasPressedThisFrame)
            {
                Debug.Log("[MixVerse] Seal 演出を再生します。");
                _sealManager.Play();
            }
        }
    }
}
