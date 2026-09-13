using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools
{
    public sealed class SealPeelPreviewWindow : EditorWindow
    {
        [SerializeField] private Texture _imageA;
        [SerializeField] private Texture _imageB;
        [SerializeField] private float _progress;
        [SerializeField] private Vector2 _direction = new Vector2(1f, -1f);
        [SerializeField] private float _radius = 0.075f;
        [SerializeField] private float _liftAngle = 22f;
        private ISealPeelPreviewPresenter _presenter;

        [MenuItem("MixVerse/Seal/Peel Preview")]
        public static void Open()
        {
            var window = GetWindow<SealPeelPreviewWindow>("Seal Peel Preview");
            window.minSize = new Vector2(440f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            _presenter = new SealPeelPreviewPresenter(new SealPeelDemoTextureFactory());
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("シールをはがす / EDIT MODE", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("画像未指定時はサンプル画像を表示します。Playは不要です。", EditorStyles.wordWrappedLabel);
            _imageA = (Texture)EditorGUILayout.ObjectField("A / はがす画像", _imageA, typeof(Texture), false);
            _imageB = (Texture)EditorGUILayout.ObjectField("B / 下にある画像", _imageB, typeof(Texture), false);
            EditorGUILayout.Space(8f);
            _progress = EditorGUILayout.Slider("はがす量 (0=A / 1=B)", _progress, 0f, 1f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("0 / 貼り付け")) _progress = 0f;
                if (GUILayout.Button("0.35 / めくれ確認")) _progress = 0.35f;
                if (GUILayout.Button("1 / はがし完了")) _progress = 1f;
            }
            _direction = EditorGUILayout.Vector2Field("はがす方向", _direction);
            _radius = EditorGUILayout.Slider("曲がりの半径", _radius, 0.01f, 0.25f);
            _liftAngle = EditorGUILayout.Slider("裏面の持ち上げ角度", _liftAngle, 5f, 60f);
            EditorGUILayout.Space(8f);
            var area = GUILayoutUtility.GetRect(100f, 10000f, 100f, 10000f, GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
            {
                _presenter?.Draw(area, _imageA, _imageB, _progress, _direction, _radius, _liftAngle);
            }
        }
    }
}
