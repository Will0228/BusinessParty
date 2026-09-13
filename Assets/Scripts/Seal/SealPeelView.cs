using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Seal
{
    /// <summary>
    /// シールはがし演出の見た目を担当するオーバーレイ。
    ///
    /// SealPeelShaderURP のマテリアルへ、はがれる側／はがした後に見える側の
    /// 2 枚のテクスチャと進捗を渡すだけで、実際のめくれ方の計算はシェーダー側が担当する。
    /// 表示する範囲（矩形の大きさ・中心位置）は SetArea で決める。
    /// </summary>
    public sealed class SealPeelView : MonoBehaviour
    {
        private static readonly int BottomTexId = Shader.PropertyToID("_BottomTex");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int PaddingId = Shader.PropertyToID("_Padding");

        [SerializeField] private RawImage _image;
        [SerializeField] private Material _material;

        [Tooltip("シールをめくった直後に一瞬見える、紙自体の裏面の色。シルバーの箔などを想定。")]
        [SerializeField] private Color _backColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private Material _materialInstance;

        private void Awake()
        {
            EnsureMaterialInstance();
        }

        /// <summary>
        /// 演出を表示する範囲を、画面中心からのオフセットとピクセルサイズで決める。
        /// </summary>
        public void SetArea(Vector2 size, Vector2 center)
        {
            if (_image == null)
            {
                return;
            }

            var rectTransform = _image.rectTransform;
            var padding = EnsureMaterialInstance() ? _materialInstance.GetFloat(PaddingId) : 0f;
            rectTransform.sizeDelta = size * (1f + 2f * padding);
            rectTransform.anchoredPosition = center;
            UpdateAspect();
        }

        public void SetTextures(Texture top, Texture bottom)
        {
            if (!EnsureMaterialInstance())
            {
                return;
            }

            // RawImage は自身の texture を CanvasRenderer 経由でシェーダーの _MainTex（メインテクスチャ）へ
            // 反映する。マテリアルへ直接 SetTexture しても毎フレーム上書きされてしまうため、
            // Top（メインテクスチャ）はこちらへ設定する。
            _image.texture = top;
            _materialInstance.SetTexture(BottomTexId, bottom);
        }

        public void SetProgress(float progress)
        {
            if (!EnsureMaterialInstance())
            {
                return;
            }

            _materialInstance.SetFloat(ProgressId, Mathf.Clamp01(progress));
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateAspect();
        }

        private void UpdateAspect()
        {
            if (_materialInstance == null || _image == null)
            {
                return;
            }

            var size = _image.rectTransform.rect.size;
            _materialInstance.SetFloat(AspectId, Mathf.Max(0.01f, size.x / Mathf.Max(1f, size.y)));
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// マテリアルの複製をまだ作っていなければ作る。
        ///
        /// 非アクティブな状態でシーンに置かれているオーバーレイは Awake がまだ呼ばれていないことがある
        /// （Unity は非アクティブな GameObject の Awake を、アクティブ化されるまで遅延させるため）。
        /// SetTextures / SetProgress は Show より先に呼ばれる想定なので、呼び出し順に依存せず
        /// 自分で初期化を保証できるようにしておく。
        /// </summary>
        /// <returns>マテリアルの複製が用意できていれば true。</returns>
        private bool EnsureMaterialInstance()
        {
            if (_materialInstance != null)
            {
                return true;
            }

            if (_image == null || _material == null)
            {
                Debug.LogError("[MixVerse] SealPeelView に RawImage かマテリアルが設定されていません。", this);
                return false;
            }

            // アセットを直接書き換えると、同じマテリアルを使う他の SealManager まで一緒に剥がれてしまうため複製する
            _materialInstance = new Material(_material)
            {
                name = _material.name + " (Instance)",
            };
            _image.material = _materialInstance;

            _materialInstance.SetColor(BackColorId, _backColor);
            UpdateAspect();

            return true;
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}
