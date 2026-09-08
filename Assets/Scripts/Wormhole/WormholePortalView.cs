using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MixVerse.Wormhole
{
    /// <summary>
    /// ワームホールの片方の口。
    /// transform の +Z 側が「表」で、表から覗くと相方の表側の景色が見える。
    ///
    /// 向こう側を撮るカメラと、その絵を貯める RenderTexture をここで抱える。
    /// どこをどう撮るかは <see cref="WormholeView"/> が決める。
    /// </summary>
    public sealed class WormholePortalView : MonoBehaviour
    {
        private static readonly int PortalTexPropertyId = Shader.PropertyToID("_PortalTex");

        [Tooltip("穴の面を描くレンダラー。WormholeShader のマテリアルを割り当てておく。")]
        [SerializeField] private Renderer _renderer;

        private Camera _portalCamera;
        private Material _material;
        private RenderTexture _frontBuffer;
        private RenderTexture _backBuffer;
        private bool _isHdr;

        /// <summary>向こう側を撮るカメラ。</summary>
        public Camera PortalCamera => _portalCamera;

        /// <summary>次に描き込むバッファ。今映しているものとは別の 1 枚。</summary>
        public RenderTexture BackBuffer => _backBuffer;

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }
        }

        private void OnDestroy()
        {
            ReleaseTextures();

            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        /// <summary>
        /// 撮影用カメラとマテリアルの実体を用意する。二度目以降は何もしない。
        /// </summary>
        public void Initialize()
        {
            if (_portalCamera != null)
            {
                return;
            }

            // Renderer.material は複製を返す。共有マテリアルのままだと
            // A と B が同じ RenderTexture を映してしまう。
            if (_renderer != null)
            {
                _material = _renderer.material;
            }

            var cameraObject = new GameObject($"{name}Camera");
            cameraObject.transform.SetParent(transform, false);

            _portalCamera = cameraObject.AddComponent<Camera>();

            // 通常の描画順では回さず、WormholeView から明示的に描かせる
            _portalCamera.enabled = false;

            var additionalData = _portalCamera.GetUniversalAdditionalCameraData();
            additionalData.renderType = CameraRenderType.Base;

            // ポストプロセスは穴の面を貼ったあとに覗く側のカメラがまとめてかける。
            // ここでかけると、ブルームやトーンマップが二重になってしまう。
            additionalData.renderPostProcessing = false;
        }

        /// <summary>
        /// 覗く側と同じ大きさの描画先を用意する。大きさが変わったときだけ作り直す。
        /// </summary>
        public void EnsureTextures(int width, int height, bool allowHdr)
        {
            if (_frontBuffer != null
                && _frontBuffer.width == width
                && _frontBuffer.height == height
                && _isHdr == allowHdr)
            {
                return;
            }

            ReleaseTextures();

            _isHdr = allowHdr;
            _frontBuffer = CreateTexture(width, height);
            _backBuffer = CreateTexture(width, height);

            ApplyDisplayTexture();
        }

        /// <summary>
        /// 覗く側のカメラと同じ写り方になるよう、画角や距離の設定をそろえる。
        /// </summary>
        public void SyncCameraSettings(Camera viewer, int cullingMask)
        {
            _portalCamera.orthographic = viewer.orthographic;
            _portalCamera.orthographicSize = viewer.orthographicSize;
            _portalCamera.fieldOfView = viewer.fieldOfView;
            _portalCamera.aspect = viewer.aspect;
            _portalCamera.nearClipPlane = viewer.nearClipPlane;
            _portalCamera.farClipPlane = viewer.farClipPlane;
            _portalCamera.clearFlags = viewer.clearFlags;
            _portalCamera.backgroundColor = viewer.backgroundColor;
            _portalCamera.allowHDR = viewer.allowHDR;
            _portalCamera.cullingMask = cullingMask;
        }

        /// <summary>描き終わったバッファを表に出す。</summary>
        public void SwapBuffers()
        {
            var swapped = _frontBuffer;
            _frontBuffer = _backBuffer;
            _backBuffer = swapped;

            ApplyDisplayTexture();
        }

        /// <summary>覗く側のカメラに穴が映っているか。映っていないなら撮る必要がない。</summary>
        public bool IsVisibleFrom(Plane[] frustumPlanes)
        {
            return _renderer != null
                && _renderer.enabled
                && GeometryUtility.TestPlanesAABB(frustumPlanes, _renderer.bounds);
        }

        private RenderTexture CreateTexture(int width, int height)
        {
            var format = _isHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            var texture = new RenderTexture(width, height, 24, format)
            {
                name = $"{name}Buffer",
                // 歪ませたぶん画面外を拾いにいくので、端の色を引き伸ばして誤魔化す
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
            };

            texture.Create();

            return texture;
        }

        private void ApplyDisplayTexture()
        {
            if (_material == null)
            {
                return;
            }

            _material.SetTexture(PortalTexPropertyId, _frontBuffer);
        }

        private void ReleaseTextures()
        {
            if (_frontBuffer != null)
            {
                _frontBuffer.Release();
                Destroy(_frontBuffer);
                _frontBuffer = null;
            }

            if (_backBuffer != null)
            {
                _backBuffer.Release();
                Destroy(_backBuffer);
                _backBuffer = null;
            }
        }
    }
}
