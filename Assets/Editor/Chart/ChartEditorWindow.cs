using MixVerse.Game;
using MixVerse.Game.Model;
using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools.Chart
{
    /// <summary>
    /// 4 小節 1 セットで譜面を編集する。小節は縦に並び、左右 2 列にノーツを置く。
    /// </summary>
    public sealed class ChartEditorWindow : EditorWindow
    {
        private const int MeasuresPerPage = 4;

        private static readonly int[] DivisionValues = { 1, 2, 3, 4, 6, 8 };
        private static readonly string[] DivisionLabels = { "4 分", "8 分", "12 分", "16 分", "24 分", "32 分" };

        [SerializeField] private NoteChartAsset _asset;
        [SerializeField] private bool _bottomIsEarliest;

        private IChartEditorPresenter _presenter;
        private ChartGridDrawer _drawer;

        [MenuItem("MixVerse/Rhythm/譜面エディタ")]
        public static void Open()
        {
            var window = GetWindow<ChartEditorWindow>("譜面エディタ");
            window.minSize = new Vector2(420f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            _drawer = new ChartGridDrawer();
            _presenter = new ChartEditorPresenter(new ChartAssetRepository(), MeasuresPerPage);
            _presenter.Load(_asset);
            saveChangesMessage = "譜面に未保存の変更があります。保存しますか？";
        }

        private void OnDisable()
        {
            _presenter = null;
            _drawer = null;
        }

        public override void SaveChanges()
        {
            if (_presenter == null || !_presenter.Save())
            {
                return;
            }

            _asset = _presenter.Asset;
            base.SaveChanges();
        }

        private void OnGUI()
        {
            if (_presenter == null)
            {
                return;
            }

            hasUnsavedChanges = _presenter.IsDirty;
            DrawAssetBar();
            DrawSettings();
            DrawPageBar();
            DrawGrid();
            DrawFooter();
        }

        private void DrawAssetBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var asset = (NoteChartAsset)EditorGUILayout.ObjectField(_presenter.Asset, typeof(NoteChartAsset), false);

                if (asset != _presenter.Asset)
                {
                    _asset = asset;
                    _presenter.Load(asset);
                }

                if (GUILayout.Button("新規", GUILayout.Width(52f)))
                {
                    _asset = null;
                    _presenter.Load(null);
                }

                if (GUILayout.Button("保存", GUILayout.Width(52f)) && _presenter.Save())
                {
                    _asset = _presenter.Asset;
                }

                if (GUILayout.Button("名前を付けて保存", GUILayout.Width(120f)) && _presenter.SaveAs())
                {
                    _asset = _presenter.Asset;
                }

                using (new EditorGUI.DisabledScope(_presenter.Asset == null))
                {
                    if (GUILayout.Button("読み直し", GUILayout.Width(72f)))
                    {
                        _presenter.Reload();
                    }
                }
            }
        }

        private void DrawSettings()
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 48f;

            using (new EditorGUILayout.HorizontalScope())
            {
                _presenter.SetBpm(EditorGUILayout.FloatField("BPM", _presenter.Bpm, GUILayout.Width(110f)));
                _presenter.SetMeasureCount(
                    EditorGUILayout.IntField("小節数", _presenter.Grid.MeasureCount, GUILayout.Width(110f)));
                _presenter.SetDivisionsPerBeat(EditorGUILayout.IntPopup("分解能", _presenter.Grid.DivisionsPerBeat,
                    DivisionLabels, DivisionValues, GUILayout.Width(130f)));
                GUILayout.FlexibleSpace();
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void DrawPageBar()
        {
            var previousArrow = _bottomIsEarliest ? "▼" : "▲";
            var nextArrow = _bottomIsEarliest ? "▲" : "▼";
            var lastMeasure = _presenter.FirstMeasure + _presenter.MeasuresInPage;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_presenter.PageIndex <= 0))
                {
                    if (GUILayout.Button($"{previousArrow} 前の {MeasuresPerPage} 小節", GUILayout.Height(24f)))
                    {
                        _presenter.MovePage(-1);
                    }
                }

                GUILayout.Label(
                    $"{_presenter.FirstMeasure + 1} - {lastMeasure} 小節　（{_presenter.PageIndex + 1} / {_presenter.PageCount} セット）",
                    EditorStyles.centeredGreyMiniLabel, GUILayout.Width(190f), GUILayout.Height(24f));

                using (new EditorGUI.DisabledScope(_presenter.PageIndex >= _presenter.PageCount - 1))
                {
                    if (GUILayout.Button($"{nextArrow} 次の {MeasuresPerPage} 小節", GUILayout.Height(24f)))
                    {
                        _presenter.MovePage(1);
                    }
                }
            }
        }

        private void DrawGrid()
        {
            var area = GUILayoutUtility.GetRect(100f, 10000f, 100f, 10000f, GUILayout.ExpandHeight(true));
            var layout = _drawer.CreateLayout(area, _presenter.Grid, _presenter.FirstMeasure,
                _presenter.MeasuresInPage, !_bottomIsEarliest);

            if (Event.current.type == EventType.Repaint)
            {
                _drawer.Draw(layout, _presenter.Grid, _presenter);
                return;
            }

            HandleGridInput(layout);
        }

        private void HandleGridInput(ChartPageLayout layout)
        {
            var current = Event.current;

            if (current.type != EventType.MouseDown || current.button != 0 ||
                !layout.TryResolve(current.mousePosition, out var lane, out var step))
            {
                return;
            }

            _presenter.Toggle(lane, step);
            current.Use();
            Repaint();
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    $"ノーツ {_presenter.NoteCount} 個　{(_presenter.IsDirty ? "未保存の変更あり" : "保存済み")}",
                    EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                _bottomIsEarliest = GUILayout.Toggle(_bottomIsEarliest, "下が先（落ちてくる向き）", EditorStyles.miniButton,
                    GUILayout.Width(150f));

                if (GUILayout.Button("このセットを消す", EditorStyles.miniButton, GUILayout.Width(110f)))
                {
                    _presenter.ClearPage();
                }

                if (GUILayout.Button("全部消す", EditorStyles.miniButton, GUILayout.Width(70f)) &&
                    EditorUtility.DisplayDialog("譜面エディタ", "置いたノーツを全部消します。よろしいですか？", "消す", "やめる"))
                {
                    _presenter.ClearAll();
                }
            }

            EditorGUILayout.LabelField("セルをクリックするとノーツを置く／消す。列ごとに別の要素として扱う。",
                EditorStyles.miniLabel);
        }
    }
}
