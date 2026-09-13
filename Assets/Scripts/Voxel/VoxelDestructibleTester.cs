using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse
{
    /// <summary>
    /// VoxelDestructibleCube の動作確認用コンポーネント。
    /// クリックした地点へ、そこから離れるほど弱まる衝撃を与えて穴をあける。
    /// </summary>
    public sealed class VoxelDestructibleTester : MonoBehaviour
    {
        [SerializeField] private VoxelDestructibleCube _target;

        [Tooltip("クリックでの Raycast に使うカメラ。空なら Camera.main を使う。")]
        [SerializeField] private Camera _camera;

        [Tooltip("クリック地点を中心にした衝撃の半径。")]
        [SerializeField] private float _impactRadius = 0.5f;

        [Tooltip("キューブを壊れていない状態に戻すキー。")]
        [SerializeField] private Key _rebuildKey = Key.R;

        [Tooltip("調整パネルの表示を切り替えるキー。")]
        [SerializeField] private Key _panelKey = Key.F1;

        private bool _showPanel = true;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (_target == null)
            {
                _target = FindFirstObjectByType<VoxelDestructibleCube>(FindObjectsInactive.Include);
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            if (_target == null)
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

            if (_rebuildKey != Key.None && keyboard[_rebuildKey].wasPressedThisFrame)
            {
                _target.Rebuild();
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

            if (Physics.Raycast(ray, out var hit) && hit.collider.GetComponentInParent<VoxelDestructibleCube>() == _target)
            {
                _target.ApplyImpact(hit.point, _impactRadius);
            }
        }

        private void OnGUI()
        {
            if (!_showPanel || _target == null)
            {
                return;
            }

            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 11 };

            GUILayout.BeginArea(new Rect(10f, 10f, 260f, 100f), GUI.skin.box);

            GUILayout.Label($"クリック: 衝撃を与える / {_rebuildKey}: 元に戻す / {_panelKey}: 開閉", _labelStyle);

            _impactRadius = Slider("衝撃の半径", _impactRadius, 0.1f, 2f);

            GUILayout.EndArea();
        }

        private float Slider(string label, float value, float min, float max)
        {
            GUILayout.Label($"{label}: {value:0.###}", _labelStyle);
            return GUILayout.HorizontalSlider(value, min, max);
        }
    }
}
