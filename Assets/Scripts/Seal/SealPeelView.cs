using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Seal
{
    /// <summary>
    /// シールはがし演出の見た目を担当する全画面オーバーレイ。
    ///
    /// SealPeelShaderURP のマテリアルへ、はがれる側／はがした後に見える側の
    /// 2 枚のテクスチャと進捗を渡すだけで、実際のめくれ方の計算はシェーダー側が担当する。
    /// </summary>
    public sealed class SealPeelView : MonoBehaviour
    {
        private static readonly int TopTexId = Shader.PropertyToID("_TopTex");
        private static readonly int BottomTexId = Shader.PropertyToID("_BottomTex");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private RawImage _image;
        [SerializeField] private Material _material;

        private Material _materialInstance;

        private void Awake()
        {
            if (_image == null || _material == null)
            {
                Debug.LogError("[MixVerse] SealPeelView に RawImage かマテリアルが設定されていません。", this);
                enabled = false;
                return;
            }

            // アセットを直接書き換えると、同じマテリアルを使う他の SealManager まで一緒に剥がれてしまうため複製する
            _materialInstance = new Material(_material);
            _image.material = _materialInstance;
        }

        public void SetTextures(Texture top, Texture bottom)
        {
            if (_materialInstance == null)
            {
                return;
            }

            _materialInstance.SetTexture(TopTexId, top);
            _materialInstance.SetTexture(BottomTexId, bottom);
        }

        public void SetProgress(float progress)
        {
            if (_materialInstance == null)
            {
                return;
            }

            _materialInstance.SetFloat(ProgressId, Mathf.Clamp01(progress));
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}
