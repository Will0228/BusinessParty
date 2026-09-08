using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 触れる側（オブジェクト A）に付けて、CrossSectionClipVolume に重なった部分を切り抜く。
    /// Renderer のマテリアルは CrossSectionClipShader を使っている必要がある。
    ///
    /// 体積の情報は MaterialPropertyBlock で Renderer ごとに渡す。マテリアルのアセットを
    /// 直接書き換えると同じマテリアルを共有する他のオブジェクトまで一緒に切れてしまい、
    /// 複製して渡すと [ExecuteAlways] でエディタ中に複製が溜まり続けるため。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CrossSectionClipTarget : MonoBehaviour
    {
        private static readonly int ClipEnabledId = Shader.PropertyToID("_ClipEnabled");
        private static readonly int ClipShapeId = Shader.PropertyToID("_ClipShape");
        private static readonly int ClipInsideId = Shader.PropertyToID("_ClipInside");
        private static readonly int ClipVolumeCenterId = Shader.PropertyToID("_ClipVolumeCenter");
        private static readonly int ClipVolumeAxisXId = Shader.PropertyToID("_ClipVolumeAxisX");
        private static readonly int ClipVolumeAxisYId = Shader.PropertyToID("_ClipVolumeAxisY");
        private static readonly int ClipVolumeAxisZId = Shader.PropertyToID("_ClipVolumeAxisZ");
        private static readonly int ClipVolumeExtentsId = Shader.PropertyToID("_ClipVolumeExtents");

        [Tooltip("触れられる側（オブジェクト B）。ここに重なった部分が消える。")]
        [SerializeField] private CrossSectionClipVolume _volume;

        [Tooltip("空なら自分と子の Renderer をまとめて対象にする。")]
        [SerializeField] private Renderer[] _renderers;

        [Tooltip("体積の内側を消す。外すと逆になり、体積に入った部分だけが残る。")]
        [SerializeField] private bool _hideInside = true;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>いま体積に触れているか。触れていないあいだは切り抜きも発光もしない。</summary>
        public bool IsTouching { get; private set; }

        public CrossSectionClipVolume Volume
        {
            get => _volume;
            set => _volume = value;
        }

        public bool HideInside
        {
            get => _hideInside;
            set => _hideInside = value;
        }

        private void OnEnable()
        {
            CacheRenderers();
        }

        private void OnDisable()
        {
            ClearOverrides();
        }

        private void OnValidate()
        {
            CacheRenderers();
        }

        // 体積も対象も動くので、両方の移動が終わったあとに書き込む。
        private void LateUpdate()
        {
            Apply();
        }

        private void CacheRenderers()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Apply()
        {
            _propertyBlock ??= new MaterialPropertyBlock();

            var hasVolume = _volume != null && _volume.isActiveAndEnabled;
            var volumeTransform = hasVolume ? _volume.transform : null;
            var touching = false;

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var contact = hasVolume && _volume.Intersects(renderer.bounds);
                touching |= contact;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(ClipEnabledId, contact ? 1f : 0f);

                if (contact)
                {
                    _propertyBlock.SetFloat(ClipShapeId, (float)_volume.Shape);
                    _propertyBlock.SetFloat(ClipInsideId, _hideInside ? 1f : 0f);
                    _propertyBlock.SetVector(ClipVolumeCenterId, volumeTransform.position);
                    _propertyBlock.SetVector(ClipVolumeAxisXId, volumeTransform.right);
                    _propertyBlock.SetVector(ClipVolumeAxisYId, volumeTransform.up);
                    _propertyBlock.SetVector(ClipVolumeAxisZId, volumeTransform.forward);
                    _propertyBlock.SetVector(ClipVolumeExtentsId, _volume.WorldExtents);
                }

                renderer.SetPropertyBlock(_propertyBlock);
            }

            IsTouching = touching;
        }

        /// <summary>切り抜きを止めて元の見た目に戻す。</summary>
        private void ClearOverrides()
        {
            if (_renderers == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();

            foreach (var renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(ClipEnabledId, 0f);
                renderer.SetPropertyBlock(_propertyBlock);
            }

            IsTouching = false;
        }
    }
}
