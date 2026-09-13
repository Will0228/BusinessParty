using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse
{
    /// <summary>
    /// 血しぶきの見た目をその場で確かめるための動作確認用コンポーネント。
    /// クリックした場所へしぶきを飛ばし、垂れ方は画面のパネルから調整できる。
    /// </summary>
    public sealed class BloodSplatterTester : MonoBehaviour
    {
        [SerializeField] private BloodSplatterOverlay _overlay;

        [Header("Keys")]
        [Tooltip("画面のどこかにしぶきを 1 発飛ばす。")]
        [SerializeField] private Key _splatKey = Key.B;

        [Tooltip("画面のどこかにまとめて浴びせる。")]
        [SerializeField] private Key _burstKey = Key.N;

        [Tooltip("血をすべて消す。")]
        [SerializeField] private Key _clearKey = Key.C;

        [Tooltip("調整パネルの表示を切り替える。")]
        [SerializeField] private Key _panelKey = Key.F1;

        [Header("Options")]
        [Tooltip("左クリックした場所へしぶきを飛ばす。ドラッグすると、その動きの向きへ飛沫が散る。")]
        [SerializeField] private bool _splatOnClick = true;

        [SerializeField] private int _burstCount = 4;

        [SerializeField] private bool _showPanel = true;

        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (_overlay == null)
            {
                _overlay = FindFirstObjectByType<BloodSplatterOverlay>(FindObjectsInactive.Include);
            }
        }

        private void Update()
        {
            if (_overlay == null)
            {
                return;
            }

            ReadKeyboard();
            ReadMouse();
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (_splatKey != Key.None && keyboard[_splatKey].wasPressedThisFrame)
            {
                _overlay.Splat(RandomScreenUv());
            }

            if (_burstKey != Key.None && keyboard[_burstKey].wasPressedThisFrame)
            {
                _overlay.SplatBurst(RandomScreenUv(), _burstCount);
            }

            if (_clearKey != Key.None && keyboard[_clearKey].wasPressedThisFrame)
            {
                _overlay.Clear();
            }

            if (_panelKey != Key.None && keyboard[_panelKey].wasPressedThisFrame)
            {
                _showPanel = !_showPanel;
            }
        }

        private void ReadMouse()
        {
            var mouse = Mouse.current;

            if (!_splatOnClick || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var position = mouse.position.ReadValue();
            var uv = new Vector2(position.x / Screen.width, position.y / Screen.height);

            // パネルの上をクリックしたときは、スライダー操作とみなして飛ばさない
            if (_showPanel && new Rect(10f, 10f, 260f, 290f).Contains(new Vector2(position.x, Screen.height - position.y)))
            {
                return;
            }

            var delta = mouse.delta.ReadValue();
            var direction = delta.sqrMagnitude > 4f ? delta.normalized : Random.insideUnitCircle.normalized;

            _overlay.Splat(uv, direction);
        }

        private static Vector2 RandomScreenUv()
        {
            return new Vector2(Random.Range(0.15f, 0.85f), Random.Range(0.35f, 0.9f));
        }

        private void OnGUI()
        {
            if (!_showPanel || _overlay == null)
            {
                return;
            }

            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 11 };

            GUILayout.BeginArea(new Rect(10f, 10f, 260f, 290f), GUI.skin.box);

            GUILayout.Label($"クリック: しぶき / {_splatKey}: ランダム", _labelStyle);
            GUILayout.Label($"{_burstKey}: まとめて / {_clearKey}: 消す / {_panelKey}: 開閉", _labelStyle);

            _overlay.SplatRadius = Slider("大きさ", _overlay.SplatRadius, 0.02f, 0.3f);
            _overlay.SplatAmount = Slider("血の量", _overlay.SplatAmount, 0.3f, 3f);
            _overlay.Gravity = Slider("垂れる速さ", _overlay.Gravity, 0f, 800f);
            _overlay.FilmThickness = Slider("残る膜の厚み", _overlay.FilmThickness, 0.02f, 1f);
            _overlay.SurfaceTension = Slider("表面張力", _overlay.SurfaceTension, 0f, 1.5f);
            _overlay.DryRate = Slider("乾く速さ", _overlay.DryRate, 0f, 1f);
            _overlay.TimeScale = Slider("演出の速さ", _overlay.TimeScale, 0f, 3f);

            if (GUILayout.Button("消す"))
            {
                _overlay.Clear();
            }

            GUILayout.EndArea();
        }

        private float Slider(string label, float value, float min, float max)
        {
            GUILayout.Label($"{label}: {value:0.###}", _labelStyle);
            return GUILayout.HorizontalSlider(value, min, max);
        }
    }
}
