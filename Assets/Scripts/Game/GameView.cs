using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Cpu;
using MixVerse.Game.Player;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace MixVerse.Game
{
    public sealed class GameView : MonoBehaviour
    {
        [SerializeField] private PartyGameSettings _settings;
        [SerializeField] private PlayerView _playerView;
        [SerializeField] private CpuView _juniorView;
        [SerializeField] private CpuView _seniorView;
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CanvasGroup _fadeOverlayGroup;
        [SerializeField] private TextMeshProUGUI _turnLabel;
        [SerializeField] private TextMeshProUGUI _resultLabel;
        [SerializeField] private float _fadeDuration = 0.6f;

        private readonly Subject<Unit> _onExitRequested = new Subject<Unit>();
        private TweenUtility _tween;

        public PartyGameSettings Settings => _settings;
        public PlayerView PlayerView => _playerView;
        public CpuView JuniorView => _juniorView;
        public CpuView SeniorView => _seniorView;
        public Observable<Unit> OnExitRequested => _onExitRequested;

        [Inject]
        public void Construct(TweenUtility tween) => _tween = tween;

        public async UniTask ShowAsync(CancellationToken token)
        {
            gameObject.SetActive(true);
            _resultLabel.gameObject.SetActive(false);
            if (_bgmSource != null) _bgmSource.Play();
            await UniTask.WhenAll(
                _tween.FadeAsync(_fadeOverlayGroup, 1, 0, _fadeDuration, token),
                _tween.FadeAsync(_canvasGroup, 0, 1, _fadeDuration, token));
        }

        public void SetStatus(string text) => _turnLabel.text = text;

        public void ShowResult(string text)
        {
            _resultLabel.text = text;
            _resultLabel.gameObject.SetActive(true);
            _fadeOverlayGroup.alpha = 0.55f;
        }

        public void Hide()
        {
            if (_bgmSource != null) _bgmSource.Stop();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _onExitRequested.OnNext(Unit.Default);
        }

        private void OnDestroy() => _onExitRequested.Dispose();
    }
}
