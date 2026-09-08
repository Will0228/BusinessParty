using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MixVerse.Wormhole
{
    /// <summary>
    /// 離れた 2 地点をつなぐワームホール。
    /// A から覗くと B のまわりが、B から覗くと A のまわりが見える。
    ///
    /// やっていることは「向こう側にカメラを置いて撮った絵を、穴の形に切り抜いて貼る」だけ。
    /// ただし覗く側のカメラを入口のローカル空間へ移し、そのまま出口のローカル空間へ
    /// 置き直しているので、首を振れば見える範囲もそのままずれる。継ぎ目のない窓になる。
    ///
    /// 撮影はカメラを毎フレーム動かして 1 回ずつ描くだけなので、
    /// レイトレーシングのように専用のハードウェアや対応シェーダーを要求しない。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class WormholeView : MonoBehaviour
    {
        [SerializeField] private WormholePortalView _portalA;
        [SerializeField] private WormholePortalView _portalB;

        [Tooltip("覗く側のカメラ。未設定なら Camera.main を使う。")]
        [SerializeField] private Camera _viewerCamera;

        [Header("Rendering")]
        [Tooltip("向こう側を撮るカメラが写すレイヤー。")]
        [SerializeField] private LayerMask _cullingMask = ~0;

        [Tooltip("向こう側の絵の解像度。画面解像度に対する倍率。重いときは下げる。")]
        [Range(0.25f, 1f)]
        [SerializeField] private float _resolutionScale = 1f;

        [Tooltip("出口の面ぎりぎりに置いた物が写り込むときだけ増やす。")]
        [SerializeField] private float _clipPlaneOffset = 0.05f;

        private readonly Plane[] _frustumPlanes = new Plane[6];

        private readonly UniversalRenderPipeline.SingleCameraRequest _renderRequest =
            new UniversalRenderPipeline.SingleCameraRequest();

        private WormholeMath _wormholeMath;

        /// <summary>片方の口。もう片方を覗くと、こちらのまわりが見える。</summary>
        public WormholePortalView PortalA => _portalA;

        /// <summary>もう片方の口。</summary>
        public WormholePortalView PortalB => _portalB;

        private void Awake()
        {
            _wormholeMath = new WormholeMath();
        }

        // 覗く側のカメラが最終的な位置に落ち着いてから撮りたいので LateUpdate で回す。
        private void LateUpdate()
        {
            var viewer = GetViewerCamera();

            if (viewer == null || _portalA == null || _portalB == null)
            {
                return;
            }

            _portalA.Initialize();
            _portalB.Initialize();

            var width = Mathf.Max(1, Mathf.RoundToInt(viewer.pixelWidth * _resolutionScale));
            var height = Mathf.Max(1, Mathf.RoundToInt(viewer.pixelHeight * _resolutionScale));

            GeometryUtility.CalculateFrustumPlanes(viewer, _frustumPlanes);

            RenderThroughPortal(_portalB, _portalA, viewer, width, height);
            RenderThroughPortal(_portalA, _portalB, viewer, width, height);
        }

        private void RenderThroughPortal(
            WormholePortalView entrance,
            WormholePortalView exit,
            Camera viewer,
            int width,
            int height)
        {
            if (!entrance.IsVisibleFrom(_frustumPlanes))
            {
                return;
            }

            entrance.EnsureTextures(width, height, viewer.allowHDR);
            entrance.SyncCameraSettings(viewer, _cullingMask.value);

            var portalCamera = entrance.PortalCamera;
            var warp = _wormholeMath.GetWarpMatrix(entrance.transform, exit.transform);
            _wormholeMath.GetVirtualPose(warp, viewer.transform, out var position, out var rotation);
            portalCamera.transform.SetPositionAndRotation(position, rotation);

            // 表から覗いているなら出口の表側だけを、裏から覗いているなら裏側だけを写す。
            // これがないと、出口の手前に立っている物が空中に浮いたまま見えてしまう。
            var side = _wormholeMath.GetViewerSide(entrance.transform, viewer.transform.position);
            var clipNormal = exit.transform.forward * side;

            portalCamera.ResetProjectionMatrix();

            var clipPlane = _wormholeMath.GetCameraSpaceClipPlane(
                portalCamera, exit.transform.position, clipNormal, _clipPlaneOffset);

            portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipPlane);

            // 今映しているのとは別の 1 枚に描いてから入れ替える。
            // 同じ 1 枚を読みながら書くと、向こう側に写り込んだ穴の中身が壊れる。
            // 入れ替え方式なら 1 フレームごとに 1 段ずつ入れ子が深まり、
            // 向かい合わせに置いたときの合わせ鏡のようなトンネルもそのまま出る。
            _renderRequest.destination = entrance.BackBuffer;

            if (!RenderPipeline.SupportsRenderRequest(portalCamera, _renderRequest))
            {
                return;
            }

            RenderPipeline.SubmitRenderRequest(portalCamera, _renderRequest);
            entrance.SwapBuffers();
        }

        private Camera GetViewerCamera()
        {
            if (_viewerCamera == null)
            {
                _viewerCamera = Camera.main;
            }

            return _viewerCamera;
        }
    }
}
