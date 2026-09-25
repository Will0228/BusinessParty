using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MixVerse.Home
{
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        private HomeControlGuideView _controlGuideView;
        private HomeTestPlayView _testPlayView;

        [Header("Scratch Transition")]
        // 左上から自動でスクラッチしていく演出。未設定ならフェードのみの従来動作にフォールバックする
        [SerializeField] private GameObject _scratchOverlayRoot;
        [SerializeField] private SimpleBlitCombiner _scratchCombiner;

        [SerializeField] private int _scratchBandCount = 4; // 画面を横に何本の帯へ分けるか

        // 帯 1 本ぶんの高さ（1 / _scratchBandCount）を塗り潰せる大きさが必要。
        // 横は 16:9 の画面でスタンプが丸く見えるよう、縦より小さめにしてある。
        [SerializeField] private Vector2 _scratchStampScale = new Vector2(0.3f, 0.5f);

        [SerializeField] private float _scratchDuration = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _alphaStartProgress = 0.55f; // スクラッチがこの進捗まで進んだらフェードを重ねて開始する
        [SerializeField] private float _fadeDuration = 0.6f;

        public Observable<Unit> OnStartButtonClicked => _startButton.OnClickAsObservable();
        public Observable<Unit> OnQuitButtonClicked => _quitButton.OnClickAsObservable();
        public Observable<Unit> OnGuideButtonClicked
        {
            get
            {
                EnsureControlGuide();
                return _controlGuideView.GuideButton.OnClickAsObservable();
            }
        }

        public Observable<Unit> OnGuideCloseButtonClicked
        {
            get
            {
                EnsureControlGuide();
                return _controlGuideView.CloseButton.OnClickAsObservable();
            }
        }

        public Observable<Unit> OnTestPlayStartRequested
        {
            get
            {
                EnsureTestPlay();
                return _testPlayView.OnStartRequested;
            }
        }

        private bool _isFading;

        // 演出中に隠した Home の中身。Show() でまとめて戻す
        private readonly List<GameObject> _hiddenContents = new List<GameObject>();

        // 演出のあいだだけ使う画面のスクショ。使い終わったら破棄する
        private Texture2D _screenshotTexture;

        private TweenUtility _tweenUtility;
        private KartStageFactory _stageFactory;

        private void Awake() => EnsureControlGuide();

        private void EnsureControlGuide()
        {
            if (_controlGuideView != null)
            {
                return;
            }

            _controlGuideView = GetComponent<HomeControlGuideView>();
            if (_controlGuideView == null)
            {
                _controlGuideView = gameObject.AddComponent<HomeControlGuideView>();
            }

            _controlGuideView.Initialize(_startButton);
        }

        private void EnsureTestPlay()
        {
            if (_testPlayView != null)
            {
                return;
            }

            EnsureControlGuide();
            _testPlayView = GetComponent<HomeTestPlayView>();
            if (_testPlayView == null)
            {
                _testPlayView = gameObject.AddComponent<HomeTestPlayView>();
            }

            _testPlayView.Initialize(_stageFactory, _controlGuideView.TestPlayScreen, _controlGuideView.TestPlayStatus);
        }

        [Inject]
        public void Construct(TweenUtility tweenUtility, KartStageFactory stageFactory)
        {
            _tweenUtility = tweenUtility;
            _stageFactory = stageFactory;
        }

        /// <summary>
        /// ゲーム画面から戻ってきたときに再表示する。
        /// </summary>
        public void Show()
        {
            _isFading = false;
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1.0f;
            EnsureControlGuide();
            _controlGuideView.HideGuide();

            if (_scratchOverlayRoot != null)
            {
                _scratchOverlayRoot.SetActive(false);
            }

            RestoreHiddenContents();
        }

        public void ShowControlGuide()
        {
            EnsureControlGuide();
            _controlGuideView.ShowGuide();
        }

        public void HideControlGuide()
        {
            EnsureControlGuide();
            _controlGuideView.HideGuide();
        }

        public void ShowTestPlay(KartRaceSettings settings)
        {
            EnsureTestPlay();
            _testPlayView.Show(settings);
        }

        public void RenderTestPlay(KartRace race, float gain, bool ready, float countdown, bool midi)
            => _testPlayView.Render(race, gain, ready, countdown, midi);

        public void ResetTestPlay() => _testPlayView.ResetRace();

        public void PlayTestPlayDriftSe() => _testPlayView.PlayDriftSe();

        public void HideTestPlay()
        {
            EnsureTestPlay();
            _testPlayView.Hide();
        }

        public async UniTask StartGameAsync(CancellationToken token)
        {
            if (_isFading)
            {
                return;
            }

            _isFading = true;
            _canvasGroup.alpha = 1.0f;
            EnsureControlGuide();

            var fadeTask = UniTask.CompletedTask;

            if (_scratchOverlayRoot != null && _scratchCombiner != null)
            {
                // ボタンだけはスクラッチの対象外（RawImage に描かれない）なので、
                // スクショを撮る前に消して、そのまま画面から消えたように見せる。
                Hide(_startButton.gameObject);
                Hide(_quitButton.gameObject);
                Hide(_controlGuideView.GuideButton.gameObject);

                // 描画が終わったフレーム末尾でないと画面を取り込めない
                await UniTask.WaitForEndOfFrame(this, token);
                CaptureScreenshot();

                // 撮った画面をそのままオーバーレイに貼り、その上から削っていく。
                // 見た目が同じなので、切り替わった瞬間の変化はない。
                _scratchOverlayRoot.SetActive(true);
                _scratchOverlayRoot.transform.SetAsLastSibling();
                _scratchCombiner.SetSourceTexture(_screenshotTexture);

                // オーバーレイがスクショで画面を肩代わりするので、元の中身は消しておく。
                // 消さないと、削れた穴の下から同じ絵がそのまま見えてしまい 3D の背景まで抜けない。
                HideContentsExceptOverlay();

                // 横帯ごとに左右交互の向きでスタンプを走らせて画面を削っていき、
                // ある程度進んだ（_alphaStartProgress）ところで CanvasGroup のフェードを重ねて開始する。
                await _scratchCombiner.PlayBandWipeAsync(
                    bandCount: _scratchBandCount,
                    stampScale: _scratchStampScale,
                    duration: _scratchDuration,
                    thresholdProgress: _alphaStartProgress,
                    onThresholdReached: () => fadeTask = _tweenUtility.FadeAsync(_canvasGroup, _canvasGroup.alpha, 0.0f, _fadeDuration, token),
                    token: token);
            }
            else
            {
                // スクラッチ演出が未設定の場合は従来通りフェードのみ行う
                fadeTask = _tweenUtility.FadeAsync(_canvasGroup, 1.0f, 0.0f, _fadeDuration, token);
            }

            await fadeTask;

            _isFading = false;
            _canvasGroup.alpha = 0.0f;

            if (_scratchOverlayRoot != null)
            {
                _scratchOverlayRoot.SetActive(false);
            }

            if (_scratchCombiner != null)
            {
                _scratchCombiner.SetSourceTexture(null);
            }

            ReleaseScreenshot();

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 現在の画面（Home の UI と背後の 3D）をそのまま 1 枚のテクスチャとして取り込む。
        /// </summary>
        private void CaptureScreenshot()
        {
            ReleaseScreenshot();
            _screenshotTexture = ScreenCapture.CaptureScreenshotAsTexture();
        }

        private void ReleaseScreenshot()
        {
            if (_screenshotTexture == null)
            {
                return;
            }

            Destroy(_screenshotTexture);
            _screenshotTexture = null;
        }

        private void HideContentsExceptOverlay()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (child == _scratchOverlayRoot)
                {
                    continue;
                }

                Hide(child);
            }
        }

        private void Hide(GameObject content)
        {
            if (content == null || !content.activeSelf)
            {
                return;
            }

            content.SetActive(false);
            _hiddenContents.Add(content);
        }

        private void RestoreHiddenContents()
        {
            foreach (var content in _hiddenContents)
            {
                if (content != null)
                {
                    content.SetActive(true);
                }
            }

            _hiddenContents.Clear();
        }

        private void OnDestroy()
        {
            ReleaseScreenshot();
        }
    }
}
