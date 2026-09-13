using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse
{
    /// <summary>
    /// 水面の波紋をその場で確かめるための動作確認用コンポーネント。
    /// クリックした地点へ即座に波紋を起こすほか、水滴を実際に落として一連の流れも試せる。
    /// </summary>
    public sealed class WaterWaveTester : MonoBehaviour
    {
        [SerializeField] private WaterWaveSurface _surface;

        [Tooltip("クリックでの Raycast に使うカメラ。空なら Camera.main を使う。")]
        [SerializeField] private Camera _camera;

        [Header("Keys")]
        [Tooltip("水滴を 1 個落とす。")]
        [SerializeField] private Key _dropKey = Key.B;

        [Tooltip("調整パネルの表示を切り替える。")]
        [SerializeField] private Key _panelKey = Key.F1;

        [Header("Droplet")]
        [Tooltip("キーで落とす水滴の、水面からの高さ。")]
        [SerializeField] private float _dropHeight = 3f;

        [SerializeField] private float _dropletMinRadius = 0.05f;
        [SerializeField] private float _dropletMaxRadius = 0.3f;

        [Header("Click Splash")]
        [Tooltip("クリックした地点へ即座に起こす波紋の半径。")]
        [SerializeField] private float _clickRadius = 0.15f;

        [Tooltip("クリックした地点へ即座に起こす波紋の速さ。")]
        [SerializeField] private float _clickSpeed = 3f;

        private bool _showPanel = true;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (_surface == null)
            {
                _surface = FindFirstObjectByType<WaterWaveSurface>(FindObjectsInactive.Include);
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            if (_surface == null)
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

            if (_dropKey != Key.None && keyboard[_dropKey].wasPressedThisFrame)
            {
                DropRandomDroplet();
            }

            if (_panelKey != Key.None && keyboard[_panelKey].wasPressedThisFrame)
            {
                _showPanel = !_showPanel;
            }
        }

        private void ReadMouse()
        {
            var mouse = Mouse.current;

            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || _camera == null)
            {
                return;
            }

            var position = mouse.position.ReadValue();
            var ray = _camera.ScreenPointToRay(new Vector3(position.x, position.y, 0f));

            if (Physics.Raycast(ray, out var hit) && hit.collider.GetComponentInParent<WaterWaveSurface>() == _surface)
            {
                _surface.Splash(hit.point, _clickRadius, _clickSpeed);
            }
        }

        /// <summary>水面の少し上から水滴を 1 個落とし、実際に着水するまでの流れを試す。</summary>
        private void DropRandomDroplet()
        {
            var origin = _surface.transform.position
                + (Vector3.up * _dropHeight)
                + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

            var radius = Random.Range(_dropletMinRadius, _dropletMaxRadius);

            var droplet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            droplet.name = "TestWaterDroplet";
            droplet.transform.SetPositionAndRotation(origin, Quaternion.identity);
            droplet.transform.localScale = Vector3.one * (radius * 2f);

            var rigidbody = droplet.AddComponent<Rigidbody>();
            rigidbody.mass = Mathf.Max(0.01f, radius);

            var waterDroplet = droplet.AddComponent<WaterDroplet>();
            waterDroplet.SetTargetSurface(_surface);
        }

        private void OnGUI()
        {
            if (!_showPanel || _surface == null)
            {
                return;
            }

            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 11 };

            GUILayout.BeginArea(new Rect(10f, 10f, 260f, 230f), GUI.skin.box);

            GUILayout.Label($"クリック: その場で波紋 / {_dropKey}: 水滴を落とす / {_panelKey}: 開閉", _labelStyle);

            _surface.AmplitudePerRadius = Slider("振幅(半径)", _surface.AmplitudePerRadius, 0f, 2f);
            _surface.AmplitudePerSpeed = Slider("振幅(速度)", _surface.AmplitudePerSpeed, 0f, 0.5f);
            _surface.MaxAmplitude = Slider("振幅の上限", _surface.MaxAmplitude, 0.05f, 1f);
            _surface.WavelengthPerRadius = Slider("波長(半径)", _surface.WavelengthPerRadius, 0.5f, 10f);
            _surface.SpeedMultiplier = Slider("伝播速度倍率", _surface.SpeedMultiplier, 0.1f, 3f);
            _surface.DecayPerPeriod = Slider("減衰(1周期毎)", _surface.DecayPerPeriod, 0.05f, 0.95f);

            GUILayout.EndArea();
        }

        private float Slider(string label, float value, float min, float max)
        {
            GUILayout.Label($"{label}: {value:0.###}", _labelStyle);
            return GUILayout.HorizontalSlider(value, min, max);
        }
    }
}
