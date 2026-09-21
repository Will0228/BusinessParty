using UnityEngine;

namespace MixVerse.Fracture
{
    public sealed class FractureDemoView : MonoBehaviour
    {
        [SerializeField] private FractureObjectView[] _targets = System.Array.Empty<FractureObjectView>();
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Transform[] _swords = System.Array.Empty<Transform>();
        [SerializeField, Range(0, 1)] private float _progress;
        private IFractureDemoPresenter _presenter;
        private RenderTexture _preview;
        private bool _show = true;

        private void Start()
        {
            _presenter = new FractureDemoPresenter(this);
            _presenter.SetProgress(_progress);
            ResizePreview();
        }

        private void Update()
        {
            if (_presenter == null) return;
            if (!Mathf.Approximately(_progress, _presenter.Progress)) _presenter.SetProgress(_progress);
            _presenter.Tick(Time.unscaledDeltaTime);
            if (_show) ResizePreview();
        }

        public void RenderProgress(float value)
        {
            _progress = value;
            foreach (var target in _targets) if (target != null) target.SetProgress(value);
            for (var i = 0; i < _swords.Length; i++)
            {
                var sword = _swords[i];
                if (sword == null) continue;
                var sweep = Mathf.SmoothStep(0, 1, Mathf.Clamp01(value / 0.55f));
                sword.localPosition = new Vector3((i == 0 ? -5 : 5) + Mathf.Lerp(-2.2f, 2.2f, sweep), Mathf.Lerp(2.2f, -2.2f, sweep), -0.2f);
                sword.gameObject.SetActive(value < 0.65f);
            }
        }

        private void ResizePreview()
        {
            var width = Mathf.Clamp(Screen.width, 320, 1920);
            var height = Mathf.Clamp(Screen.height, 240, 1080);
            if (_preview != null && _preview.width == width && _preview.height == height) return;
            ReleasePreview();
            _preview = new RenderTexture(width, height, 24) { name = "Sword fracture preview" };
            _preview.Create();
            _previewCamera.targetTexture = _preview;
            _previewCamera.orthographicSize = Mathf.Max(8.2f, 11f / ((float)width / height));
        }

        private void OnGUI()
        {
            if (_presenter == null) return;
            GUI.depth = -10000;
            if (!_show)
            {
                if (GUI.Button(new Rect(Screen.width - 190, 12, 178, 32), "Open sword fracture demo"))
                {
                    _show = true;
                    _previewCamera.enabled = true;
                }
                return;
            }
            if (_preview != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _preview, ScaleMode.StretchToFill);
            var oldMatrix = GUI.matrix;
            var scale = Mathf.Min(Screen.width / 1000f, Screen.height / 650f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            var title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(26, 20, 680, 42), "SWORD IMPACT / FRACTURE LAB", title);
            GUI.Label(new Rect(28, 65, 720, 28), "Closed solid fragments  |  GPU animation  |  Reversible progress");
            GUI.Label(new Rect(width * 0.25f - 40, 115, 120, 30), "CUBE / 216");
            GUI.Label(new Rect(width * 0.75f - 40, 115, 140, 30), "SPHERE / 216");
            if (GUI.Button(new Rect(width - 140, 25, 112, 30), "Close demo"))
            {
                _show = false;
                _previewCamera.enabled = false;
            }
            GUI.Box(new Rect(24, height - 142, width - 48, 120), GUIContent.none);
            GUI.Label(new Rect(44, height - 130, 700, 26), $"IMPACT  {_presenter.Progress:0.000}    |    0 : Untouched     ->     1 : Fully fragmented");
            var next = GUI.HorizontalSlider(new Rect(44, height - 94, width - 360, 28), _presenter.Progress, 0, 1);
            if (!Mathf.Approximately(next, _presenter.Progress)) _presenter.SetProgress(next);
            if (GUI.Button(new Rect(width - 288, height - 102, 110, 34), "Play slash")) _presenter.Play();
            if (GUI.Button(new Rect(width - 166, height - 102, 110, 34), "Reset / 0")) _presenter.SetProgress(0);
            GUI.Label(new Rect(44, height - 61, width - 88, 26), "Drag the gauge in either direction. Orange surfaces show the inside of each fragment.");
            GUI.matrix = oldMatrix;
        }

        private void OnDisable()
        {
            if (_previewCamera != null) _previewCamera.enabled = false;
            ReleasePreview();
        }

        private void OnEnable()
        {
            if (_previewCamera != null) _previewCamera.enabled = _show;
        }

        private void ReleasePreview()
        {
            if (_previewCamera != null) _previewCamera.targetTexture = null;
            if (_preview == null) return;
            _preview.Release();
            Destroy(_preview);
            _preview = null;
        }
    }
}
