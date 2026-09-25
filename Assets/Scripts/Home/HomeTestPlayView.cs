using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MixVerse.Home
{
    public sealed class HomeTestPlayView : MonoBehaviour
    {
        private const float HudReferenceHeight = 900f;

        private readonly Subject<Unit> _onStartRequested = new Subject<Unit>();
        private KartStageFactory _factory;
        private RawImage _screen;
        private TMP_Text _status;
        private KartStageView _stage;
        private RectTransform _hud;
        private RenderTexture _texture;
        private CompositeDisposable _stageBindings;

        public Observable<Unit> OnStartRequested => _onStartRequested;

        public void Initialize(KartStageFactory factory, RawImage screen, TMP_Text status)
        {
            _factory = factory;
            _screen = screen;
            _status = status;
        }

        public void Show(KartRaceSettings settings)
        {
            if (_stage != null)
            {
                return;
            }

            _stage = _factory.Create(settings, null);
            var size = _screen.rectTransform.rect.size;
            var pixels = size * _screen.canvas.scaleFactor;
            _texture = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(pixels.x)), Mathf.Max(1, Mathf.RoundToInt(pixels.y)), 24);
            _stage.Camera.targetTexture = _texture;
            _screen.texture = _texture;
            _screen.enabled = true;
            MoveHudIntoScreen(size);
            SetEvent();
        }

        private void SetEvent()
        {
            _stageBindings = new CompositeDisposable();
            _stage.ActionButton.OnClickAsObservable()
                .Subscribe(_ => _onStartRequested.OnNext(Unit.Default))
                .AddTo(_stageBindings);
        }

        // インゲームの HUD は全画面のオーバーレイ Canvas なので、そのままだと Home の上に被さってしまう。
        // 中身をテストプレイ画面の中へ移し、1600x900 基準のレイアウトのまま縮小して重ねる
        private void MoveHudIntoScreen(Vector2 size)
        {
            var hudCanvas = _stage.RaceHud.transform.parent;
            var scale = size.y / HudReferenceHeight;
            _hud = (RectTransform)new GameObject("Test Play HUD", typeof(RectTransform)).transform;
            _hud.SetParent(_screen.transform, false);
            _hud.anchorMin = Vector2.one * 0.5f;
            _hud.anchorMax = Vector2.one * 0.5f;
            _hud.pivot = Vector2.one * 0.5f;
            _hud.sizeDelta = size / scale;
            _hud.localScale = Vector3.one * scale;

            while (hudCanvas.childCount > 0)
            {
                hudCanvas.GetChild(0).SetParent(_hud, false);
            }

            Destroy(hudCanvas.gameObject);

            foreach (var child in _hud.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = _screen.gameObject.layer;
            }
        }

        public void Render(KartRace race, float gain, bool ready, float countdown, bool midi)
        {
            _stage.Render(race, gain, ready, countdown, midi);
            _stage.ExitButton.gameObject.SetActive(false);
            _status.text = midi
                ? "<color=#5AFF6E>DJコントローラー接続中</color>"
                : "<color=#9FB3BF>DJコントローラー未接続：キーボードでも操作できます</color>";
        }

        public void ResetRace() => _stage.ResetObjects();

        public void PlayDriftSe() => _stage.PlayDriftSe();

        public void Hide()
        {
            _stageBindings?.Dispose();
            _stageBindings = null;

            if (_hud != null)
            {
                Destroy(_hud.gameObject);
                _hud = null;
            }

            // カメラが破棄済みの RenderTexture へ描こうとして画面全体に映り込まないよう、先に止めてから解放する
            if (_stage != null)
            {
                _stage.gameObject.SetActive(false);
                Destroy(_stage.gameObject);
                _stage = null;
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
                _texture = null;
            }

            if (_screen != null)
            {
                _screen.texture = null;
                _screen.enabled = false;
            }

            if (_status != null)
            {
                _status.text = string.Empty;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (_stage == null || keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                _onStartRequested.OnNext(Unit.Default);
            }
        }

        private void OnDestroy()
        {
            Hide();
            _onStartRequested.Dispose();
        }
    }
}
