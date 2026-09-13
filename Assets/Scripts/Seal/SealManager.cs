using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse;
using UnityEngine;

namespace MixVerse.Seal
{
    /// <summary>
    /// ObjectGroupA をシールに見立てて剥がし、下から ObjectGroupB が現れる演出。
    ///
    /// 3D のまま剥がす代わりに、剥がす前後の見た目をそれぞれ 1 枚の画像として撮っておき、
    /// 2D 上でめくり合成してから 3D 側を入れ替える。ObjectGroupA と ObjectGroupB は
    /// 同じ位置・画角に収まるように重ねて置いておくこと。
    ///
    /// 撮影から 2D への切り替えまでは同じフレーム内（await を挟まず）に完結させている。
    /// Unity 本来のフレーム描画はスクリプト処理がすべて終わったあとに 1 回だけ走るため、
    /// ObjectGroupB を撮影用に一瞬だけ有効化しても実際の画面には映り込まない。
    /// </summary>
    public sealed class SealManager : MonoBehaviour
    {
        [Header("Objects")]
        [Tooltip("はがれる側。最初はこちらが表示されている。")]
        [SerializeField] private GameObject _objectGroupA;

        [Tooltip("はがした後に現れる側。ObjectGroupA と同じ位置・画角に収まるよう重ねて置く。")]
        [SerializeField] private GameObject _objectGroupB;

        [Tooltip("撮影に使うカメラ。未設定なら Camera.main を使う。")]
        [SerializeField] private Camera _camera;

        [Header("View")]
        [SerializeField] private SealPeelView _peelView;

        [Header("Area")]
        [Tooltip("演出を表示する範囲の幅・高さ（ピクセル）。")]
        [SerializeField] private Vector2 _areaSize = new Vector2(400f, 400f);

        [Tooltip("演出を表示する範囲の中心。画面中心からのオフセット（ピクセル）。")]
        [SerializeField] private Vector2 _areaCenter = Vector2.zero;

        [Header("Timing")]
        [SerializeField] private float _duration = 1.2f;

        [SerializeField] private bool _playOnStart;

        private readonly SealSnapshotCapturer _capturer = new();
        private TweenUtility _tweenUtility;
        private CancellationTokenSource _playCts;

        /// <summary>演出を再生中かどうか。多重再生を避けるための判定に使う。</summary>
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _tweenUtility = new TweenUtility();

            // 開始前は A だけが実体で、B は入れ替わりに備えて無効にしておく
            SetActiveSafe(_objectGroupA, true);
            SetActiveSafe(_objectGroupB, false);
        }

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
        }

        /// <summary>シールはがしを再生する。多重再生はしない。</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (IsPlaying)
            {
                return;
            }

            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = new CancellationTokenSource();

            PlayAsync(_playCts.Token).Forget();
        }

        /// <summary>完了まで待てる版。ゲーム側の演出シーケンスに組み込むときはこちらを使う。</summary>
        public async UniTask PlayAsync(CancellationToken token)
        {
            if (IsPlaying)
            {
                return;
            }

            var camera = GetCamera();

            if (camera == null || _objectGroupA == null || _objectGroupB == null || _peelView == null)
            {
                Debug.LogError("[MixVerse] SealManager の参照が不足しているため、演出を中止しました。", this);
                return;
            }

            IsPlaying = true;

            RenderTexture topTexture = null;
            RenderTexture bottomTexture = null;

            try
            {
                var pixelRect = ComputeAreaPixelRect(camera);

                // A だけを表に出して撮る
                SetActiveSafe(_objectGroupA, true);
                SetActiveSafe(_objectGroupB, false);
                topTexture = _capturer.Capture(camera, camera.cullingMask, pixelRect);

                // 入れ替えて B だけを撮る
                SetActiveSafe(_objectGroupA, false);
                SetActiveSafe(_objectGroupB, true);
                bottomTexture = _capturer.Capture(camera, camera.cullingMask, pixelRect);

                // ここからは 2D 側が画面を覆うので、3D の実体はいったん両方隠しておく
                SetActiveSafe(_objectGroupB, false);

                _peelView.SetArea(_areaSize, _areaCenter);
                _peelView.SetTextures(topTexture, bottomTexture);
                _peelView.SetProgress(0f);
                _peelView.Show();

                await _tweenUtility.ValueAsync(0f, 1f, _duration, token, _peelView.SetProgress);

                _peelView.Hide();

                // はがし終わったので 3D 側を入れ替える。A はもう使わないので隠したままにする。
                SetActiveSafe(_objectGroupB, true);
            }
            finally
            {
                if (topTexture != null)
                {
                    RenderTexture.ReleaseTemporary(topTexture);
                }

                if (bottomTexture != null)
                {
                    RenderTexture.ReleaseTemporary(bottomTexture);
                }

                IsPlaying = false;
            }
        }

        /// <summary>
        /// 画面中心を基準に、インスペクターで指定した幅・高さ・中心オフセットからピクセル矩形を求める。
        /// </summary>
        private Rect ComputeAreaPixelRect(Camera camera)
        {
            var width = Mathf.Max(1f, _areaSize.x);
            var height = Mathf.Max(1f, _areaSize.y);
            var centerX = (camera.pixelWidth * 0.5f) + _areaCenter.x;
            var centerY = (camera.pixelHeight * 0.5f) + _areaCenter.y;

            return new Rect(centerX - (width * 0.5f), centerY - (height * 0.5f), width, height);
        }

        private Camera GetCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            return _camera;
        }

        private void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
