using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Home
{
    public sealed class HomeTestPlayView : MonoBehaviour
    {
        private KartTestPlayStageFactory _factory;
        private RawImage _screen;
        private TMP_Text _hud;
        private TMP_Text _status;
        private KartTestPlayStageView _stage;
        private RenderTexture _texture;

        public void Initialize(KartTestPlayStageFactory factory, RawImage screen, TMP_Text hud, TMP_Text status)
        {
            _factory = factory;
            _screen = screen;
            _hud = hud;
            _status = status;
        }

        public void Show()
        {
            if (_stage != null)
            {
                return;
            }

            _stage = _factory.Create();
            var pixels = _screen.rectTransform.rect.size * _screen.canvas.scaleFactor;
            _texture = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(pixels.x)), Mathf.Max(1, Mathf.RoundToInt(pixels.y)), 24);
            _stage.Camera.targetTexture = _texture;
            _screen.texture = _texture;
            _screen.enabled = true;
        }

        public void Render(KartTestDrive drive, float gain, bool midi)
        {
            _stage.Render(drive);
            var state = drive.TurboSeconds > 0f ? "<color=#FFD84A>TURBO  加速中</color>"
                : drive.IsDrifting ? $"DRIFT  {drive.DriftTier} / 2" : string.Empty;
            _hud.text = $"<size=40>{Mathf.Abs(drive.Speed):00}</size> <size=16>/ 100</size>\n" +
                        $"{(drive.Direction > 0 ? "DRIVE" : "REVERSE")}   GAIN {gain * 100f:0}%\n{state}";
            _status.text = midi
                ? "<color=#5AFF6E>DJコントローラー接続中</color>"
                : "<color=#9FB3BF>DJコントローラー未接続：キーボードでも操作できます</color>";
        }

        public void PlayDriftSe() => _stage.PlayDriftSe();

        public void Hide()
        {
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

            if (_hud != null)
            {
                _hud.text = string.Empty;
            }

            if (_status != null)
            {
                _status.text = string.Empty;
            }
        }

        private void OnDestroy() => Hide();
    }
}
